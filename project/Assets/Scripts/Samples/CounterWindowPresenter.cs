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
        private UIManager uiManager;

        private Button closeBtn;
        private Button actionBtn;
        private Button resetBtn;
        private Label counterLabel;
        private VisualElement profileSection;

        [Inject]
        public void OnInjected(SampleCounterModel _counter, UIManager _uiManager)
        {
            counterModel = _counter;
            uiManager = _uiManager;
        }

        public override void OnViewReady(VisualElement _root)
        {
            closeBtn     = _root.Q<Button>("close-btn");
            actionBtn    = _root.Q<Button>("action-btn");
            resetBtn     = _root.Q<Button>("reset-btn");
            counterLabel = _root.Q<Label>("counter-label");
            profileSection = _root.Q("profile-section");
        }

        public override void OnShow()
        {
            base.OnShow();

            counterModel.Count
                .Subscribe(_count => counterLabel.text = $"{_count}번 클릭")
                .AddTo(Disposables);

            Observable.FromEvent(_h => actionBtn.clicked += _h, _h => actionBtn.clicked -= _h)
                .Subscribe(_ => counterModel.Increment())
                .AddTo(Disposables);

            Observable.FromEvent(_h => resetBtn.clicked += _h, _h => resetBtn.clicked -= _h)
                .Subscribe(_ => counterModel.Reset())
                .AddTo(Disposables);

            Observable.FromEvent(_h => closeBtn.clicked += _h, _h => closeBtn.clicked -= _h)
                .Subscribe(_ => RequestHide())
                .AddTo(Disposables);

            uiManager.Show<ProfilePresenter>(profileSection).HideOnHide(this);
        }

        public override void OnHide()
        {
            base.OnHide();
        }

        public override void OnDetached()
        {
            counterModel = null;
            uiManager = null;
        }

    }
}
