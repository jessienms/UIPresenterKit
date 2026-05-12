using R3;
using UnityEngine.UIElements;

namespace UILib
{
    /// <summary>
    /// IWindowPresenter + IWindowLifecycle 를 구현하는 편의 기반 클래스.
    /// [Window("key")] attribute 와 [Inject] OnInjected(Models...) 메서드를 추가로 선언한다.
    ///
    /// 라이프사이클 순서:
    ///   (신규) OnInjected → OnViewReady → OnShow → OnHide (반복) → OnDetached → Dispose
    ///   (재사용, 1차 캐시) OnShow → OnHide (반복)
    ///   (재사용, 2차 풀)  OnInjected → OnShow → OnHide (반복) → OnDetached → Dispose
    /// </summary>
    public abstract class WindowPresenterBase : IWindowPresenter, IWindowLifecycle
    {
        private readonly Subject<Unit> closeRequested = new();

        protected readonly CompositeDisposable disposables = new();

        public Observable<Unit> CloseRequested => closeRequested;

        /// <summary>X 버튼 등 자체 close 트리거 시 호출한다.</summary>
        protected void RequestClose() => closeRequested.OnNext(Unit.Default);

        // --- IWindowPresenter ---

        public virtual void OnViewReady(UIDocument _doc) { }

        public virtual void OnDetached() { }

        public virtual void Dispose()
        {
            disposables.Dispose();
            closeRequested.Dispose();
        }

        // --- IWindowLifecycle (UIManager 전용) ---

        void IWindowLifecycle.Show()
        {
            disposables.Clear();
            OnShow();
        }

        void IWindowLifecycle.Hide() => OnHide();

        // --- 사용자 오버라이드 대상 ---

        protected virtual void OnShow() { }

        protected virtual void OnHide()
        {
            disposables.Clear();
        }
    }
}
