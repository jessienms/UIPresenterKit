using System;
using UnityEngine.UIElements;

namespace UILib
{
    internal sealed class WindowInstance
    {
        public string Key { get; }
        public UIDocument Document { get; }
        public IWindowPresenter Presenter { get; }
        public bool IsPooled { get; }
        public bool IsViewReady { get; set; }

        public WindowInstance(string _key, UIDocument _document, IWindowPresenter _presenter, bool _isPooled)
        {
            if (_document == null)
            {
                throw new ArgumentNullException(nameof(_document));
            }

            if (_presenter == null)
            {
                throw new ArgumentNullException(nameof(_presenter));
            }

            Key = _key;
            Document = _document;
            Presenter = _presenter;
            IsPooled = _isPooled;
        }
    }
}
