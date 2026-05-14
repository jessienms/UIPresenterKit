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
    /// scope 당 singleton. Factory + 1차 캐시 + Presenter 반환 역할.
    ///
    /// 활성화 API:
    /// Show&lt;T&gt;()                       : 동적 prefab spawn + 1차/2차 캐시. Presenter 반환.
    /// Show(args)                       : 동적, 인자 주입. TPresenter/TArgs 는 constraint 로 자동 추론.
    /// Show&lt;T&gt;(UIDocument)              : 씬 정적 UIDocument 바인딩. 캐싱 없음. Presenter 반환.
    /// Show(UIDocument, args)           : 정적 UIDocument, 인자 주입.
    /// Show&lt;T&gt;(VisualElement)           : 기존 VisualElement 바인딩. 캐싱 없음. Presenter 반환.
    /// Show(VisualElement, args)        : 정적 VisualElement, 인자 주입.
    /// ShowAttached&lt;T&gt;(parent)          : UXML 로드 → parent 자식 추가. 1차/2차 캐시 운용.
    /// ShowAttached(parent, args)       : UXML 동적 마운트, 인자 주입.
    ///
    /// 비활성화 API:
    /// Hide(presenter)                  : 통합 cleanup. 마운팅 방식에 따라 자동 분기한다.
    ///                                    - 동적 Show       → 1차 캐시로 회수
    ///                                    - 정적 Show       → 즉시 OnCleared + Dispose
    ///                                    - 동적 ShowAttached → parent 에서 분리 후 1차 캐시로 회수
    ///
    /// 기타:
    /// Preload&lt;T&gt;()                    : 활성화 없이 1차 캐시만 채운다.
    /// BindListView&lt;TSlot, TData&gt;()    : ListView 슬롯을 Presenter 로 wiring. IDisposable 반환.
    /// Dispose 시 1차 캐시 전체를 OnCleared 후 UIPoolingManager 로 이관한다.
    ///
    /// 가시성 제어: 최초 활성화만 SetActive(true), 이후 Show/Hide 는 display:flex/none 으로 처리.
    /// SetActive 사이클이 없으므로 UIDocument 가 UXML 을 재clone 하지 않아 OnViewReady 는 최초 1회만 호출된다.
    /// </summary>
    public sealed class UIManager : IDisposable
    {
        private readonly IObjectResolver resolver;
        private readonly UIPoolingManager poolingManager;
        private readonly IAssetLoader assetLoader;

        private readonly Dictionary<string, Stack<DocumentInstance>> cache = new();
        private readonly Dictionary<string, Stack<ElementInstance>> elementCache = new();
        private readonly Dictionary<IPresenter, PresenterInstanceBase> activeWindows = new();
        private readonly Dictionary<IPresenter, IDisposable> hideSubscriptions = new();
        private readonly Dictionary<string, VisualTreeAsset> uxmlCache = new();

        private int nextOrder;

        private static readonly Dictionary<Type, string> TypeToKey = new();

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
        /// 캐싱/풀링 없음. Hide 시 Presenter 를 즉시 OnCleared + Dispose.
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

        // --- ShowAttached (UXML 동적 자식 마운트) ---

        /// <summary>
        /// UXML 을 로드해 VisualElement 를 생성하고 _parent 의 자식으로 추가하며 Presenter 를 활성화한다.
        /// [Window("key")] attribute 로 UXML 키를 지정한다. 1차/2차 캐시로 재사용한다.
        /// Hide 시 parent 에서 분리 후 1차 캐시 → scope 종료 시 2차 전역 풀로 이관.
        /// </summary>
        public async UniTask<T> ShowAttached<T>(VisualElement _parent)
            where T : class, IPresenter, new()
        {
            var (instance, presenter) = await PrepareAttach<T>(_parent);
            presenter.OnShow();
            FinalizeShow(presenter, instance);
            return presenter;
        }

        /// <summary>UXML 동적 자식 마운트 (인자 주입): IPresenterArgs&lt;TPresenter&gt; 파라미터에서 TPresenter 를 추론한다.</summary>
        public async UniTask<TPresenter> ShowAttached<TPresenter>(VisualElement _parent, IPresenterArgs<TPresenter> _args)
            where TPresenter : class, IPresenter, new()
        {
            var (instance, presenter) = await PrepareAttach<TPresenter>(_parent);
            _args.InvokeOnShow(presenter);
            FinalizeShow(presenter, instance);
            return presenter;
        }

        /// <summary>
        /// Show / ShowAttached 로 활성화한 Presenter 를 비활성화한다. 마운팅 방식에 따라 자동 분기:
        /// - 동적 Show (DocumentInstance + pooled): 1차 캐시로 회수
        /// - 정적 Show (UIDocument/VisualElement, not pooled): 즉시 OnCleared + Dispose
        /// - 동적 ShowAttached (ElementInstance + pooled): parent 에서 분리 후 1차 캐시로 회수
        /// </summary>
        public void Hide(IPresenter _presenter)
        {
            if (_presenter == null)
                return;

            if (!activeWindows.Remove(_presenter, out var instance))
                return;

            if (hideSubscriptions.Remove(_presenter, out var sub))
                sub.Dispose();

            instance.Presenter.OnHide();
            var root = instance.GetRoot();
            root.SetActiveAsDisplay(false);

            if (!instance.IsPooled)
            {
                instance.Presenter.OnCleared();
                instance.Presenter.Dispose();
            }
            else if (instance is ElementInstance attached)
            {
                root.RemoveFromHierarchy();
                ReleaseElement(attached);
            }
            else
            {
                PushToCache((DocumentInstance)instance);
            }
        }

        public async UniTask Preload<T>()
            where T : class, IPresenter, new()
        {
            var key = GetKey(typeof(T));
            if (cache.TryGetValue(key, out var stack) && stack.Count > 0) return;

            var instance = await AcquireOrCreate<T>(key);
            PushToCache(instance);
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
            binding.Install();
            return binding;
        }

        public void Dispose()
        {
            foreach (var presenter in new List<IPresenter>(activeWindows.Keys))
                Hide(presenter);

            foreach (var (key, stack) in cache)
            {
                while (stack.TryPop(out var inst))
                {
                    inst.Presenter.OnCleared();
                    poolingManager.ReleaseDocument(key, inst);
                }
            }
            cache.Clear();

            foreach (var (key, stack) in elementCache)
            {
                while (stack.TryPop(out var inst))
                {
                    inst.Presenter.OnCleared();
                    poolingManager.ReleaseElement(key, inst);
                }
            }
            elementCache.Clear();

            uxmlCache.Clear();
        }

        // --- Private helpers ---

        private async UniTask<(DocumentInstance instance, T presenter)> PrepareDynamic<T>()
            where T : class, IPresenter, new()
        {
            var key = GetKey(typeof(T));
            var instance = await AcquireOrCreate<T>(key);

            if (!instance.IsViewReady)
            {
                instance.Document.gameObject.SetActive(true);
                instance.Presenter.OnViewReady(instance.GetRoot());
                instance.IsViewReady = true;
            }

            instance.GetRoot().SetActiveAsDisplay(true);
            instance.Document.sortingOrder = ++nextOrder;

            return (instance, (T)instance.Presenter);
        }

        private (DocumentInstance instance, T presenter) PrepareStaticDoc<T>(UIDocument _sceneDoc)
            where T : class, IPresenter, new()
        {
            if (_sceneDoc == null)
                throw new ArgumentNullException(nameof(_sceneDoc));

            var presenter = new T();
            resolver.Inject(presenter);
            var instance = new DocumentInstance(null, _sceneDoc, presenter, false);

            _sceneDoc.gameObject.SetActive(true);
            presenter.OnViewReady(instance.GetRoot());
            instance.GetRoot().SetActiveAsDisplay(true);

            return (instance, presenter);
        }

        private (ElementInstance instance, T presenter) PrepareStaticRoot<T>(VisualElement _root)
            where T : class, IPresenter, new()
        {
            if (_root == null)
                throw new ArgumentNullException(nameof(_root));

            var presenter = new T();
            resolver.Inject(presenter);
            var instance = new ElementInstance(null, _root, presenter, false);

            presenter.OnViewReady(_root);
            _root.SetActiveAsDisplay(true);

            return (instance, presenter);
        }

        private void FinalizeShow(IPresenter _presenter, PresenterInstanceBase _instance)
        {
            activeWindows[_presenter] = _instance;
            SubscribeHideRequest(_presenter);
        }

        private async UniTask<DocumentInstance> AcquireOrCreate<T>(string _key)
            where T : class, IPresenter, new()
        {
            if (cache.TryGetValue(_key, out var stack) && stack.Count > 0)
                return stack.Pop();

            var existing = poolingManager.AcquireDocument(_key);
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
            return new DocumentInstance(_key, doc, presenter, true);
        }

        private void PushToCache(DocumentInstance _instance)
        {
            if (!cache.TryGetValue(_instance.Key, out var stack))
            {
                stack = new Stack<DocumentInstance>();
                cache[_instance.Key] = stack;
            }
            stack.Push(_instance);
        }

        private void SubscribeHideRequest(IPresenter _presenter)
        {
            var sub = _presenter.HideRequested
                .Take(1)
                .Subscribe(_ => Hide(_presenter));
            hideSubscriptions[_presenter] = sub;
        }

        internal ElementInstance AcquireOrCreateElement<T>(string _key, VisualTreeAsset _uxml)
            where T : class, IPresenter, new()
        {
            if (elementCache.TryGetValue(_key, out var stack) && stack.Count > 0)
                return stack.Pop();

            var existing = poolingManager.AcquireElement(_key);
            if (existing != null)
            {
                resolver.Inject(existing.Presenter);
                return existing;
            }

            var root = _uxml.CloneTree();
            root.style.flexGrow = 1;
            var presenter = new T();
            resolver.Inject(presenter);
            return new ElementInstance(_key, root, presenter, true);
        }

        internal void ReleaseElement(ElementInstance _instance)
        {
            if (!elementCache.TryGetValue(_instance.Key, out var stack))
            {
                stack = new Stack<ElementInstance>();
                elementCache[_instance.Key] = stack;
            }
            stack.Push(_instance);
        }

        private async UniTask<(ElementInstance instance, T presenter)> PrepareAttach<T>(VisualElement _parent)
            where T : class, IPresenter, new()
        {
            if (_parent == null)
                throw new ArgumentNullException(nameof(_parent));

            var key = GetKey(typeof(T));
            var instance = await AcquireOrCreateElement<T>(key);

            if (!instance.IsViewReady)
            {
                instance.Presenter.OnViewReady(instance.GetRoot());
                instance.IsViewReady = true;
            }

            _parent.Add(instance.GetRoot());
            instance.GetRoot().SetActiveAsDisplay(true);
            return (instance, (T)instance.Presenter);
        }

        private async UniTask<ElementInstance> AcquireOrCreateElement<T>(string _key)
            where T : class, IPresenter, new()
        {
            if (!uxmlCache.TryGetValue(_key, out var uxml))
            {
                uxml = await assetLoader.LoadUxmlAsync(_key);
                uxmlCache[_key] = uxml;
            }
            return AcquireOrCreateElement<T>(_key, uxml);
        }

        private static string GetKey(Type _type)
        {
            if (!TypeToKey.TryGetValue(_type, out var key))
            {
                var attr = _type.GetCustomAttribute<WindowAttribute>();
                if (attr == null)
                    throw new InvalidOperationException($"[UILib] {_type.Name} 에 [Window] attribute 가 없습니다.");
                key = attr.Key;
                TypeToKey[_type] = key;
            }
            return key;
        }
    }
}
