using UnityEngine;

namespace UILib
{
    internal sealed class WindowPair
    {
        public string Key { get; }
        public GameObject Go { get; }
        public IWindowPresenter Presenter { get; }
        public bool IsPooled { get; }

        public WindowPair(string _key, GameObject _go, IWindowPresenter _presenter, bool _isPooled)
        {
            Key = _key;
            Go = _go;
            Presenter = _presenter;
            IsPooled = _isPooled;
        }
    }
}
