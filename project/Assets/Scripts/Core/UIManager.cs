using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
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
    ///
    /// 가시성 제어: 최초 활성화만 SetActive(true), 이후 Show/Hide 는 display:flex/none 으로 처리.
    /// SetActive 사이클이 없으므로 UIDocument 가 UXML 을 재clone 하지 않아 OnViewReady 는 최초 1회만 호출된다.
    /// </summary>
    public sealed class UIManager : IDisposable
    {
        private readonly IObjectResolver resolver;
        private readonly UIPoolingManager poolingManager;

        private readonly Dictionary<string, Stack<WindowInstance>> cache = new();
        private readonly Dictionary<IWindowHandle, WindowSession> activeWindows = new();

        private int nextOrder;

        private static readonly Dictionary<Type, string> typeToKey = new();

        private readonly struct WindowSession
        {
            public readonly WindowInstance Instance;
            public readonly IDisposable CloseSubscription;
            public readonly Action InvalidateHandle;

            public WindowSession(WindowInstance _instance, IDisposable _closeSubscription, Action _invalidateHandle)
            {
                Instance = _instance;
                CloseSubscription = _closeSubscription;
                InvalidateHandle = _invalidateHandle;
            }
        }

        public UIManager(IObjectResolver _resolver, UIPoolingManager _poolingManager)
        {
            resolver = _resolver;
            poolingManager = _poolingManager;
        }

        /// <summary>동적 Show: prefab 을 1차/2차 캐시 또는 신규 spawn 으로 확보해 활성화한다.</summary>
        public async UniTask<WindowHandle<T>> Show<T>()
            where T : class, IWindowPresenter, new()
        {
            var key = GetKey(typeof(T));
            var windowInstance = await AcquireOrCreate<T>(key);
            var doc = windowInstance.GameObject.GetComponent<UIDocument>();

            if (!windowInstance.IsViewReady)
            {
                // 최초 활성화: UIDocument.OnEnable 이 UXML 을 클론하므로 SetActive 가 필요.
                windowInstance.GameObject.SetActive(true);
                windowInstance.Presenter.OnViewReady(doc);
                windowInstance.IsViewReady = true;
            }
            else
            {
                // 재사용: 트리는 유효하므로 display 만 복원.
                doc.rootVisualElement.style.display = DisplayStyle.Flex;
            }

            doc.sortingOrder = ++nextOrder;
            windowInstance.Presenter.Show();

            var handle = new WindowHandle<T>((T)windowInstance.Presenter);
            var closeSubscription = windowInstance.Presenter.CloseRequested
                .Take(1)
                .Subscribe(_ => Hide(handle));
            activeWindows[handle] = new WindowSession(windowInstance, closeSubscription, handle.Invalidate);

            return handle;
        }

        /// <summary>
        /// 정적 Show: 씬에 이미 배치된 UIDocument 에 Presenter 를 연결해 활성화한다.
        /// 캐싱/풀링 없음. Hide 시 Presenter 를 즉시 OnDetached + Dispose.
        /// [Window] attribute 불필요. sortingOrder 는 씬 설정 그대로 유지.
        /// </summary>
        public WindowHandle<T> Show<T>(UIDocument _sceneDoc)
            where T : class, IWindowPresenter, new()
        {
            var presenter = new T();
            resolver.Inject(presenter);
            var windowInstance = new WindowInstance(null, _sceneDoc.gameObject, presenter, false);

            // GO 가 최초 비활성 상태일 수 있으므로 SetActive(true) 는 항상 호출.
            // 이미 활성이면 no-op. display:none 상태라면 Flex 로 복원.
            _sceneDoc.gameObject.SetActive(true);
            _sceneDoc.SetActiveAsDisplay(true);
            presenter.OnViewReady(_sceneDoc);
            presenter.Show();

            var handle = new WindowHandle<T>(presenter);
            var closeSubscription = presenter.CloseRequested
                .Take(1)
                .Subscribe(_ => Hide(handle));
            activeWindows[handle] = new WindowSession(windowInstance, closeSubscription, handle.Invalidate);

            return handle;
        }

        public void Hide(IWindowHandle _handle)
        {
            if (!_handle.IsValid)
            {
                return;
            }
            
            if (!activeWindows.Remove(_handle, out var session))
            {
                return;
            }

            session.CloseSubscription.Dispose();

            session.Instance.Presenter.Hide();
            if (session.Instance.GameObject)
            {
                session.Instance.GameObject.GetComponent<UIDocument>().SetActiveAsDisplay(false);    
            }

            if (session.Instance.IsPooled)
            {
                PushToCache(session.Instance);
            }
            else
            {
                Debug.Assert(session.Instance.Presenter != null);
                
                session.Instance.Presenter.OnDetached();
                session.Instance.Presenter.Dispose();
            }

            session.InvalidateHandle();
        }

        public async UniTask Preload<T>()
            where T : class, IWindowPresenter, new()
        {
            var key = GetKey(typeof(T));
            if (cache.TryGetValue(key, out var stack) && stack.Count > 0) return;

            var pair = await AcquireOrCreate<T>(key);
            PushToCache(pair);
        }

        public void Dispose()
        {
            foreach (var handle in new List<IWindowHandle>(activeWindows.Keys))
            {
                Hide(handle);
            }

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

        private async UniTask<WindowInstance> AcquireOrCreate<T>(string _key)
            where T : class, IWindowPresenter, new()
        {
            if (cache.TryGetValue(_key, out var stack) && stack.Count > 0)
            {
                return stack.Pop();
            }

            var existing = poolingManager.Acquire(_key);
            if (existing != null)
            {
                resolver.Inject(existing.Presenter);
                return existing;
            }

            var go = await poolingManager.Spawn(_key);
            var presenter = new T();
            resolver.Inject(presenter);
            return new WindowInstance(_key, go, presenter, true);
        }

        private void PushToCache(WindowInstance _pair)
        {
            if (!cache.TryGetValue(_pair.Key, out var stack))
            {
                stack = new Stack<WindowInstance>();
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
