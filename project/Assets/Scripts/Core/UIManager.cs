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
    /// scope 당 singleton. Factory + 1차 캐시 + Presenter 반환 역할.
    /// Show&lt;T&gt;()                   : 동적 prefab spawn + 1차/2차 캐시. Presenter 반환.
    /// Show(args)                   : 동적, 인자 주입. TPresenter/TArgs 는 constraint 로 자동 추론.
    /// Show&lt;T&gt;(UIDocument)          : 씬 정적 UIDocument 바인딩. 캐싱 없음. Presenter 반환.
    /// Show(UIDocument, args)       : 정적 UIDocument, 인자 주입.
    /// Show&lt;T&gt;(VisualElement)       : 기존 VisualElement 바인딩. 캐싱 없음. Presenter 반환.
    /// Show(VisualElement, args)    : 정적 VisualElement, 인자 주입.
    /// Attach&lt;T&gt;(parent)           : UXML 로드 → parent 자식 추가. 1차/2차 캐시 운용.
    /// Attach(parent, args)         : UXML 동적 마운트, 인자 주입.
    /// Hide(presenter)              : 1차 캐시로 회수 (동적) 또는 즉시 Dispose (정적).
    /// Detach(presenter)            : parent 에서 분리 후 1차 attached 캐시로 회수.
    /// Preload&lt;T&gt;()               : 활성화 없이 1차 캐시만 채운다.
    /// BindListView&lt;TSlot, TData&gt;() : ListView 슬롯을 Presenter 로 wiring. IDisposable 반환.
    /// Dispose 시 1차 캐시 전체를 OnDetached 후 UIPoolingManager 로 이관한다.
    ///
    /// 가시성 제어: 최초 활성화만 SetActive(true), 이후 Show/Hide 는 display:flex/none 으로 처리.
    /// SetActive 사이클이 없으므로 UIDocument 가 UXML 을 재clone 하지 않아 OnViewReady 는 최초 1회만 호출된다.
    /// </summary>
    public sealed class UIManager : IDisposable
    {
        private readonly IObjectResolver resolver;
        private readonly UIPoolingManager poolingManager;
        private readonly IAssetLoader assetLoader;

        private readonly Dictionary<string, Stack<PresenterInstance>> cache = new();
        private readonly Dictionary<string, Stack<PresenterInstance>> attachedCache = new();
        private readonly Dictionary<IPresenter, PresenterInstance> activeWindows = new();
        private readonly Dictionary<IPresenter, IDisposable> hideSubscriptions = new();
        private readonly Dictionary<string, Stack<PresenterInstance>> slotCache = new();
        private readonly Dictionary<string, VisualTreeAsset> uxmlCache = new();

        private int nextOrder;

        private static readonly Dictionary<Type, string> typeToKey = new();

        public UIManager(IObjectResolver _resolver, UIPoolingManager _poolingManager, IAssetLoader _assetLoader)
        {
            resolver = _resolver;
            poolingManager = _poolingManager;
            assetLoader = _assetLoader;
        }

        // --- 동적 Show (prefab spawn) ---

        /// <summary>동적 Show: prefab 을 1차/2차 캐시 또는 신규 spawn 으로 확보해 활성화한다.</summary>
        public async UniTask<T> Show<T>()
            where T : class, IPresenter, new()
        {
            var (instance, presenter) = await PrepareDynamic<T>();
            presenter.OnShow();
            FinalizeShow(presenter, instance);
            return presenter;
        }

        /// <summary>동적 Show (인자 주입): IPresenterArgs&lt;TPresenter&gt; 파라미터에서 TPresenter 를 추론한다.</summary>
        public async UniTask<TPresenter> Show<TPresenter>(IPresenterArgs<TPresenter> _args)
            where TPresenter : class, IPresenter, new()
        {
            var (instance, presenter) = await PrepareDynamic<TPresenter>();
            _args.InvokeOnShow(presenter);
            FinalizeShow(presenter, instance);
            return presenter;
        }

        // --- 정적 Show (UIDocument) ---

        /// <summary>
        /// 정적 Show: 씬에 이미 배치된 UIDocument 에 Presenter 를 연결해 활성화한다.
        /// 캐싱/풀링 없음. Hide 시 Presenter 를 즉시 OnDetached + Dispose.
        /// [Window] attribute 불필요. sortingOrder 는 씬 설정 그대로 유지.
        /// </summary>
        public T Show<T>(UIDocument _sceneDoc)
            where T : class, IPresenter, new()
        {
            var (instance, presenter) = PrepareStaticDoc<T>(_sceneDoc);
            presenter.OnShow();
            FinalizeShow(presenter, instance);
            return presenter;
        }

        /// <summary>정적 Show (인자 주입, UIDocument): IPresenterArgs&lt;TPresenter&gt; 파라미터에서 TPresenter 를 추론한다.</summary>
        public TPresenter Show<TPresenter>(UIDocument _sceneDoc, IPresenterArgs<TPresenter> _args)
            where TPresenter : class, IPresenter, new()
        {
            var (instance, presenter) = PrepareStaticDoc<TPresenter>(_sceneDoc);
            _args.InvokeOnShow(presenter);
            FinalizeShow(presenter, instance);
            return presenter;
        }

        // --- 정적 Show (VisualElement) ---

        /// <summary>기존 VisualElement 노드에 Presenter 를 연결해 활성화한다.</summary>
        public T Show<T>(VisualElement _root)
            where T : class, IPresenter, new()
        {
            var (instance, presenter) = PrepareStaticRoot<T>(_root);
            presenter.OnShow();
            FinalizeShow(presenter, instance);
            return presenter;
        }

        /// <summary>기존 VisualElement 노드에 Presenter 를 연결해 활성화한다 (인자 주입).</summary>
        public TPresenter Show<TPresenter>(VisualElement _root, IPresenterArgs<TPresenter> _args)
            where TPresenter : class, IPresenter, new()
        {
            var (instance, presenter) = PrepareStaticRoot<TPresenter>(_root);
            _args.InvokeOnShow(presenter);
            FinalizeShow(presenter, instance);
            return presenter;
        }

        // --- Attach (UXML 동적 자식 마운트) ---

        /// <summary>
        /// UXML 을 로드해 VisualElement 를 생성하고 _parent 의 자식으로 추가하며 Presenter 를 활성화한다.
        /// [Window("key")] attribute 로 UXML 키를 지정한다. 1차/2차 캐시로 재사용한다.
        /// Detach 시 parent 에서 분리 후 1차 캐시 → scope 종료 시 2차 전역 풀로 이관.
        /// </summary>
        public async UniTask<T> Attach<T>(VisualElement _parent)
            where T : class, IPresenter, new()
        {
            var (instance, presenter) = await PrepareAttach<T>(_parent);
            presenter.OnShow();
            FinalizeAttach(presenter, instance);
            return presenter;
        }

        /// <summary>UXML 동적 자식 마운트 (인자 주입): IPresenterArgs&lt;TPresenter&gt; 파라미터에서 TPresenter 를 추론한다.</summary>
        public async UniTask<TPresenter> Attach<TPresenter>(VisualElement _parent, IPresenterArgs<TPresenter> _args)
            where TPresenter : class, IPresenter, new()
        {
            var (instance, presenter) = await PrepareAttach<TPresenter>(_parent);
            _args.InvokeOnShow(presenter);
            FinalizeAttach(presenter, instance);
            return presenter;
        }

        public void Hide(IPresenter _presenter)
        {
            if (_presenter == null)
            {
                return;
            }

            if (!activeWindows.Remove(_presenter, out var presenterInstance))
            {
                return;
            }

            if (hideSubscriptions.Remove(_presenter, out var hideSubscription))
            {
                hideSubscription.Dispose();
            }

            presenterInstance.Presenter.OnHide();
            presenterInstance.GetRoot().SetActiveAsDisplay(false);

            if (presenterInstance.IsPooled)
            {
                PushToCache(presenterInstance);
            }
            else
            {
                Debug.Assert(presenterInstance.Presenter != null);

                presenterInstance.Presenter.OnDetached();
                presenterInstance.Presenter.Dispose();
            }
        }

        /// <summary>
        /// Attach 로 마운트한 Presenter 를 비활성화하고 parent 에서 분리한다.
        /// 1차 캐시(attachedCache)로 회수되며 scope 종료 시 2차 전역 풀로 이관된다.
        /// </summary>
        public void Detach(IPresenter _presenter)
        {
            if (_presenter == null)
            {
                return;
            }

            if (!activeWindows.Remove(_presenter, out var presenterInstance))
            {
                return;
            }

            if (hideSubscriptions.Remove(_presenter, out var hideSubscription))
            {
                hideSubscription.Dispose();
            }

            var root = presenterInstance.GetRoot();
            presenterInstance.Presenter.OnHide();
            root.SetActiveAsDisplay(false);
            root.RemoveFromHierarchy();

            PushToAttachedCache(presenterInstance);
        }

        public async UniTask Preload<T>()
            where T : class, IPresenter, new()
        {
            var key = GetKey(typeof(T));
            if (cache.TryGetValue(key, out var stack) && stack.Count > 0) return;

            var pair = await AcquireOrCreate<T>(key);
            PushToCache(pair);
        }

        // --- ListView 슬롯 바인딩 ---

        /// <summary>
        /// ListView 의 각 슬롯을 TSlot Presenter 로 wiring 한다.
        /// UXML 을 비동기 로드(캐시 miss 시 1회)한 뒤 makeItem/bindItem/unbindItem/destroyItem 을 자동 연결한다.
        /// 반환된 IDisposable 을 윈도우 Disposables 에 추가하면 OnHide 시 슬롯이 자동 회수된다.
        /// </summary>
        public async UniTask<IDisposable> BindListView<TSlot, TData>(
            ListView _listView,
            IList<TData> _items)
            where TSlot : class, IPresenter<TData>, new()
        {
            var key = GetKey(typeof(TSlot));
            if (!uxmlCache.TryGetValue(key, out var uxml))
            {
                uxml = await assetLoader.LoadUxmlAsync(key);
                uxmlCache[key] = uxml;
            }

            var binding = new SlotBinding<TSlot, TData>(this, _listView, _items, key, uxml);
            binding.Attach();
            return binding;
        }

        public void Dispose()
        {
            foreach (var presenter in new List<IPresenter>(activeWindows.Keys))
            {
                if (activeWindows.TryGetValue(presenter, out var inst) && inst.Document == null && inst.IsPooled)
                    Detach(presenter);
                else
                    Hide(presenter);
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

            foreach (var (key, stack) in attachedCache)
            {
                while (stack.TryPop(out var inst))
                {
                    inst.Presenter.OnDetached();
                    poolingManager.ReleaseAttached(key, inst);
                }
            }
            attachedCache.Clear();

            foreach (var stack in slotCache.Values)
            {
                while (stack.TryPop(out var inst))
                {
                    inst.Presenter.OnDetached();
                    inst.Presenter.Dispose();
                }
            }
            slotCache.Clear();
            uxmlCache.Clear();
        }

        // --- Private helpers ---

        private async UniTask<(PresenterInstance instance, T presenter)> PrepareDynamic<T>()
            where T : class, IPresenter, new()
        {
            var key = GetKey(typeof(T));
            var presenterInstance = await AcquireOrCreate<T>(key);
            var doc = presenterInstance.Document;

            if (!presenterInstance.IsViewReady)
            {
                doc.gameObject.SetActive(true);
                presenterInstance.Presenter.OnViewReady(presenterInstance.GetRoot());
                presenterInstance.IsViewReady = true;
            }

            presenterInstance.GetRoot().SetActiveAsDisplay(true);
            doc.sortingOrder = ++nextOrder;

            return (presenterInstance, (T)presenterInstance.Presenter);
        }

        private (PresenterInstance instance, T presenter) PrepareStaticDoc<T>(UIDocument _sceneDoc)
            where T : class, IPresenter, new()
        {
            if (_sceneDoc == null)
            {
                throw new ArgumentNullException(nameof(_sceneDoc));
            }

            var presenter = new T();
            resolver.Inject(presenter);
            var presenterInstance = new PresenterInstance(null, _sceneDoc, presenter, false);

            _sceneDoc.gameObject.SetActive(true);
            presenter.OnViewReady(presenterInstance.GetRoot());
            presenterInstance.GetRoot().SetActiveAsDisplay(true);

            return (presenterInstance, presenter);
        }

        private (PresenterInstance instance, T presenter) PrepareStaticRoot<T>(VisualElement _root)
            where T : class, IPresenter, new()
        {
            if (_root == null)
            {
                throw new ArgumentNullException(nameof(_root));
            }

            var presenter = new T();
            resolver.Inject(presenter);
            var presenterInstance = new PresenterInstance(null, _root, presenter, false);

            presenter.OnViewReady(_root);
            _root.SetActiveAsDisplay(true);

            return (presenterInstance, presenter);
        }

        private void FinalizeShow(IPresenter _presenter, PresenterInstance _instance)
        {
            activeWindows[_presenter] = _instance;
            SubscribeHideRequest(_presenter);
        }

        private void FinalizeAttach(IPresenter _presenter, PresenterInstance _instance)
        {
            activeWindows[_presenter] = _instance;
            SubscribeDetachRequest(_presenter);
        }

        private async UniTask<PresenterInstance> AcquireOrCreate<T>(string _key)
            where T : class, IPresenter, new()
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
            if (!go.TryGetComponent<UIDocument>(out var doc))
            {
                UnityEngine.Object.Destroy(go);
                throw new InvalidOperationException($"[UILib] '{_key}' 인스턴스 root 에 UIDocument 가 없습니다. Window prefab 의 루트 GameObject 에 UIDocument 를 추가하세요.");
            }

            var presenter = new T();
            resolver.Inject(presenter);
            return new PresenterInstance(_key, doc, presenter, true);
        }

        private void PushToCache(PresenterInstance _pair)
        {
            if (!cache.TryGetValue(_pair.Key, out var stack))
            {
                stack = new Stack<PresenterInstance>();
                cache[_pair.Key] = stack;
            }
            stack.Push(_pair);
        }

        private void SubscribeHideRequest(IPresenter _presenter)
        {
            var hideSubscription = _presenter.HideRequested
                .Take(1)
                .Subscribe(_ => Hide(_presenter));
            hideSubscriptions[_presenter] = hideSubscription;
        }

        private void SubscribeDetachRequest(IPresenter _presenter)
        {
            var sub = _presenter.HideRequested
                .Take(1)
                .Subscribe(_ => Detach(_presenter));
            hideSubscriptions[_presenter] = sub;
        }

        internal PresenterInstance AcquireSlot<TSlot>(string _key, VisualTreeAsset _uxml)
            where TSlot : class, IPresenter, new()
        {
            if (slotCache.TryGetValue(_key, out var stack) && stack.Count > 0)
                return stack.Pop();

            var root = _uxml.CloneTree();
            root.style.flexGrow = 1;
            var presenter = new TSlot();
            resolver.Inject(presenter);
            var instance = new PresenterInstance(_key, root, presenter, true);
            presenter.OnViewReady(root);
            instance.IsViewReady = true;
            return instance;
        }

        internal void ReleaseSlot(string _key, PresenterInstance _instance)
        {
            if (!slotCache.TryGetValue(_key, out var stack))
            {
                stack = new Stack<PresenterInstance>();
                slotCache[_key] = stack;
            }
            stack.Push(_instance);
        }

        private async UniTask<(PresenterInstance instance, T presenter)> PrepareAttach<T>(VisualElement _parent)
            where T : class, IPresenter, new()
        {
            if (_parent == null)
                throw new ArgumentNullException(nameof(_parent));

            var key = GetKey(typeof(T));
            var instance = await AcquireOrCreateAttached<T>(key);

            if (!instance.IsViewReady)
            {
                instance.Presenter.OnViewReady(instance.GetRoot());
                instance.IsViewReady = true;
            }

            _parent.Add(instance.GetRoot());
            instance.GetRoot().SetActiveAsDisplay(true);
            return (instance, (T)instance.Presenter);
        }

        private async UniTask<PresenterInstance> AcquireOrCreateAttached<T>(string _key)
            where T : class, IPresenter, new()
        {
            if (attachedCache.TryGetValue(_key, out var stack) && stack.Count > 0)
                return stack.Pop();

            var existing = poolingManager.AcquireAttached(_key);
            if (existing != null)
            {
                resolver.Inject(existing.Presenter);
                return existing;
            }

            if (!uxmlCache.TryGetValue(_key, out var uxml))
            {
                uxml = await assetLoader.LoadUxmlAsync(_key);
                uxmlCache[_key] = uxml;
            }

            var root = uxml.CloneTree();
            root.style.flexGrow = 1;

            var presenter = new T();
            resolver.Inject(presenter);
            return new PresenterInstance(_key, root, presenter, true);
        }

        private void PushToAttachedCache(PresenterInstance _instance)
        {
            if (!attachedCache.TryGetValue(_instance.Key, out var stack))
            {
                stack = new Stack<PresenterInstance>();
                attachedCache[_instance.Key] = stack;
            }
            stack.Push(_instance);
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
