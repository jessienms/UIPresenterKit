using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UILib
{
    internal sealed class SlotBinding<TSlot, TData> : IDisposable
        where TSlot : class, IPresenter<TData>, new()
    {
        private readonly UIManager manager;
        private readonly ListView listView;
        private readonly IList<TData> items;
        private readonly string key;
        private readonly VisualTreeAsset uxml;
        private readonly HashSet<AttachedInstance> active = new();
        private bool disposed;

        internal SlotBinding(
            UIManager _manager,
            ListView _listView,
            IList<TData> _items,
            string _key,
            VisualTreeAsset _uxml)
        {
            manager = _manager;
            listView = _listView;
            items = _items;
            key = _key;
            uxml = _uxml;
        }

        internal void Install()
        {
            listView.itemsSource = (IList)items;
            listView.makeItem = MakeItem;
            listView.bindItem = BindItem;
            listView.unbindItem = UnbindItem;
            listView.destroyItem = DestroyItem;
            listView.Rebuild();
        }

        private VisualElement MakeItem()
        {
            var inst = manager.AcquireSlot<TSlot>(key, uxml);
            active.Add(inst);
            inst.Root.userData = inst;
            return inst.Root;
        }

        private void BindItem(VisualElement _element, int _index)
        {
            var inst = (AttachedInstance)_element.userData;
            inst.IsHidden = false;
            ((IPresenter<TData>)inst.Presenter).OnShow(items[_index]);
        }

        private static void UnbindItem(VisualElement _element, int _index)
        {
            var inst = (AttachedInstance)_element.userData;
            if (inst.IsHidden) return;
            inst.Presenter.OnHide();
            inst.IsHidden = true;
        }

        private void DestroyItem(VisualElement _element)
        {
            var inst = (AttachedInstance)_element.userData;
            if (!inst.IsHidden)
            {
                inst.Presenter.OnHide();
                inst.IsHidden = true;
            }
            active.Remove(inst);
            manager.ReleaseSlot(key, inst);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            listView.makeItem = null;
            listView.bindItem = null;
            listView.unbindItem = null;
            listView.destroyItem = null;
            listView.itemsSource = null;

            foreach (var inst in active)
            {
                if (!inst.IsHidden)
                {
                    inst.Presenter.OnHide();
                    inst.IsHidden = true;
                }
                manager.ReleaseSlot(key, inst);
            }
            active.Clear();
        }
    }
}
