using System;
using UnityEngine.UIElements;

namespace UIPresenterKit
{
    internal sealed class DocumentInstance : PresenterInstanceBase
    {
        public UIDocument Document { get; }

        public DocumentInstance(string _key, UIDocument _document, IPresenter _presenter, bool _isPooled)
            : base(_key, _presenter, _isPooled)
        {
            Document = _document ?? throw new ArgumentNullException(nameof(_document));
        }

        public override VisualElement GetRoot() => Document.rootVisualElement;
    }
}
