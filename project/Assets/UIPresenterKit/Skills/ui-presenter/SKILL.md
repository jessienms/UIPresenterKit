---
name: ui-presenter
description: Unity UIToolkit UI 구현 스킬. 사용자가 원하는 UI를 UXML, USS, Presenter 코드로 만들어주는 작업에 항상 사용한다. UIPresenterKit의 PresenterBase, UIManager, Floating 등 프로젝트 패턴을 정확히 적용한다. UIToolkit, UXML, USS, Presenter, UIManager.Show, ShowAttached, PresenterBase 등의 키워드가 나오거나 "UI 만들어줘", "창 띄워줘", "팝업 추가해줘" 같은 요청이 오면 반드시 이 스킬을 사용한다.
---

# Unity UIToolkit UI 구현 스킬

이 스킬은 UIPresenterKit을 활용해 UI를 처음부터 끝까지 구현한다.

## 작업 순서

1. 사용자의 UI 요구사항 파악 (어떤 요소, 어디서 띄울지, 인자 필요 여부)
2. UXML 작성 (프로젝트의 UI 리소스 폴더)
3. USS 작성 (필요 시 기존 파일 확장 또는 신규)
4. Presenter 클래스 작성 (프로젝트의 Script 폴더 하위 적절한 위치)
5. 호출부 구현 (어떤 UIManager API로 띄울지)
6. PrefabRegistry 또는 Addressable 등록 안내

---

## 핵심 규칙

- UIManager는 VContainer scope 당 singleton이므로 Inject로 받는다
- **이벤트**는 `event Action` 대신 `Subject<T>` + R3 observable 패턴
- **씬 자산 연결**은 새 MonoBehaviour wrapper 금지 — 기존 LifetimeScope에 `[SerializeField]`로 노출

---

## UIPresenterKit 마운팅 방식 선택

```
원하는 UI 유형
├── 독립 창 (팝업, 전체화면)
│   └── [Window("key")] + UIManager.Show<T>()            ← 동적, 캐시됨
│
├── 씬에 배치된 UIDocument
│   └── UIManager.Show<T>(sceneDoc)                      ← 정적, 캐시 없음
│
├── 부모 UXML 안에 이미 배치된 VisualElement에 Presenter 부여 (슬롯·카드·패널 등)
│   └── UIManager.Show<T>(visualElement)                 ← 정적, 캐시 없음, [Window] 불필요
│       ※ new T() 직접 인스턴스화 금지 — DI·라이프사이클이 우회됨
│
├── UXML을 로드해 기존 VisualElement 하위에 새 자식으로 붙이기
│   └── [Window("key")] + UIManager.ShowAttached<T>(parent)  ← 동적, 캐시됨
│
├── ListView 슬롯
│   └── UIManager.BindListView<TSlot, TData>(listView, items)
│
└── 3D 월드 위에 떠있는 UI (HP바, 데미지 텍스트 등)
    └── ShowAttached + root.BindToWorld(transform, camera, opts)
```

**"정적 Show (VisualElement)" 선택 기준**: 부모 UXML에 `<ui:Instance template="..."/>` 로 이미 자리가 잡혀 있고 UXML을 런타임에 추가 로드하지 않아도 되는 경우. `[Window]` attribute 및 PrefabRegistry 등록이 **불필요**하다. Hide 시 캐시 없이 즉시 OnCleared + Dispose가 실행된다.

---

## Presenter 패턴

### 기본형 (인자 없음)

```csharp
using R3;
using UIPresenterKit;
using UnityEngine.UIElements;

[Window("<addressable-key>")]
internal sealed class <Name>Presenter : PresenterBase
{
    // 이벤트는 Subject<T> 사용
    private readonly Subject<Unit> confirmSubject = new();
    public Observable<Unit> OnConfirm => confirmSubject;

    private Button confirmButton;
    private Label titleLabel;

    public override void OnViewReady(VisualElement _root)
    {
        confirmButton = _root.Q<Button>("ConfirmButton");
        titleLabel = _root.Q<Label>("TitleLabel");
    }

    public override void OnShow()
    {
        base.OnShow(); // Disposables.Clear() 포함

        Observable.FromEvent(
                _h => confirmButton.clicked += _h,
                _h => confirmButton.clicked -= _h)
            .Subscribe(_ => confirmSubject.OnNext(Unit.Default))
            .AddTo(Disposables);
    }

    public override void OnCleared()
    {
        // OnInjected로 받은 dependency를 null로
    }

    public override void Dispose()
    {
        confirmSubject.Dispose();
        base.Dispose();
    }
}
```

