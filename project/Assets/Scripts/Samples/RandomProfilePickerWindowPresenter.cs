using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UILib;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace Samples
{
    [Window("UI/RandomProfilePickerWindow")]
    public sealed class RandomProfilePickerWindowPresenter : PresenterBase
    {
        private UIManager uiManager;
        private SampleProfileModel model;

        private VisualElement slotArea;
        private Button addBtn;
        private Button removeBtn;
        private Button closeBtn;
        private Label countLabel;

        private readonly List<IPresenter> slots = new();
        private const int MaxSlots = 3;

        [Inject]
        public void OnInjected(UIManager _uiManager, SampleProfileModel _model)
        {
            uiManager = _uiManager;
            model = _model;
        }

        public override void OnViewReady(VisualElement _root)
        {
            slotArea   = _root.Q<VisualElement>("slot-area");
            addBtn     = _root.Q<Button>("add-btn");
            removeBtn  = _root.Q<Button>("remove-btn");
            closeBtn   = _root.Q<Button>("close-btn");
            countLabel = _root.Q<Label>("count-label");
        }

        public override void OnShow()
        {
            base.OnShow();
            UpdateButtons();

            Observable.FromEvent(_h => addBtn.clicked += _h, _h => addBtn.clicked -= _h)
                .Subscribe(_ => AddSlotAsync().Forget())
                .AddTo(Disposables);

            Observable.FromEvent(_h => removeBtn.clicked += _h, _h => removeBtn.clicked -= _h)
                .Subscribe(_ => RemoveSlot())
                .AddTo(Disposables);

            Observable.FromEvent(_h => closeBtn.clicked += _h, _h => closeBtn.clicked -= _h)
                .Subscribe(_ => RequestHide())
                .AddTo(Disposables);
        }

        public override void OnHide()
        {
            for (var i = slots.Count - 1; i >= 0; i--)
                uiManager.Detach(slots[i]);
            slots.Clear();
            base.OnHide();
        }

        public override void OnDetached()
        {
            uiManager  = null;
            model      = null;
            slotArea   = null;
            addBtn     = null;
            removeBtn  = null;
            closeBtn   = null;
            countLabel = null;
        }

        private async UniTaskVoid AddSlotAsync()
        {
            if (slots.Count >= MaxSlots) return;
            var profile = model.Profiles[Random.Range(0, model.Profiles.Count)];
            var slot = await uiManager.Attach<ProfileSlotPresenter>(slotArea, new ProfileSlotArgs(profile));
            slots.Add(slot);
            UpdateButtons();
        }

        private void RemoveSlot()
        {
            if (slots.Count == 0) return;
            var first = slots[0];
            slots.RemoveAt(0);
            uiManager.Detach(first);
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            addBtn.SetEnabled(slots.Count < MaxSlots);
            removeBtn.SetEnabled(slots.Count > 0);
            countLabel.text = $"{slots.Count} / {MaxSlots}";
        }
    }
}
