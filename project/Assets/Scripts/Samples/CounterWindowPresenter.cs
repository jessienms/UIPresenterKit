using R3;
using UILib;
using UnityEngine.UIElements;
using VContainer;

namespace Samples
{
    [Window("UI/CounterWindow")]
    public sealed class CounterWindowPresenter : WindowPresenterBase
    {
        private SampleCounterModel counterModel;
        private SampleProfileModel profileModel;

        private Button closeBtn;
        private Button actionBtn;
        private Button resetBtn;
        private Label counterLabel;

        private ProfilePresenter profilePresenter;

        [Inject]
        public void OnInjected(SampleCounterModel _counter, SampleProfileModel _profile)
        {
            counterModel = _counter;
            profileModel = _profile;
        }

        public override void OnViewReady(UIDocument _doc)
        {
            var root     = _doc.rootVisualElement;
            closeBtn     = root.Q<Button>("close-btn");
            actionBtn    = root.Q<Button>("action-btn");
            resetBtn     = root.Q<Button>("reset-btn");
            counterLabel = root.Q<Label>("counter-label");

            // profilePresenter = new ProfilePresenter(profileModel);
            // profilePresenter.Bind(root.Q("profile-section"));
        }

        protected override void OnShow()
        {
            counterModel.Count
                .Subscribe(_count => counterLabel.text = $"{_count}번 클릭")
                .AddTo(disposables);

            Observable.FromEvent(_h => actionBtn.clicked += _h, _h => actionBtn.clicked -= _h)
                .Subscribe(_ => counterModel.Increment())
                .AddTo(disposables);

            Observable.FromEvent(_h => resetBtn.clicked += _h, _h => resetBtn.clicked -= _h)
                .Subscribe(_ => counterModel.Reset())
                .AddTo(disposables);

            Observable.FromEvent(_h => closeBtn.clicked += _h, _h => closeBtn.clicked -= _h)
                .Subscribe(_ => RequestClose())
                .AddTo(disposables);
        }

        public override void OnDetached()
        {
            counterModel = null;
            profileModel = null;
        }

        public override void Dispose()
        {
            profilePresenter?.Dispose();
            profilePresenter = null;
            base.Dispose();
        }
    }
}
