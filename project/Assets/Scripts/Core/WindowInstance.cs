using UnityEngine;

namespace UILib
{
    internal sealed class WindowInstance
    {
        public string Key { get; }
        public GameObject GameObject { get; }
        public IWindowPresenter Presenter { get; }
        public bool IsPooled { get; }
        public bool IsViewReady { get; set; }

        public WindowInstance(string _key, GameObject _gameObject, IWindowPresenter _presenter, bool _isPooled)
        {
            Key = _key;
            GameObject = _gameObject;
            Presenter = _presenter;
            IsPooled = _isPooled;
        }
    }
}
