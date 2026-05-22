# UIPresenterKit

Unity UI Toolkit + VContainer 기반의 UI 모듈 라이브러리.  
팝업·HUD·월드 UI 등 모든 UI를 **Presenter 패턴**으로 일관되게 구현한다.

## 설치

`Packages/manifest.json`에 아래를 추가한다.

```json
"com.sg.uipresenterkit": "https://internal.skeinglobe.com/git/sg/UIPresenterKit.git?path=/project/Assets/UIPresenterKit"
```

## 빠른 시작

1. `LifetimeScope`에 `UIManager`, `UIPoolingManager`, `PrefabRegistry`를 등록한다.
2. Presenter 클래스를 작성한다 (`PresenterBase` 상속).
3. `UIManager.Show<T>()`로 UI를 띄운다.

자세한 사용법은 `Skills/ui-presenter/SKILL.md` 참고.

---

## AI 에이전트 설정 (Claude Code)

이 패키지에는 **Claude Code** 전용 `ui-presenter` 스킬이 포함되어 있다.  
스킬을 설치하면 AI가 UIPresenterKit의 올바른 패턴으로 UI를 자동 구현할 수 있다.

### 스킬 설치 방법

Claude Code 대화창에 아래 내용을 그대로 붙여넣고 실행한다.

---

> **UIPresenterKit 스킬을 설치해줘.**
>
> 아래 순서대로 진행해줘:
> 1. `Library/PackageCache/com.sg.uipresenterkit*/Skills/ui-presenter/` 경로를 Glob으로 찾아줘.
> 2. 해당 폴더 안의 `SKILL.md`와 `references/uipresenterkit-api.md` 파일을 Read로 읽어줘.
> 3. 읽은 내용을 프로젝트 루트의 `.claude/skills/ui-presenter/SKILL.md`와 `.claude/skills/ui-presenter/references/uipresenterkit-api.md`에 Write로 복사해줘.
> 4. 완료되면 스킬이 활성화됐다고 알려줘.

---

설치 후에는 "UI 만들어줘", "팝업 추가해줘" 등의 요청 시 스킬이 자동으로 적용된다.

### 스킬 파일 위치

```
Library/PackageCache/com.sg.uipresenterkit@<hash>/
└── Skills/
    └── ui-presenter/
        ├── SKILL.md                        ← 스킬 메인 (패턴·예제)
        └── references/
            └── uipresenterkit-api.md       ← 전체 API 레퍼런스
```
