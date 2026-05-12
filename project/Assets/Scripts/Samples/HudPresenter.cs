using Cysharp.Threading.Tasks;
using R3;
using UILib;
using UnityEngine.UIElements;
using VContainer;

namespace Samples
{
    public sealed class HudPresenter : WindowPresenterBase
    {
        private UIManager uiManager;

        private Button openCounterBtn;
        private Button openProfileBtn;

        [Inject]
        public void OnInjected(UIManager _uiManager)
        {
            uiManager = _uiManager;
        }

        public override void OnViewReady(UIDocument _doc)
        {
            var root = _doc.rootVisualElement;
            openCounterBtn = root.Q<Button>("open-counter-btn");
            openProfileBtn = root.Q<Button>("open-profile-btn");
        }

        protected override void OnShow()
        {
            Observable.FromEvent(_h => openCounterBtn.clicked += _h, _h => openCounterBtn.clicked -= _h)
                .Subscribe(_ => uiManager.Show<CounterWindowPresenter>().Forget())
                .AddTo(disposables);

            Observable.FromEvent(_h => openProfileBtn.clicked += _h, _h => openProfileBtn.clicked -= _h)
                .Subscribe(_ => uiManager.Show<ProfileWindowPresenter>().Forget())
                .AddTo(disposables);
        }

        public override void OnDetached()
        {
            uiManager = null;
        }
    }
}
