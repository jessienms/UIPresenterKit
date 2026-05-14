using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UIPresenterKit.Core;
using UnityEngine.UIElements;
using VContainer;

namespace UIPresenterKit.Samples
{
    [Window("UI/ProfileListWindow")]
    public sealed class ProfileListWindowPresenter : PresenterBase
    {
        private UIManager uiManager;
        private SampleProfileModel profileModel;
        private Button closeBtn;
        private Button randomPickBtn;
        private Label countLabel;
        private ListView profileList;

        [Inject]
        public void OnInjected(UIManager _uiManager, SampleProfileModel _profileModel)
        {
            uiManager = _uiManager;
            profileModel = _profileModel;
        }

        public override void OnViewReady(VisualElement _root)
        {
            closeBtn = _root.Q<Button>("close-btn");
            randomPickBtn = _root.Q<Button>("random-pick-btn");
            countLabel = _root.Q<Label>("count-label");
            profileList = _root.Q<ListView>("profile-list");

            profileList.fixedItemHeight = 76;
            profileList.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
            profileList.selectionType = SelectionType.Single;
        }

        public override void OnShow()
        {
            base.OnShow();

            countLabel.text = $"{profileModel.Profiles.Count} profiles";

            BindSlotListAsync().Forget();

            Observable.FromEvent<IEnumerable<object>>(
                    _h => profileList.itemsChosen += _h,
                    _h => profileList.itemsChosen -= _h)
                .Subscribe(_items =>
                {
                    foreach (var item in _items)
                    {
                        if (item is SampleProfileData profile)
                        {
                            uiManager.Show(new ProfileDetailArgs(profile)).Forget();
                            break;
                        }
                    }
                })
                .AddTo(Disposables);

            Observable.FromEvent(_h => randomPickBtn.clicked += _h, _h => randomPickBtn.clicked -= _h)
                .Subscribe(_ => uiManager.Show<RandomProfilePickerWindowPresenter>().Forget())
                .AddTo(Disposables);

            Observable.FromEvent(_h => closeBtn.clicked += _h, _h => closeBtn.clicked -= _h)
                .Subscribe(_ => RequestHide())
                .AddTo(Disposables);
        }

        public override void OnCleared()
        {
            uiManager = null;
            profileModel = null;
            closeBtn = null;
            randomPickBtn = null;
            countLabel = null;
            profileList = null;
        }

        private async UniTaskVoid BindSlotListAsync()
        {
            var binding = await uiManager.BindListView<ProfileSlotPresenter, SampleProfileData>(
                profileList, profileModel.Profiles);
            binding.DisposeOnHide(this);
        }
    }
}
