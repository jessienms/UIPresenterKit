using R3;
using UIPresenterKit.Core;
using UnityEngine.UIElements;
using VContainer;

namespace UIPresenterKit.Samples
{
    public sealed class ProfilePresenter : PresenterBase
    {
        private SampleProfileModel model;

        private Label userNameLabel;
        private Label levelLabel;
        private Button nextProfileBtn;

        [Inject]
        public void OnInjected(SampleProfileModel _model)
        {
            model = _model;
        }

        public override void OnCleared()
        {
            model = null;
        }

        public override void OnViewReady(VisualElement _root)
        {
            userNameLabel  = _root.Q<Label>("username-label");
            levelLabel     = _root.Q<Label>("level-label");
            nextProfileBtn = _root.Q<Button>("next-profile-btn");
        }

        public override void OnShow()
        {
            base.OnShow();

            model.UserName
                .Subscribe(_name => userNameLabel.text = _name)
                .AddTo(Disposables);

            model.Level
                .Subscribe(_level => levelLabel.text = $"Lv. {_level}")
                .AddTo(Disposables);

            Observable.FromEvent(_h => nextProfileBtn.clicked += _h, _h => nextProfileBtn.clicked -= _h)
                .Subscribe(_ => model.NextProfile())
                .AddTo(Disposables);
        }
    }
}
