using ObservableCollections;
using R3;
using UIPresenterKit.Samples.Game;
using UIPresenterKit.Samples.Model;
using UnityEngine.UIElements;
using VContainer;

namespace UIPresenterKit.Samples.UI
{
    [Window("UI/MobSpawnWindow")]
    public sealed class MobSpawnPresenter : PresenterBase
    {
        private MobProcessor processor;
        private MobListModel model;

        private Button closeBtn;
        private Button spawnBtn;
        private Button removeBtn;
        private Label countLabel;

        [Inject]
        public void OnInjected(MobProcessor _processor, MobListModel _model)
        {
            processor = _processor;
            model = _model;
        }

        public override void OnCleared()
        {
            processor = null;
            model = null;
        }

        public override void OnViewReady(VisualElement _root)
        {
            closeBtn = _root.Q<Button>("close-btn");
            spawnBtn = _root.Q<Button>("spawn-btn");
            removeBtn = _root.Q<Button>("remove-btn");
            countLabel = _root.Q<Label>("count-label");
        }

        public override void OnShow()
        {
            base.OnShow();

            countLabel.text = $"현재: {model.Mobs.Count}마리";

            model.Mobs.ObserveCountChanged()
                .Subscribe(_c => countLabel.text = $"현재: {_c}마리")
                .AddTo(Disposables);

            Observable.FromEvent(_h => spawnBtn.clicked += _h, _h => spawnBtn.clicked -= _h)
                .Subscribe(_ => processor.SpawnOne())
                .AddTo(Disposables);

            Observable.FromEvent(_h => removeBtn.clicked += _h, _h => removeBtn.clicked -= _h)
                .Subscribe(_ => processor.RemoveOldest())
                .AddTo(Disposables);

            Observable.FromEvent(_h => closeBtn.clicked += _h, _h => closeBtn.clicked -= _h)
                .Subscribe(_ => RequestHide())
                .AddTo(Disposables);
        }
    }
}