### 인자 있는 형 (TArg 패턴)

```csharp
[Window("<key>")]
internal sealed class <Name>Presenter : PresenterBase<I<Name>Data>
{
    private Label nameLabel;

    public override void OnViewReady(VisualElement _root)
    {
        nameLabel = _root.Q<Label>("NameLabel");
    }

    public override void OnShow(I<Name>Data _data)
    {
        Disposables.Clear();
        nameLabel.text = _data.Name;

        _data.SomeReactive
            .Subscribe(_v => nameLabel.text = _v)
            .AddTo(Disposables);
    }

    public override void OnCleared() { }

    internal sealed class Args : IPresenterArgs<<Name>Presenter>
    {
        private readonly I<Name>Data data;
        public Args(I<Name>Data _data) => data = _data;
        public void InvokeOnShow(<Name>Presenter _p) => _p.OnShow(data);
    }
}
```

### VContainer 의존성 주입

```csharp
// Presenter 안에서 [Inject] 메서드로 받는다 (생성자 X)
[Inject]
public void OnInjected(ISomeService _service)
{
    this.service = _service;
}
```

---

## UIManager 호출 패턴

```csharp
// 동적 Show (await 필요)
var presenter = await uiManager.Show<MyPresenter>();

// 인자 있는 동적 Show
var presenter = await uiManager.Show(new MyPresenter.Args(someData));

// 정적 Show (씬 UIDocument)
var presenter = uiManager.Show<MyPresenter>(sceneUIDocument);

// 정적 Show (기존 VisualElement — [Window] 불필요, 캐시 없음)
var slot = uiManager.Show<SlotPresenter>(slotContainer, new SlotPresenter.Args(data));
uiManager.Hide(slot);

// ShowAttached (부모 VisualElement 아래에 UXML 마운트)
var presenter = await uiManager.ShowAttached<MyPresenter>(parentElement);

// Hide
uiManager.Hide(presenter);

// 자기 자신 숨기기 (Presenter 내부)
RequestHide();
```

> **슬롯 부모의 정리 패턴**: 부모가 OnShow마다 자식 슬롯 Presenter를 재생성하는 경우,
> 부모의 `OnHide()` override에서 각 슬롯을 `uiManager.Hide(child)` 로 정리한다.
>
> ```csharp
> public override void OnHide()
> {
>     foreach (var slot in slotPresenters) uiManager.Hide(slot);
>     slotPresenters.Clear();
>     base.OnHide();
> }
> ```

### 호출 후 이벤트 대기 패턴

```csharp
var presenter = await uiManager.Show<MyPresenter>();
var result = await presenter.OnConfirm.FirstAsync(cancellationToken);
uiManager.Hide(presenter);
```

---

## UXML 구조 템플릿

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements" editor-extension-mode="False">
    <ui:VisualElement name="Root" style="flex-grow: 1; justify-content: center; align-items: center;">
        <ui:VisualElement name="Panel">
            <ui:Label name="TitleLabel" text="제목"/>
            <!-- 내용 요소들 -->
            <ui:Button name="ConfirmButton" text="확인"/>
            <ui:Button name="CancelButton" text="취소"/>
        </ui:VisualElement>
    </ui:VisualElement>
</ui:UXML>
```

---

## 월드 공간 Floating UI

```csharp
// ShowAttached로 마운트 후 OnShow에서 BindToWorld 호출
public override void OnShow(Transform _target)
{
    Disposables.Clear();

    root.BindToWorld(_target, Camera.main, new FloatingOptions
    {
        mode = FloatingMode.Continuous,
        worldOffset = Vector3.up * 2.2f,
        pivot = new Vector2(0.5f, 1f),
        hideWhenBehindCamera = true,
    }).AddTo(Disposables);
}
```

**USS 필수 설정** (BindToWorld 사용 시):
```css
.floating-root {
    position: absolute;
    flex-grow: 0;
}
```

---

## PrefabRegistry 등록 안내

동적 `Show<T>()` / `ShowAttached<T>()` 사용 시 `[Window("key")]`의 key를:
- **Document 방식** → PrefabRegistry의 `entries`에 key + prefab(UIDocument가 붙은 GameObject) 등록
- **Element 방식** → PrefabRegistry의 `uxmlEntries`에 key + VisualTreeAsset 등록
- **Addressable** 사용 시 → Addressable Address를 `[Window("key")]`와 동일하게 설정

---

## 참고

- 상세 API: `references/uipresenterkit-api.md`
