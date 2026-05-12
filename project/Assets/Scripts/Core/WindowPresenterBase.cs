using System;
using R3;
using UnityEngine.UIElements;

namespace UILib
{
    /// <summary>
    /// IWindowPresenter 를 구현하는 편의 기반 클래스.
    /// [Window("key")] attribute 와 [Inject] OnInjected(Models...) 메서드를 추가로 선언한다.
    ///
    /// 라이프사이클 순서:
    ///   (신규) OnInjected → OnViewReady → OnShow → OnHide (반복) → OnDetached → Dispose
    ///   (재사용, 1차 캐시) OnShow → OnHide (반복)
    ///   (재사용, 2차 풀)  OnInjected → OnShow → OnHide (반복) → OnDetached → Dispose
    /// </summary>
    public abstract class WindowPresenterBase : IWindowPresenter
    {
        private readonly Subject<Unit> hideRequested = new();
        private readonly Subject<Unit> onHideSubject = new();

        protected readonly CompositeDisposable Disposables = new();

        public Observable<Unit> HideRequested => hideRequested;
        public Observable<Unit> OnHideAsObservable => onHideSubject;

        /// <summary>X 버튼 등 자체 hide 트리거 시 호출한다.</summary>
        public void RequestHide() => hideRequested.OnNext(Unit.Default);

        // --- IWindowPresenter ---
        public virtual void OnViewReady(VisualElement _root) { }


        public virtual void OnDetached() { }

        public virtual void Dispose()
        {
            Disposables.Dispose();
            hideRequested.Dispose();
        }

        public virtual void OnShow()
        {
            Disposables.Clear();
        }

        public virtual void OnHide()
        {
            onHideSubject.OnNext(Unit.Default);
            Disposables.Clear();
        }

        public void AddTo(IDisposable _disposable)
        {
            Disposables.Add(_disposable);
        }
    }
}
