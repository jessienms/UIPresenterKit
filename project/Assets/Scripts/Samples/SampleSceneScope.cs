using Cysharp.Threading.Tasks;
using UILib;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using VContainer.Unity;

namespace Samples
{
    /// <summary>
    /// 씬 LifetimeScope. UIManager 와 공유 Model 을 등록한다.
    /// ParentLifetimeScope 를 SampleAppScope 로 지정해야 한다.
    /// </summary>
    public sealed class SampleSceneScope : LifetimeScope
    {
        [SerializeField] private UIDocument menuDocument;
        [SerializeField] private UIDocument optionsDocument;

        protected override void Configure(IContainerBuilder _builder)
        {
            _builder.Register<SampleProfileModel>(Lifetime.Singleton);
            _builder.Register<SampleCounterModel>(Lifetime.Singleton);
            _builder.Register<UIManager>(Lifetime.Singleton);
            _builder.RegisterInstance(new MenuDocumentRef(menuDocument));
            _builder.RegisterInstance(new OptionsDocumentRef(optionsDocument));
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

        public SampleSceneEntryPoint(
            UIManager _manager,
            MenuDocumentRef _menuRef,
            OptionsDocumentRef _optionsRef)
        {
            uiManager = _manager;
            menuDocument = _menuRef.Document;
            optionDocument = _optionsRef.Document;
        }

        public void Start() => RunAsync().Forget();

        private async UniTaskVoid RunAsync()
        {
            // 첫 클릭 응답성을 위해 동적 윈도우 prefab 미리 캐시
            await uiManager.Preload<CounterWindowPresenter>();
            await uiManager.Preload<ProfileWindowPresenter>();
            await uiManager.Preload<ProfileListWindowPresenter>();

            menuDocument.SetActiveAsDisplay(false);
            optionDocument.SetActiveAsDisplay(false);
            
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
}
