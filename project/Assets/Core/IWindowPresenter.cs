using System;
using R3;
using UnityEngine.UIElements;

namespace UIPresenterKit.Core
{
    /// <summary>
    /// [Window("key")] attribute 로 prefab 키를 지정한다.
    /// 빈 생성자 + [Inject] OnInjected(Models...) 메서드를 선언한다.
    /// OnShow/OnHide 는 UIManager 만 호출한다. 외부 hide 트리거는 RequestHide() 로 신호를 보낸다.
    /// </summary>
    public interface IPresenter : IDisposable
    {
        Observable<Unit> HideRequested { get; }

        /// <summary>Hide 완료 시점에 발행. 외부에서 hide 이벤트를 관찰할 때 사용한다.</summary>
        Observable<Unit> OnHideAsObservable { get; }

        /// <summary>X 버튼 등 자체 hide 트리거 시 호출한다.</summary>
        void RequestHide();

        /// <summary>첫 활성화 직후 평생 1회. root.Q&lt;T&gt; 쿼리 결과를 멤버에 캐시한다.</summary>
        void OnViewReady(VisualElement _root);

        /// <summary>UIManager.Dispose 시 1회. OnInjected 에서 받은 dependency 를 모두 null 로 만든다.</summary>
        void OnCleared();

        /// <summary>매 Show. 임시 구독 시작, UI 상태 reset. UIManager 만 호출한다.</summary>
        void OnShow();

        /// <summary>매 Hide. 임시 구독 해제. UIManager 만 호출한다.</summary>
        void OnHide();
    }

    /// <summary>
    /// TPresenter 를 Owner 로 선언하는 Args bundle struct 가 구현한다.
    /// UIManager.Show(args) 의 파라미터 타입이 IPresenterArgs&lt;TPresenter&gt; 이므로
    /// 컴파일러가 TPresenter 를 직접 추론한다.
    /// </summary>
    public interface IPresenterArgs<TPresenter> where TPresenter : class, IPresenter, new()
    {
        /// <summary>presenter.OnShow(this) 를 호출한다. UIManager 만 사용한다.</summary>
        void InvokeOnShow(TPresenter _presenter);
    }

    /// <summary>
    /// Show 시점에 외부 인자를 전달받는 Presenter.
    /// UIManager.Show(args) 로 활성화한다. TArg 는 IPresenterArgs&lt;TSelf&gt; 를 구현하는 struct 다.
    /// </summary>
    public interface IPresenter<in TArg> : IPresenter
    {
        /// <summary>인자와 함께 매 Show. UIManager 만 호출한다.</summary>
        void OnShow(TArg _arg);
    }
}
