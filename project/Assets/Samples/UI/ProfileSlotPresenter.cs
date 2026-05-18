using UIPresenterKit.Samples.Model;
using UnityEngine.UIElements;

namespace UIPresenterKit.Samples.UI
{
    public readonly struct ProfileSlotArgs : IPresenterArgs<ProfileSlotPresenter>
    {
        public SampleProfileData Profile { get; }
        public ProfileSlotArgs(SampleProfileData _profile) { Profile = _profile; }
        public void InvokeOnShow(ProfileSlotPresenter _presenter) => _presenter.OnShow(Profile);
    }

    [Window("UI/ProfileListSlot")]
    public sealed class ProfileSlotPresenter : PresenterBase<SampleProfileData>
    {
        private Label idLabel;
        private Label nameLabel;
        private Label metaLabel;
        private Label statusLabel;

        public override void OnViewReady(VisualElement _root)
        {
            idLabel = _root.Q<Label>("profile-id");
            nameLabel = _root.Q<Label>("profile-name");
            metaLabel = _root.Q<Label>("profile-meta");
            statusLabel = _root.Q<Label>("profile-status");
        }

        public override void OnShow(SampleProfileData _profile)
        {
            base.OnShow(_profile);
            idLabel.text = $"#{_profile.Id:000}";
            nameLabel.text = _profile.Name;
            metaLabel.text = $"Lv. {_profile.Level} / {_profile.Role}";
            statusLabel.text = _profile.Status;
        }

        public override void OnHide()
        {
            base.OnHide();
            idLabel.text = string.Empty;
            nameLabel.text = string.Empty;
            metaLabel.text = string.Empty;
            statusLabel.text = string.Empty;
        }

        public override void OnCleared()
        {
            idLabel = null;
            nameLabel = null;
            metaLabel = null;
            statusLabel = null;
        }
    }
}
