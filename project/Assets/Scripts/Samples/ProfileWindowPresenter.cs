using R3;
using UILib;
using UnityEngine.UIElements;
using VContainer;

namespace Samples
{
    [Window("UI/ProfileWindow")]
    public sealed class ProfileWindowPresenter : WindowPresenterBase
    {
        private SampleProfileModel profileModel;

        private Button closeBtn;
        private Label greetingLabel;

        private ProfilePresenter profilePresenter;

        [Inject]
        public void OnInjected(SampleProfileModel _profile)
        {
            profileModel = _profile;
        }

        public override void OnViewReady(UIDocument _doc)
        {
            var root      = _doc.rootVisualElement;
            closeBtn      = root.Q<Button>("close-btn");
            greetingLabel = root.Q<Label>("greeting-label");

            // profilePresenter = new ProfilePresenter(profileModel);
            // profilePresenter.Bind(root.Q("profile-section"));
        }

        protected override void OnShow()
        {
            profileModel.UserName
                .CombineLatest(profileModel.Level, (_name, _level) => $"안녕하세요, {_name}님! (Lv. {_level})")
                .Subscribe(_text => greetingLabel.text = _text)
                .AddTo(disposables);

            Observable.FromEvent(_h => closeBtn.clicked += _h, _h => closeBtn.clicked -= _h)
                .Subscribe(_ => RequestClose())
                .AddTo(disposables);
        }

        public override void OnDetached()
        {
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
