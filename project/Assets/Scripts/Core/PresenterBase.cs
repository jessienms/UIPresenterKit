using System;
using R3;
using UnityEngine.UIElements;
using Unit = R3.Unit;

namespace UIPresenterKit.Core
{
    /// <summary>
    /// IPresenter 를 구현하는 편의 기반 클래스.
    /// [Window("key")] attribute 와 [Inject] OnInjected(Models...) 메서드를 추가로 선언한다.
    ///
    /// 라이프사이클 순서:
    ///   (신규) OnInjected → OnViewReady → OnShow → OnHide (반복) → OnCleared → Dispose
    ///   (재사용, 1차 캐시) OnShow → OnHide (반복)
    ///   (재사용, 2차 풀)  OnInjected → OnShow → OnHide (반복) → OnCleared → Dispose
    /// </summary>
    public abstract class PresenterBase : IPresenter
    {
        private readonly Subject<Unit> hideRequested = new();
        private readonly Subject<Unit> onHideSubject = new();

        protected readonly CompositeDisposable Disposables = new();

        public Observable<Unit> HideRequested => hideRequested;
        public Observable<Unit> OnHideAsObservable => onHideSubject;

        /// <summary>X 버튼 등 자체 hide 트리거 시 호출한다.</summary>
        public void RequestHide() => hideRequested.OnNext(Unit.Default);

        public virtual void OnViewReady(VisualElement _root) { }

        public virtual void OnCleared() { }

        public virtual void Dispose()
        {
            Disposables.Dispose();
            hideRequested.Dispose();
            onHideSubject.Dispose();
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

    /// <summary>
    /// 인자를 받는 Presenter 용 기반 클래스.
    /// UIManager.Show&lt;TPresenter, TArg&gt;(arg) 로만 활성화해야 한다.
    /// 인자 없는 Show 를 호출하면 InvalidOperationException 이 발생한다.
    /// </summary>
    public abstract class PresenterBase<TArg> : PresenterBase, IPresenter<TArg>
    {
        public sealed override void OnShow()
        {
            throw new InvalidOperationException(
                $"{GetType().Name} requires an argument. Use UIManager.Show(args) where args implements IPresenterArgs<{GetType().Name}>.");
        }

        public virtual void OnShow(TArg _arg)
        {
            Disposables.Clear();
        }
    }
}
