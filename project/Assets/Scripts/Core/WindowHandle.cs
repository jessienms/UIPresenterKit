using System;

namespace UILib
{
    public interface IWindowHandle
    {
        IWindowPresenter Presenter { get; }
        bool IsValid { get; }
    }

    public sealed class WindowHandle<T> : IWindowHandle where T : class, IWindowPresenter
    {
        private T presenter;

        public T Presenter
        {
            get
            {
                if (!IsValid) throw new ObjectDisposedException(nameof(WindowHandle<T>), "Handle is no longer valid. Call UIManager.Show<T>() again to get a new handle.");
                return presenter;
            }
        }

        IWindowPresenter IWindowHandle.Presenter => presenter;

        public bool IsValid { get; private set; } = true;

        internal WindowHandle(T _presenter) => presenter = _presenter;

        internal void Invalidate()
        {
            IsValid = false;
            presenter = null;
        }
    }
}
