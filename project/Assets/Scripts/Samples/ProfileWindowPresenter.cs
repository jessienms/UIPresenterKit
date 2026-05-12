using R3;
using UILib;
using UnityEngine.UIElements;
using VContainer;

namespace Samples
{
    [Window("UI/ProfileWindow")]
    public sealed class ProfileWindowPresenter : WindowPresenterBase
    {
        private UIManager uiManager;
        private SampleProfileModel profileModel;

        private Button closeBtn;
        private Label greetingLabel;
        private VisualElement profileSection;

        [Inject]
        public void OnInjected(UIManager _uiManager, SampleProfileModel _profile)
        {
            uiManager = _uiManager;
            profileModel = _profile;
        }

        public override void OnViewReady(VisualElement _root)
        {
            closeBtn      = _root.Q<Button>("close-btn");
            greetingLabel = _root.Q<Label>("greeting-label");
            profileSection = _root.Q("profile-section");
        }

        public override void OnShow()
        {
            base.OnShow();

            profileModel.UserName
                .CombineLatest(profileModel.Level, (_name, _level) => $"안녕하세요, {_name}님! (Lv. {_level})")
                .Subscribe(_text => greetingLabel.text = _text)
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
            uiManager = null;
            profileModel = null;
        }

    }
}
