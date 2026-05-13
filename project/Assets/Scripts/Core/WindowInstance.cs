using System;
using UnityEngine.UIElements;

namespace UILib
{
    internal sealed class WindowInstance
    {
        public string Key { get; }
        public UIDocument Document { get; }
        public VisualElement Root { get; private set; }
        public IPresenter Presenter { get; }
        public bool IsPooled { get; }
        public bool IsViewReady { get; set; }

        public WindowInstance(string _key, UIDocument _document, IPresenter _presenter, bool _isPooled)
            : this(_key, _document, null, _presenter, _isPooled)
        {
        }

        public WindowInstance(string _key, VisualElement _root, IPresenter _presenter, bool _isPooled)
            : this(_key, null, _root, _presenter, _isPooled)
        {
        }

        private WindowInstance(string _key, UIDocument _document, VisualElement _root, IPresenter _presenter, bool _isPooled)
        {
            if (_document == null && _root == null)
            {
                throw new ArgumentException("[UILib] WindowInstance 는 UIDocument 또는 VisualElement root 중 하나가 필요합니다.");
            }

            if (_presenter == null)
            {
                throw new ArgumentNullException(nameof(_presenter));
            }

            Key = _key;
            Document = _document;
            Root = _root;
            Presenter = _presenter;
            IsPooled = _isPooled;
        }

        public VisualElement GetRoot()
        {
            var root = Root ?? Document?.rootVisualElement;
            if (root == null)
            {
                throw new InvalidOperationException("[UILib] Window root VisualElement 를 찾을 수 없습니다.");
            }

            Root = root;
            return root;
        }
    }
}
