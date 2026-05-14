using UnityEngine.UIElements;

namespace UILib
{
    internal abstract class PresenterInstanceBase
    {
        public string Key { get; }
        public IPresenter Presenter { get; }
        public bool IsPooled { get; }
        public bool IsViewReady { get; set; }
        public bool IsHidden { get; set; }

        protected PresenterInstanceBase(string _key, IPresenter _presenter, bool _isPooled)
        {
            Key = _key;
            Presenter = _presenter;
            IsPooled = _isPooled;
        }

        public abstract VisualElement GetRoot();
    }
}
