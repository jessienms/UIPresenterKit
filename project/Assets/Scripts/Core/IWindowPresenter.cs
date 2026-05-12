using System;
using R3;
using UnityEngine.UIElements;

namespace UILib
{
    /// <summary>
    /// [Window("key")] attribute 로 prefab 키를 지정한다.
    /// 빈 생성자 + [Inject] OnInjected(Models...) 메서드를 선언한다.
    /// Show/Hide 는 UIManager 만 호출한다. 외부 close 트리거는 CloseRequested.OnNext() 로 신호를 보낸다.
    /// </summary>
    public interface IWindowPresenter : IDisposable
    {
        Observable<Unit> CloseRequested { get; }

        /// <summary>첫 SetActive(true) 직후 평생 1회. root.Q&lt;T&gt; 쿼리 결과를 멤버에 캐시한다.</summary>
        void OnViewReady(UIDocument _doc);

        /// <summary>매 Show. 임시 구독 시작, UI 상태 reset. UIManager 만 호출한다.</summary>
        void Show();

        /// <summary>매 Hide. 임시 구독 해제. UIManager 만 호출한다.</summary>
        void Hide();

        /// <summary>UIManager.Dispose 시 1회. OnInjected 에서 받은 dependency 를 모두 null 로 만든다.</summary>
        void OnDetached();
    }
}
