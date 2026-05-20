# UI Presenter Kit

Unity **UI Toolkit** 기반 프로젝트를 위한 MVP 패턴 UI 프레임워크 라이브러리.  
표준화된 presenter 를 작성할 수 있게 도와줍니다.
**VContainer**로 의존성을 주입하고, **R3**로 반응형 UI 상태를 관리합니다.
리소스 풀링 기능으로 presenter 생성시 부담을 줄여줍니다.

## 특징

- **MVP 패턴**: Presenter가 View(VisualElement)와 Model을 연결하며, VContainer가 의존성을 자동 주입
- **다양한 마운팅 방식**: Prefab 동적 스폰 / 씬 내 UIDocument 바인딩 / VisualElement 자식으로 동적 추가
- **2단계 캐싱**: 인스턴스 캐시 + 오브젝트 풀로 GC 부담 최소화
- **Floating UI**: 3D 월드 좌표 추적 UI (HP 바, 데미지 텍스트 등) 지원
- **ListView 바인딩**: 슬롯 Presenter의 생성·재사용·정리를 자동 관리

## 요구 사항

| 패키지 | 버전 |
|---|---|
| Unity | 6000.3 이상 |
| VContainer (`jp.hadashikick.vcontainer`) | ≥ 1.17.0 |
| R3 (`com.cysharp.r3`) | ≥ 1.3.0 |
| UniTask (`com.cysharp.unitask`) | ≥ 2.5.10 |

