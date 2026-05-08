using System;
using R3;
using UnityEngine.UIElements;
using VContainer.Unity;

namespace Samples
{
    public class SampleWindowPresenter : IStartable, IDisposable
    {
        readonly SampleCounterModel counterModel;
        readonly ProfilePresenter profilePresenter;
        readonly UIDocument document;
        readonly CompositeDisposable disposables = new();

        public SampleWindowPresenter(
            SampleCounterModel _counterModel,
            ProfilePresenter _profilePresenter,
            UIDocument _document)
        {
            counterModel = _counterModel;
            profilePresenter = _profilePresenter;
            document = _document;
        }

        public void Start()
        {
            var root = document.rootVisualElement;

            var closeBtn     = root.Q<Button>("close-btn");
            var actionBtn    = root.Q<Button>("action-btn");
            var resetBtn     = root.Q<Button>("reset-btn");
            var counterLabel = root.Q<Label>("counter-label");

            profilePresenter.Bind(root.Q("profile-section"));

            counterModel.Count
                .Subscribe(_count => counterLabel.text = $"{_count}번 클릭")
                .AddTo(disposables);

            Observable.FromEvent(_h => actionBtn.clicked += _h, _h => actionBtn.clicked -= _h)
                .Subscribe(_ => counterModel.Increment())
                .AddTo(disposables);

            Observable.FromEvent(_h => resetBtn.clicked += _h, _h => resetBtn.clicked -= _h)
                .Subscribe(_ => counterModel.Reset())
                .AddTo(disposables);

            Observable.FromEvent(_h => closeBtn.clicked += _h, _h => closeBtn.clicked -= _h)
                .Subscribe(_ => document.gameObject.SetActive(false))
                .AddTo(disposables);
        }

        public void Dispose() => disposables.Dispose();
    }
}
