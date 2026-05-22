# UIPresenterKit 상세 API 레퍼런스

## PresenterBase 라이프사이클

```
신규:     OnInjected → OnViewReady → OnShow → OnHide (반복) → OnCleared → Dispose
재사용(1차 캐시): OnShow → OnHide (반복)
재사용(2차 풀):   OnInjected → OnShow → OnHide (반복) → OnCleared → Dispose
```

| 메서드 | 호출 시점 | 용도 |
|--------|-----------|------|
| `OnInjected(...)` | 최초 생성 / 풀 재사용 시 | VContainer DI 수신 |
| `OnViewReady(root)` | 최초 활성화 1회 | `root.Q<T>()` 캐시 |
| `OnShow()` / `OnShow(arg)` | 매 Show | 구독 시작, UI 초기화 |
| `OnHide()` | 매 Hide | `Disposables.Clear()` (base 처리) |
| `OnCleared()` | scope 종료 직전 | dependency null 처리 |
| `Dispose()` | 완전 해제 | Subject 등 Dispose |

## UIManager API 전체

```csharp
// 동적 (1차/2차 캐시 운용, [Window] 필수)
UniTask<T>          Show<T>()
UniTask<TPresenter> Show<TPresenter>(IPresenterArgs<TPresenter> args)
UniTask<T>          ShowAttached<T>(VisualElement parent)
UniTask<TPresenter> ShowAttached<TPresenter>(VisualElement parent, IPresenterArgs<TPresenter> args)
UniTask             Preload<T>()

// 정적 (캐시 없음, Hide 시 즉시 Dispose)
T           Show<T>(UIDocument sceneDoc)
TPresenter  Show<TPresenter>(UIDocument sceneDoc, IPresenterArgs<TPresenter> args)
T           Show<T>(VisualElement root)
TPresenter  Show<TPresenter>(VisualElement root, IPresenterArgs<TPresenter> args)

// 비활성화
void Hide(IPresenter presenter)

// ListView
UniTask<IDisposable> BindListView<TSlot, TData>(ListView listView, IList<TData> items)
```

## FloatingOptions

```csharp
new FloatingOptions
{
    mode = FloatingMode.Continuous,   // 또는 OneShot
    worldOffset = Vector3.up * 2f,    // 월드 오프셋
    screenOffset = Vector2.zero,      // 화면 픽셀 오프셋
    pivot = new Vector2(0.5f, 0.5f),  // (0,0)=좌상단, (0.5,1)=하단중앙
    hideWhenBehindCamera = true,
    hideWhenOffScreen = false,
}
```

## IPresenter 인터페이스

```csharp
Observable<Unit> HideRequested      // 자체 hide 신호 (UIManager가 구독)
Observable<Unit> OnHideAsObservable // 외부에서 hide 완료 감지용
void RequestHide()                  // Presenter 내부에서 자기 자신 숨기기
```

## PrefabRegistry 키 규칙

- `[Window("some-key")]` — UXML/prefab Addressable Address와 동일
- 대소문자 정확히 일치해야 런타임 로드 성공
- Document 방식(팝업 창): UIDocument 컴포넌트가 붙은 prefab
- Element 방식(ShowAttached): VisualTreeAsset(.uxml)
