using System;
using R3;
using UnityEngine.UIElements;
using VContainer.Unity;

namespace Samples
{
    public class SampleWindow2Presenter : IStartable, IDisposable
    {
        readonly SampleProfileModel profileModel;
        readonly ProfilePresenter profilePresenter;
        readonly UIDocument document;
        readonly CompositeDisposable disposables = new();

        public SampleWindow2Presenter(
            SampleProfileModel _profileModel,
            ProfilePresenter _profilePresenter,
            UIDocument _document)
        {
            profileModel = _profileModel;
            profilePresenter = _profilePresenter;
            document = _document;
        }

        public void Start()
        {
            var root = document.rootVisualElement;

            var closeBtn      = root.Q<Button>("close-btn");
            var greetingLabel = root.Q<Label>("greeting-label");

            profilePresenter.Bind(root.Q("profile-section"));

            profileModel.UserName
                .CombineLatest(profileModel.Level, (_name, _level) => $"안녕하세요, {_name}님! (Lv. {_level})")
                .Subscribe(_text => greetingLabel.text = _text)
                .AddTo(disposables);

            Observable.FromEvent(_h => closeBtn.clicked += _h, _h => closeBtn.clicked -= _h)
                .Subscribe(_ => document.gameObject.SetActive(false))
                .AddTo(disposables);
        }

        public void Dispose() => disposables.Dispose();
    }
}
