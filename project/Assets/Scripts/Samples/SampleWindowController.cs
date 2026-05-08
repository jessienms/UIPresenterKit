using UnityEngine;
using UnityEngine.UIElements;

namespace Samples
{
    /// <summary>
    /// UI Toolkit 기본 사용 예제.
    /// 같은 GameObject에 UIDocument 컴포넌트가 필요합니다.
    /// UIDocument의 Source Asset에 SampleWindow.uxml을 할당하세요.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class SampleWindowController : MonoBehaviour
    {
        private Button _closeBtn;
        private Button _actionBtn;
        private Label _counterLabel;
        private int _clickCount;

        private void Start()
        {
            // UIDocument.rootVisualElement 가 UXML 트리의 진입점입니다
            var root = GetComponent<UIDocument>().rootVisualElement;

            // Q<T>("name") 으로 UXML에서 name 속성으로 요소를 찾습니다
            _closeBtn     = root.Q<Button>("close-btn");
            _actionBtn    = root.Q<Button>("action-btn");
            _counterLabel = root.Q<Label>("counter-label");

            _closeBtn.clicked  += OnCloseClicked;
            _actionBtn.clicked += OnActionClicked;
        }

        private void OnDestroy()
        {
            // 이벤트 구독을 명시적으로 해제합니다
            if (_closeBtn  != null) _closeBtn.clicked  -= OnCloseClicked;
            if (_actionBtn != null) _actionBtn.clicked -= OnActionClicked;
        }

        private void OnCloseClicked()
        {
            // GameObject를 비활성화하면 UIDocument도 함께 숨겨집니다
            gameObject.SetActive(false);
        }

        private void OnActionClicked()
        {
            _clickCount++;
            _counterLabel.text = $"{_clickCount}번 클릭했습니다!";
        }
    }
}
