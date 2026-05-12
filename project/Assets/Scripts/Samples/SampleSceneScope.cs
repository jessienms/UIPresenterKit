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
        [SerializeField] private UIDocument hudDocument;

        protected override void Configure(IContainerBuilder _builder)
        {
            _builder.Register<SampleProfileModel>(Lifetime.Singleton);
            _builder.Register<SampleCounterModel>(Lifetime.Singleton);
            _builder.Register<UIManager>(Lifetime.Singleton);
            _builder.RegisterComponent(hudDocument);
            _builder.RegisterEntryPoint<SampleSceneEntryPoint>();
        }
    }

    /// <summary>
    /// 씬 시작 진입점. 동적 윈도우 prefab 을 미리 캐시하고 정적 HUD 를 Show.
    /// </summary>
    sealed class SampleSceneEntryPoint : IStartable
    {
        private readonly UIManager uiManager;
        private readonly UIDocument hudDocument;

        public SampleSceneEntryPoint(UIManager _manager, UIDocument _hudDocument)
        {
            uiManager = _manager;
            hudDocument = _hudDocument;
        }

        public void Start() => RunAsync().Forget();

        private async UniTaskVoid RunAsync()
        {
            // 첫 클릭 응답성을 위해 동적 윈도우 prefab 미리 캐시
            await uiManager.Preload<CounterWindowPresenter>();
            await uiManager.Preload<ProfileWindowPresenter>();

            // 정적 HUD 바인딩 — 씬 종료 시 UIManager.Dispose 가 자동 Hide
            uiManager.Show<HudPresenter>(hudDocument);
        }
    }
}
