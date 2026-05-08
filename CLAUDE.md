# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

다른 Unity 게임 프로젝트에서 패키지 형태로 가져다 쓸 수 있는 **UI 모듈 라이브러리**. UI Toolkit과 VContainer를 핵심 기술로 사용하며, 반응형 프로그래밍 패턴을 기반으로 설계한다.

- `doc/` — 라이브러리 사용자(개발자)에게 배포되는 문서
- `project/` — 라이브러리 코드와 테스트 씬이 담기는 Unity 프로젝트

## 기술 스택

| 패키지 | 버전 | 역할 |
|---|---|---|
| Unity | 6000.3.9f1 | 엔진 |
| UI Toolkit (`com.unity.modules.uielements`) | built-in | UI 컴포넌트 |
| VContainer (`jp.hadashikick.vcontainer`) | 1.17.0 | DI 컨테이너 |
| R3 (`com.cysharp.r3`) | 1.3.0 | Reactive Extensions |
| UniTask (`com.cysharp.unitask`) | 2.5.10 | 비동기 처리 |
| ObservableCollections (`com.cysharp.observablecollections`) | 1.1.3 | 반응형 컬렉션 |
| New Input System (`com.unity.inputsystem`) | 1.18.0 | 입력 처리 |
| URP (`com.unity.render-pipelines.universal`) | 17.3.0 | 렌더 파이프라인 |
| Unity Test Framework (`com.unity.test-framework`) | 1.6.0 | 테스트 |

외부 패키지는 OpenUPM(`https://package.openupm.com`)을 통해 설치한다(`com.cysharp.*`, `jp.hadashikick.*` 스코프).

## 아키텍처 원칙

### DI 우선 설계 (VContainer)
모든 UI 모듈은 VContainer의 `LifetimeScope`를 통해 의존성을 주입받는다. `new` 키워드로 직접 인스턴스를 생성하지 않는다. 씬별 또는 모듈별 `LifetimeScope`를 구성하고, 상위 스코프에서 하위 스코프로 의존성을 전달하는 계층 구조를 유지한다.

### 반응형 UI 패턴 (R3 + ObservableCollections)
UI 상태는 `ReactiveProperty<T>` 또는 `Observable<T>`로 표현한다. UI 컴포넌트는 상태 변경을 직접 감지하여 자신을 갱신한다(`Subscribe`). 이벤트 핸들러보다 스트림 합성(`Select`, `Where`, `CombineLatest` 등)을 우선한다.

### UI Toolkit 컴포넌트 구조
- UXML로 레이아웃을 정의하고 USS로 스타일을 분리한다.
- 코드에서는 `VisualElement`를 직접 생성하기보다 `UxmlFactory` 또는 `QuerySelector`로 참조한다.
- 재사용 가능한 UI 요소는 Custom Control로 캡슐화한다.

### 비동기 처리 (UniTask)
`async/await`에는 반드시 `UniTask`를 사용한다. `Task`나 코루틴을 혼용하지 않는다.

## 테스트

Unity Test Runner(Window > General > Test Runner)에서 실행한다.
- **Edit Mode 테스트**: 로직 단위 테스트, DI 설정 검증
- **Play Mode 테스트**: UI 상호작용, 씬 통합 테스트

CLI로 실행할 경우:
```bash
# Edit Mode
"<UnityEditorPath>/Unity.exe" -batchmode -runTests -testPlatform EditMode -projectPath project -testResults results.xml

# Play Mode
"<UnityEditorPath>/Unity.exe" -batchmode -runTests -testPlatform PlayMode -projectPath project -testResults results.xml
```