VContainer, R3, UniTask는 [OpenUPM](https://openupm.com)을 통해 설치할 수 있습니다.

```json
// Packages/manifest.json
{
  "scopedRegistries": [
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": [
        "jp.hadashikick",
        "com.cysharp"
      ]
    }
  ]
}
```

## 설치

Unity **Package Manager → Add package from git URL**에 아래 URL을 입력합니다.

```
https://internal.skeinglobe.com/git/sg/UIPresenterKit.git?path=/project/Assets/UIPresenterKit
```

또는 `Packages/manifest.json`에 직접 추가합니다.

```json
{
  "dependencies": {
    "com.sg.uipresenterkit": "https://internal.skeinglobe.com/git/sg/UIPresenterKit.git?path=/project/Assets/UIPresenterKit"
  }
}
```

---

## 셋업

### 1. IAssetLoader 구현

UIPresenterKit은 Presenter Prefab과 UXML을 키 문자열로 로드하기 위해 `IAssetLoader` 인터페이스를 사용합니다. **프로젝트의 에셋 관리 방식에 맞춰 직접 구현**이 필요합니다.

```csharp
public interface IAssetLoader
{
    UniTask<GameObject> LoadAsync(string key);
    UniTask<VisualTreeAsset> LoadUxmlAsync(string key);
}
```

#### 내장 구현체 — PrefabRegistry (ScriptableObject)

Inspector에서 키와 에셋을 직접 매핑하는 가장 단순한 구현체입니다. 소규모 프로젝트나 빠른 프로토타이핑에 적합합니다.

1. `Assets > Create > UILib > PrefabRegistry`로 ScriptableObject 생성
2. `Entries` 배열에 key → Prefab 매핑, `UxmlEntries` 배열에 key → UXML 매핑 추가
3. 앱 루트 LifetimeScope의 Inspector 필드에 할당

#### 커스텀 구현 — Addressables

Addressables나 에셋 번들 등 별도 로딩 시스템을 사용할 경우 `IAssetLoader`를 직접 구현합니다.

```csharp
public sealed class AddressableAssetLoader : IAssetLoader
{
    public async UniTask<GameObject> LoadAsync(string key)
        => await Addressables.LoadAssetAsync<GameObject>(key).ToUniTask();

    public async UniTask<VisualTreeAsset> LoadUxmlAsync(string key)
        => await Addressables.LoadAssetAsync<VisualTreeAsset>(key).ToUniTask();
}
```

### 2. LifetimeScope 구성 예시

아래의 예시는 LifetimeScope는 **앱 루트**와 **씬** 두 계층으로 설명합니다.

**앱 루트 LifetimeScope** (씬 전환 시 유지):

```csharp
public sealed class AppScope : LifetimeScope
{
    [SerializeField] PrefabRegistry prefabRegistry; // IAssetLoader 구현체 할당

    protected override void Configure(IContainerBuilder builder)
    {
        // PrefabRegistry 사용 시
        builder.RegisterInstance<IAssetLoader>(prefabRegistry);

        // Addressables 사용 시
        // builder.Register<AddressableAssetLoader>(Lifetime.Singleton).AsImplementedInterfaces();

        builder.Register<UIPoolingManager>(Lifetime.Singleton);
		
		// 전역적인 UIManager
        builder.Register<UIManager>(Lifetime.Singleton);
    }
}
```

**씬 LifetimeScope** (ParentLifetimeScope를 AppScope로 지정):

```csharp
public sealed class MainSceneScope : LifetimeScope
{
    [SerializeField] UIDocument menuDocument;

    protected override void Configure(IContainerBuilder builder)
    {
        // 한정된 범위의 UIManager (현재는 MainScene 의 생명주기와 일치)
        builder.Register<UIManager>(Lifetime.Singleton);
		
		...
    }
}
```

### 3. UI Manager 사용 예시

```csharp
public sealed class MainSceneEntryPoint : IStartable
{
    private readonly UIManager uiManager;
    private readonly UIDocument menuDocument;

    public MainSceneEntryPoint(UIManager _uiManager, UIDocument _menuDocument)
    {
        uiManager = _uiManager;
        menuDocument = _menuDocument;
    }

    public void Start() => InitAsync().Forget();

    private async UniTaskVoid InitAsync()
    {
        // 첫 호출 응답성을 위해 사전 캐시
        await uiManager.Preload<ProfileWindowPresenter>();

        // 정적 UIDocument 바인딩
        uiManager.Show<MenuPresenter>(menuDocument);
    }
}
```

---

## 사용 방법

### Presenter 제작

모든 Presenter는 `PresenterBase` 또는 `PresenterBase<TArg>`를 상속하고, `[Window]` 어트리뷰트로 에셋 키를 지정합니다.

#### 기본 Presenter (인자 없음)

```csharp
[Window("UI/ProfileWindow")] // IAssetLoader에 등록한 key와 일치해야 함
public sealed class ProfileWindowPresenter : PresenterBase
{
    private UIManager uiManager;
    private Label nameLabel;
    private Button closeBtn;

    [Inject]
    public void OnInjected(UIManager uiManager)
        => this.uiManager = uiManager;

    // 최초 생성 시 1회 호출 — VisualElement 참조를 여기서 캐싱
    public override void OnViewReady(VisualElement root)
    {
        nameLabel = root.Q<Label>("name-label");
        closeBtn  = root.Q<Button>("close-btn");
    }

    // 매 활성화 시 호출
    public override void OnShow()
    {
        base.OnShow(); // Disposables 초기화 (필수 호출)

        Observable.FromEvent(h => closeBtn.clicked += h, h => closeBtn.clicked -= h)
            .Subscribe(_ => RequestHide()) // 자신을 닫도록 UIManager에 요청
            .AddTo(Disposables);           // OnHide 시 자동 해제
    }
}
```

#### 인자를 받는 Presenter

```csharp
// 1. 인자 struct 정의
public readonly struct ProfileDetailArgs : IPresenterArgs<ProfileDetailPresenter>
{
    public string Name  { get; }
    public int    Level { get; }

    public ProfileDetailArgs(string name, int level)
    {
        Name  = name;
        Level = level;
    }

    public void InvokeOnShow(ProfileDetailPresenter presenter)
        => presenter.OnShow(this);
}

// 2. Presenter 구현
[Window("UI/ProfileDetail")]
public sealed class ProfileDetailPresenter : PresenterBase<ProfileDetailArgs>
{
    private Label nameLabel;
    private Label levelLabel;

    public override void OnViewReady(VisualElement root)
    {
        nameLabel  = root.Q<Label>("name-label");
        levelLabel = root.Q<Label>("level-label");
    }

    public override void OnShow(ProfileDetailArgs args)
    {
        base.OnShow(args); // 필수 호출

        nameLabel.text  = args.Name;
        levelLabel.text = $"Lv. {args.Level}";
    }
}
```

---

### UIManager로 UI 표현하기

#### 동적 Show — Prefab 스폰 (캐시/풀 재사용)

```csharp
// 인자 없음
var presenter = await uiManager.Show<ProfileWindowPresenter>();

// 인자 있음
var detail = await uiManager.Show(new ProfileDetailArgs("홍길동", 42));

// 닫기
uiManager.Hide(presenter);
```

#### 정적 Show — 씬 내 UIDocument 바인딩

씬에 배치된 UIDocument를 그대로 사용합니다. `Hide` 시 캐싱 없이 즉시 정리됩니다.

```csharp
// 인자 없음
uiManager.Show<MenuPresenter>(menuDocument);

// 인자 있음
uiManager.Show(menuDocument, new MenuArgs(title));
```

#### ShowAttached — VisualElement 자식으로 추가

UXML을 지정한 부모 VisualElement의 자식으로 추가합니다. `Hide` 시 부모에서 분리 후 캐시로 회수됩니다.

```csharp
// 자식으로 추가
var slot = await uiManager.ShowAttached<ProfileSlotPresenter>(listContainer);

// 인자 있음
var slot = await uiManager.ShowAttached(listContainer, new ProfileSlotArgs(profileData));

// 분리 + 캐시 회수
uiManager.Hide(slot);
```

#### ListView 바인딩

슬롯 Presenter의 생성, 데이터 연결, 정리를 자동으로 관리합니다.

```csharp
// 슬롯 Presenter는 PresenterBase<TData>를 상속
[Window("UI/ProfileSlot")]
public sealed class ProfileSlotPresenter : PresenterBase<ProfileData>
{
    private Label nameLabel;

    public override void OnViewReady(VisualElement root)
        => nameLabel = root.Q<Label>("name");

    public override void OnShow(ProfileData data)
    {
        base.OnShow(data);
        nameLabel.text = data.Name;
    }
}

// 바인딩
var binding = await uiManager.BindListView<ProfileSlotPresenter, ProfileData>(
    listView, profileModel.Profiles);

binding.DisposeOnHide(this); // 부모 Presenter hide 시 자동 정리
```

#### 사전 로드 (Preload)

첫 `Show` 호출의 응답 지연을 없애려면 미리 캐시해둡니다.

```csharp
await uiManager.Preload<ProfileWindowPresenter>();
```

---

### Floating UI — 3D 월드 좌표 추적

`BindToWorld`로 VisualElement를 3D 오브젝트에 고정합니다.

```csharp
[Window("UI/HpBar")]
public sealed class HpBarPresenter : PresenterBase<HpBarArgs>
{
    public override void OnShow(HpBarArgs args)
    {
        base.OnShow(args);

        // HP 바 값 연동
        args.CurrentHp
            .Subscribe(v => progressBar.value = v)
            .AddTo(Disposables);

        // 매 프레임 3D 좌표 추적
        root.BindToWorld(args.Target, args.Camera, new FloatingOptions
        {
            mode                 = FloatingMode.Continuous, // 매 프레임 갱신
            worldOffset          = Vector3.up * 1.5f,
            pivot                = new Vector2(0.5f, 1f),
            hideWhenBehindCamera = true,
        }).AddTo(Disposables); // OnHide 시 자동 해제
    }
}
```

단발성 UI(데미지 텍스트 등)는 `FloatingMode.OneShot`을 사용합니다.

```csharp
root.BindToWorld(worldPosition, camera, new FloatingOptions
{
    mode = FloatingMode.OneShot, // 호출 시 1회만 위치 계산
});
```

---

### Presenter 라이프사이클

```
[신규 생성]
    OnInjected      ← VContainer 의존성 주입 (1회)
    OnViewReady     ← VisualElement 참조 캐싱 (1회)

[매 활성화/비활성화]
    OnShow          ← 표시될 때마다
    OnHide          ← 숨겨질 때마다 (Disposables 자동 정리)

[풀에서 재사용 시]
    OnInjected → OnShow / OnHide → OnCleared  ← 반복

[종료]
    OnCleared       ← UIManager Dispose 시 (1회)
    Dispose
```

> `OnShow`/`OnHide`는 `display: flex/none`으로 가시성을 제어하므로 `SetActive` 사이클이 발생하지 않습니다. `OnViewReady`는 최초 생성 시 단 1회만 호출됩니다.

---

### 편의 확장 메서드

```csharp
// 부모 Presenter가 hide될 때 함께 hide
childPresenter.HideOnHide(parentPresenter);

// 부모 Presenter가 hide될 때 Dispose
someDisposable.DisposeOnHide(parentPresenter);

// display:flex / display:none 전환 (SetActive 대신)
element.SetActiveAsDisplay(true);
```
