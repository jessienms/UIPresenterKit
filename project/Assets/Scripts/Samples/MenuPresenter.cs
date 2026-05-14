using Cysharp.Threading.Tasks;
using R3;
using UIPresenterKit.Core;
using UnityEngine.UIElements;
using VContainer;

namespace UIPresenterKit.Samples
{
    public sealed class MenuPresenter : PresenterBase
    {
        private UIManager uiManager;
        private UIDocument optionsDocument;

        private Button openCounterBtn;
        private Button openProfileBtn;
        private Button openProfileListBtn;
        private Button toggleOptionsBtn;

        private OptionsPanelPresenter optionsPresenter;

        [Inject]
        public void OnInjected(UIManager _uiManager, OptionsDocumentRef _optionsRef)
        {
            uiManager = _uiManager;
            optionsDocument = _optionsRef.Document;
        }

        public override void OnCleared()
        {
            uiManager = null;
            optionsDocument = null;
        }

        public override void OnViewReady(VisualElement _root)
        {
            openCounterBtn = _root.Q<Button>("open-counter-btn");
            openProfileBtn = _root.Q<Button>("open-profile-btn");
            openProfileListBtn = _root.Q<Button>("open-profile-list-btn");
            toggleOptionsBtn = _root.Q<Button>("toggle-options-btn");
        }

        public override void OnShow()
        {
            base.OnShow();

            Observable.FromEvent(_h => openCounterBtn.clicked += _h, _h => openCounterBtn.clicked -= _h)
                .Subscribe(_ => uiManager.Show<CounterWindowPresenter>().Forget())
                .AddTo(Disposables);

            Observable.FromEvent(_h => openProfileBtn.clicked += _h, _h => openProfileBtn.clicked -= _h)
                .Subscribe(_ => uiManager.Show<ProfileWindowPresenter>().Forget())
                .AddTo(Disposables);

            Observable.FromEvent(_h => openProfileListBtn.clicked += _h, _h => openProfileListBtn.clicked -= _h)
                .Subscribe(_ => uiManager.Show<ProfileListWindowPresenter>().Forget())
                .AddTo(Disposables);

            Observable.FromEvent(_h => toggleOptionsBtn.clicked += _h, _h => toggleOptionsBtn.clicked -= _h)
                .Subscribe(_ => ToggleOptions())
                .AddTo(Disposables);
        }

        private void ToggleOptions()
        {
            if (optionsPresenter != null)
            {
                uiManager.Hide(optionsPresenter);
                optionsPresenter = null;
                toggleOptionsBtn.text = "옵션 열기";
            }
            else
            {
                optionsPresenter = uiManager.Show<OptionsPanelPresenter>(optionsDocument);
                toggleOptionsBtn.text = "옵션 닫기";
            }
        }
    }
}
