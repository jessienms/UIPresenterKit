using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine.UIElements;
using VContainer;

namespace UILib
{
    /// <summary>
    /// scope 당 singleton. Factory + 1차 캐시 + Handle 발급 역할.
    /// Show&lt;T&gt;()            : 동적 prefab spawn + 1차/2차 캐시. Handle 반환.
    /// Show&lt;T&gt;(UIDocument)  : 씬 정적 UIDocument 바인딩. 캐싱 없음. Handle 반환.
    /// Hide(handle)          : 1차 캐시로 회수 (동적) 또는 즉시 Dispose (정적).
    /// Preload&lt;T&gt;()         : 활성화 없이 1차 캐시만 채운다.
    /// Dispose 시 1차 캐시 전체를 OnDetached 후 UIPoolingManager 로 이관한다.
    /// </summary>
    public sealed class UIManager : IDisposable
    {
        private readonly IObjectResolver resolver;
        private readonly UIPoolingManager poolingManager;

        private readonly Dictionary<string, Stack<WindowPair>> cache = new();
        private readonly Dictionary<IWindowHandle, LiveEntry> liveHandles = new();

        private int nextOrder;

        private static readonly Dictionary<Type, string> typeToKey = new();

        private struct LiveEntry
        {
            public readonly WindowPair Pair;
            public readonly IDisposable CloseSub;
            public readonly Action Invalidate;
            public LiveEntry(WindowPair _pair, IDisposable _closeSub, Action _invalidate)
            {
                Pair = _pair; CloseSub = _closeSub; Invalidate = _invalidate;
            }
        }

        public UIManager(IObjectResolver _resolver, UIPoolingManager _poolingManager)
        {
            resolver = _resolver;
            poolingManager = _poolingManager;
        }

        /// <summary>동적 Show: prefab 을 1차/2차 캐시 또는 신규 spawn 으로 확보해 활성화한다.</summary>
        public async UniTask<WindowHandle<T>> Show<T>()
            where T : class, IWindowPresenter, IWindowLifecycle, new()
        {
            var key = GetKey(typeof(T));
            var pair = await AcquireOrCreate<T>(key);

            pair.Go.SetActive(true);
            pair.Presenter.OnViewReady(pair.Go.GetComponent<UIDocument>());
            pair.Go.GetComponent<UIDocument>().sortingOrder = ++nextOrder;
            ((IWindowLifecycle)pair.Presenter).Show();

            var handle = new WindowHandle<T>((T)pair.Presenter);
            var closeSub = pair.Presenter.CloseRequested
                .Take(1)
                .Subscribe(_ => Hide(handle));
            liveHandles[handle] = new LiveEntry(pair, closeSub, handle.Invalidate);

            return handle;
        }

        /// <summary>
        /// 정적 Show: 씬에 이미 배치된 UIDocument 에 Presenter 를 연결해 활성화한다.
        /// 캐싱/풀링 없음. Hide 시 Presenter 를 즉시 OnDetached + Dispose.
        /// [Window] attribute 불필요. sortingOrder 는 씬 설정 그대로 유지.
        /// </summary>
        public WindowHandle<T> Show<T>(UIDocument _sceneDoc)
            where T : class, IWindowPresenter, IWindowLifecycle, new()
        {
            var presenter = new T();
            resolver.Inject(presenter);
            var pair = new WindowPair(null, _sceneDoc.gameObject, presenter, false);

            _sceneDoc.gameObject.SetActive(true);
            presenter.OnViewReady(_sceneDoc);
            ((IWindowLifecycle)presenter).Show();

            var handle = new WindowHandle<T>(presenter);
            var closeSub = presenter.CloseRequested
                .Take(1)
                .Subscribe(_ => Hide(handle));
            liveHandles[handle] = new LiveEntry(pair, closeSub, handle.Invalidate);

            return handle;
        }

        public void Hide(IWindowHandle _handle)
        {
            if (!_handle.IsValid) return;
            if (!liveHandles.TryGetValue(_handle, out var entry)) return;

            liveHandles.Remove(_handle);
            entry.CloseSub.Dispose();

            ((IWindowLifecycle)entry.Pair.Presenter).Hide();
            entry.Pair.Go.SetActive(false);

            if (entry.Pair.IsPooled)
            {
                PushToCache(entry.Pair);
            }
            else
            {
                entry.Pair.Presenter.OnDetached();
                entry.Pair.Presenter.Dispose();
            }

            entry.Invalidate();
        }

        public async UniTask Preload<T>()
            where T : class, IWindowPresenter, IWindowLifecycle, new()
        {
            var key = GetKey(typeof(T));
            if (cache.TryGetValue(key, out var stack) && stack.Count > 0) return;

            var pair = await AcquireOrCreate<T>(key);
            PushToCache(pair);
        }

        public void Dispose()
        {
            foreach (var handle in new List<IWindowHandle>(liveHandles.Keys))
                Hide(handle);

            foreach (var (key, stack) in cache)
            {
                while (stack.TryPop(out var pair))
                {
                    pair.Presenter.OnDetached();
                    poolingManager.Release(key, pair);
                }
            }
            cache.Clear();
        }

        private async UniTask<WindowPair> AcquireOrCreate<T>(string _key)
            where T : class, IWindowPresenter, IWindowLifecycle, new()
        {
            if (cache.TryGetValue(_key, out var stack) && stack.Count > 0)
                return stack.Pop();

            var existing = poolingManager.Acquire(_key);
            if (existing != null)
            {
                resolver.Inject(existing.Presenter);
                return existing;
            }

            var go = await poolingManager.Spawn(_key);
            var presenter = new T();
            resolver.Inject(presenter);
            return new WindowPair(_key, go, presenter, true);
        }

        private void PushToCache(WindowPair _pair)
        {
            if (!cache.TryGetValue(_pair.Key, out var stack))
            {
                stack = new Stack<WindowPair>();
                cache[_pair.Key] = stack;
            }
            stack.Push(_pair);
        }

        private static string GetKey(Type _type)
        {
            if (!typeToKey.TryGetValue(_type, out var key))
            {
                var attr = _type.GetCustomAttribute<WindowAttribute>();
                if (attr == null)
                    throw new InvalidOperationException($"[UILib] {_type.Name} 에 [Window] attribute 가 없습니다.");
                key = attr.Key;
                typeToKey[_type] = key;
            }
            return key;
        }
    }
}
