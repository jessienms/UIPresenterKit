using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using VContainer.Unity;

namespace Samples
{
    // 공유 Model은 DI로 등록하고, 창별 Presenter는 Start()에서 직접 조립한다.
    // UIDocument처럼 창마다 달라지는 값은 SerializeField로 받아 수동으로 넘긴다.
    public class SampleSceneScope : LifetimeScope
    {
        [SerializeField] UIDocument window1Document;
        [SerializeField] UIDocument window2Document;

        SampleWindowPresenter window1Presenter;
        SampleWindow2Presenter window2Presenter;

        protected override void Configure(IContainerBuilder _builder)
        {
            _builder.Register<SampleProfileModel>(Lifetime.Singleton);
            _builder.Register<SampleCounterModel>(Lifetime.Singleton);
        }

        void Start()
        {
            var profileModel = Container.Resolve<SampleProfileModel>();
            var counterModel = Container.Resolve<SampleCounterModel>();

            // 두 창은 서로 다른 Presenter 클래스지만
            // ProfilePresenter(profileModel)를 공통으로 사용한다.
            window1Presenter = new SampleWindowPresenter(
                counterModel,
                new ProfilePresenter(profileModel),
                window1Document);
            window1Presenter.Start();

            window2Presenter = new SampleWindow2Presenter(
                profileModel,
                new ProfilePresenter(profileModel),
                window2Document);
            window2Presenter.Start();
        }

        protected override void OnDestroy()
        {
            window1Presenter?.Dispose();
            window2Presenter?.Dispose();
            base.OnDestroy();
        }
    }
}
