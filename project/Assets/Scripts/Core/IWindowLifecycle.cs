namespace UILib
{
    /// <summary>
    /// UIManager 전용 활성화/비활성화 인터페이스.
    /// Explicit interface implementation 으로 선언해 외부 직접 호출을 차단한다.
    /// </summary>
    public interface IWindowLifecycle
    {
        /// <summary>매 Show. 임시 구독 시작, UI 상태 reset. UIManager 만 호출한다.</summary>
        void Show();

        /// <summary>매 Hide. 임시 구독 해제. UIManager 만 호출한다.</summary>
        void Hide();
    }
}
