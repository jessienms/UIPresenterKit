using System;
using R3;
using UnityEngine.UIElements;

namespace Samples
{
    public class ProfilePresenter : IDisposable
    {
        readonly SampleProfileModel model;
        readonly CompositeDisposable disposables = new();

        public ProfilePresenter(SampleProfileModel _model)
        {
            model = _model;
        }

        public void Bind(VisualElement _root)
        {
            var userNameLabel  = _root.Q<Label>("username-label");
            var levelLabel     = _root.Q<Label>("level-label");
            var nextProfileBtn = _root.Q<Button>("next-profile-btn");

            model.UserName
                .Subscribe(_name => userNameLabel.text = _name)
                .AddTo(disposables);

            model.Level
                .Subscribe(_level => levelLabel.text = $"Lv. {_level}")
                .AddTo(disposables);

            Observable.FromEvent(_h => nextProfileBtn.clicked += _h, _h => nextProfileBtn.clicked -= _h)
                .Subscribe(_ => model.NextProfile())
                .AddTo(disposables);
        }

        public void Dispose() => disposables.Dispose();
    }
}
