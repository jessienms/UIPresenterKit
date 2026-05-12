using System;
using R3;
using UnityEngine.UIElements;

namespace UILib
{
    /// <summary>
    /// [Window("key")] attribute 로 prefab 키를 지정한다.
    /// 빈 생성자 + [Inject] OnInjected(Models...) 메서드를 선언한다.
    /// OnShow/OnHide 는 UIManager 만 호출한다. 외부 hide 트리거는 RequestHide() 로 신호를 보낸다.
    /// </summary>
    public interface IWindowPresenter : IDisposable
    {
        Observable<Unit> HideRequested { get; }

        /// <summary>X 버튼 등 자체 hide 트리거 시 호출한다.</summary>
        void RequestHide();

        /// <summary>첫 활성화 직후 평생 1회. root.Q&lt;T&gt; 쿼리 결과를 멤버에 캐시한다.</summary>
        void OnViewReady(VisualElement _root);

        /// <summary>UIManager.Dispose 시 1회. OnInjected 에서 받은 dependency 를 모두 null 로 만든다.</summary>
        void OnDetached();

        /// <summary>매 Show. 임시 구독 시작, UI 상태 reset. UIManager 만 호출한다.</summary>
        void OnShow();

        /// <summary>매 Hide. 임시 구독 해제. UIManager 만 호출한다.</summary>
        void OnHide();
    }
}
