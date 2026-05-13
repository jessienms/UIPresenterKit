using R3;
using UILib;
using UnityEngine.UIElements;

namespace Samples
{
    public readonly struct ProfileDetailArgs : IPresenterArgs<ProfileDetailWindowPresenter>
    {
        public SampleProfileData Profile { get; }
        public ProfileDetailArgs(SampleProfileData _profile) { Profile = _profile; }
        public void InvokeOnShow(ProfileDetailWindowPresenter _presenter) => _presenter.OnShow(this);
    }

    [Window("UI/ProfileDetailWindow")]
    public sealed class ProfileDetailWindowPresenter : PresenterBase<ProfileDetailArgs>
    {
        private Label nameLabel;
        private Label levelLabel;
        private Label roleLabel;
        private Label statusLabel;
        private Button closeBtn;

        public override void OnViewReady(VisualElement _root)
        {
            nameLabel   = _root.Q<Label>("name-label");
            levelLabel  = _root.Q<Label>("level-label");
            roleLabel   = _root.Q<Label>("role-label");
            statusLabel = _root.Q<Label>("status-label");
            closeBtn    = _root.Q<Button>("close-btn");
        }

        public override void OnShow(ProfileDetailArgs _args)
        {
            base.OnShow(_args);

            var profile = _args.Profile;
            nameLabel.text   = profile.Name;
            levelLabel.text  = $"Lv. {profile.Level}";
            roleLabel.text   = profile.Role;
            statusLabel.text = profile.Status;

            Observable.FromEvent(_h => closeBtn.clicked += _h, _h => closeBtn.clicked -= _h)
                .Subscribe(_ => RequestHide())
                .AddTo(Disposables);
        }
    }
}
