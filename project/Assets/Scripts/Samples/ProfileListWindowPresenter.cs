using R3;
using UILib;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace Samples
{
    [Window("UI/ProfileListWindow")]
    public sealed class ProfileListWindowPresenter : WindowPresenterBase
    {
        private SampleProfileModel profileModel;
        private Button closeBtn;
        private Label countLabel;
        private ListView profileList;
        private VisualTreeAsset slotTemplate;

        [Inject]
        public void OnInjected(SampleProfileModel _profileModel)
        {
            profileModel = _profileModel;
        }

        public override void OnViewReady(VisualElement _root)
        {
            closeBtn = _root.Q<Button>("close-btn");
            countLabel = _root.Q<Label>("count-label");
            profileList = _root.Q<ListView>("profile-list");
            slotTemplate = Resources.Load<VisualTreeAsset>("UI/ProfileListSlot");

            if (slotTemplate == null)
            {
                Debug.LogError("[Samples] Resources/UI/ProfileListSlot.uxml 을 찾을 수 없습니다.");
                return;
            }

            profileList.fixedItemHeight = 76;
            profileList.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
            profileList.makeItem = MakeItem;
            profileList.bindItem = BindItem;
            profileList.unbindItem = UnbindItem;
            profileList.selectionType = SelectionType.Single;
        }

        public override void OnShow()
        {
            base.OnShow();

            countLabel.text = $"{profileModel.Profiles.Count} profiles";
            profileList.itemsSource = profileModel.Profiles;
            profileList.Rebuild();

            Observable.FromEvent(_h => closeBtn.clicked += _h, _h => closeBtn.clicked -= _h)
                .Subscribe(_ => RequestHide())
                .AddTo(Disposables);
        }

        public override void OnDetached()
        {
            profileModel = null;
            closeBtn = null;
            countLabel = null;
            profileList = null;
            slotTemplate = null;
        }

        private VisualElement MakeItem()
        {
            var slot = slotTemplate.CloneTree();
            slot.style.flexGrow = 1;
            slot.userData = new SlotView(slot);
            return slot;
        }

        private void BindItem(VisualElement _element, int _index)
        {
            if (_element.userData is not SlotView slotView)
            {
                slotView = new SlotView(_element);
                _element.userData = slotView;
            }

            var profile = profileModel.Profiles[_index];
            slotView.IdLabel.text = $"#{profile.Id:000}";
            slotView.NameLabel.text = profile.Name;
            slotView.MetaLabel.text = $"Lv. {profile.Level} / {profile.Role}";
            slotView.StatusLabel.text = profile.Status;
        }

        private static void UnbindItem(VisualElement _element, int _index)
        {
            if (_element.userData is not SlotView slotView)
            {
                return;
            }

            slotView.IdLabel.text = string.Empty;
            slotView.NameLabel.text = string.Empty;
            slotView.MetaLabel.text = string.Empty;
            slotView.StatusLabel.text = string.Empty;
        }

        private sealed class SlotView
        {
            public Label IdLabel { get; }
            public Label NameLabel { get; }
            public Label MetaLabel { get; }
            public Label StatusLabel { get; }

            public SlotView(VisualElement _root)
            {
                IdLabel = _root.Q<Label>("profile-id");
                NameLabel = _root.Q<Label>("profile-name");
                MetaLabel = _root.Q<Label>("profile-meta");
                StatusLabel = _root.Q<Label>("profile-status");
            }
        }
    }
}
