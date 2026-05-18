using Cysharp.Threading.Tasks;
using UIPresenterKit.Core;
using UIPresenterKit.Samples.Game;
using UIPresenterKit.Samples.Model;
using UIPresenterKit.Samples.UI;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using VContainer.Unity;

namespace UIPresenterKit.Samples.Scope
{
    /// <summary>
    /// 씬 LifetimeScope. UIManager 와 공유 Model 을 등록한다.
    /// ParentLifetimeScope 를 SampleAppScope 로 지정해야 한다.
    /// </summary>
    public sealed class SampleSceneScope : LifetimeScope
    {
        [SerializeField] private UIDocument menuDocument;
        [SerializeField] private UIDocument optionsDocument;
        [SerializeField] private UIDocument floatingDocument;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private GameObject mob1Prefab;

        protected override void Configure(IContainerBuilder _builder)
        {
            _builder.Register<SampleProfileModel>(Lifetime.Singleton);
            _builder.Register<MobListModel>(Lifetime.Singleton);
            _builder.Register<UIManager>(Lifetime.Singleton);
            _builder.RegisterInstance(new MenuDocumentRef(menuDocument));
            _builder.RegisterInstance(new OptionsDocumentRef(optionsDocument));
            _builder.RegisterInstance(new FloatingDocumentRef(floatingDocument));
            _builder.RegisterInstance(new MainCameraRef(mainCamera));
            _builder.RegisterInstance(new MobPrefabRef(mob1Prefab));
            
            _builder.Register<MobProcessor>(Lifetime.Singleton);
            _builder.RegisterEntryPoint<MobProcessor>();
            _builder.RegisterEntryPoint<SampleSceneEntryPoint>();
        }
    }

    /// <summary>
    /// 씬 시작 진입점. 동적 윈도우 prefab 을 미리 캐시하고 정적 메뉴를 Show.
    /// </summary>
    sealed class SampleSceneEntryPoint : IStartable
    {
        private readonly UIManager uiManager;
        private readonly UIDocument menuDocument;
        private readonly UIDocument optionDocument;
        private readonly UIDocument floatingDocument;

        public SampleSceneEntryPoint(
            UIManager _manager,
            MenuDocumentRef _menuRef,
            OptionsDocumentRef _optionsRef,
            FloatingDocumentRef _floatingRef)
        {
            uiManager = _manager;
            menuDocument = _menuRef.Document;
            optionDocument = _optionsRef.Document;
            floatingDocument = _floatingRef.Document;
        }

        public void Start() => RunAsync().Forget();

        private async UniTaskVoid RunAsync()
        {
            // 첫 클릭 응답성을 위해 동적 윈도우 prefab 미리 캐시
            await uiManager.Preload<MobSpawnPresenter>();
            await uiManager.Preload<ProfileWindowPresenter>();
            await uiManager.Preload<ProfileListWindowPresenter>();
            await uiManager.Preload<RandomProfilePickerWindowPresenter>();

            menuDocument.SetActiveAsDisplay(false);
            optionDocument.SetActiveAsDisplay(false);

            // floatingDocument 는 항상 활성화 (HP 바 mount 호스트)
            floatingDocument.gameObject.SetActive(true);
            floatingDocument.rootVisualElement.pickingMode = PickingMode.Ignore;

            // 정적 메뉴 바인딩 — 씬 종료 시 UIManager.Dispose 가 자동 Hide
            uiManager.Show<MenuPresenter>(menuDocument);
        }
    }

    public sealed class MenuDocumentRef
    {
        public UIDocument Document { get; }
        public MenuDocumentRef(UIDocument _doc) { Document = _doc; }
    }

    public sealed class OptionsDocumentRef
    {
        public UIDocument Document { get; }
        public OptionsDocumentRef(UIDocument _doc) { Document = _doc; }
    }

    public sealed class FloatingDocumentRef
    {
        public UIDocument Document { get; }
        public FloatingDocumentRef(UIDocument _doc) { Document = _doc; }
    }

    public sealed class MainCameraRef
    {
        public Camera Camera { get; }
        public MainCameraRef(Camera _cam) { Camera = _cam; }
    }

    public sealed class MobPrefabRef
    {
        public GameObject Prefab { get; }
        public MobPrefabRef(GameObject _prefab) { Prefab = _prefab; }
    }
}
