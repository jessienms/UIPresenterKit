using System;
using UnityEngine.UIElements;

namespace UILib
{
    internal sealed class ElementInstance : PresenterInstanceBase
    {
        public VisualElement Root { get; }

        public ElementInstance(string _key, VisualElement _root, IPresenter _presenter, bool _isPooled)
            : base(_key, _presenter, _isPooled)
        {
            Root = _root ?? throw new ArgumentNullException(nameof(_root));
        }

        public override VisualElement GetRoot() => Root;
    }
}
