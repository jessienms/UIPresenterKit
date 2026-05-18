using System;
using R3;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIPresenterKit.Core
{
	/// <summary>Floating UI 의 동작 방식.</summary>
	public enum FloatingMode
	{
		/// <summary>매 프레임 타겟 좌표를 갱신한다. HP 바, 이름표 등에 사용.</summary>
		Continuous,
		/// <summary>호출 시점에 1회만 위치를 계산한다. 데미지 텍스트처럼 위치만 잡고 이후 애니메이션으로 처리하는 경우에 사용.</summary>
		OneShot,
	}

	/// <summary>BindToWorld 동작 옵션.</summary>
	public struct FloatingOptions
	{
		public FloatingMode Mode;

		/// <summary>타겟 기준 월드 오프셋. 예: 캐릭터 머리 위에 표시하려면 Vector3.up * 2f.</summary>
		public Vector3 WorldOffset;

		/// <summary>panel 좌표계 기준 화면 추가 오프셋 (픽셀).</summary>
		public Vector2 ScreenOffset;

		/// <summary>
		/// UI 요소의 어느 지점이 타겟 화면 좌표에 맞춰질지.
		/// (0,0) = 좌상단, (0.5,0.5) = 중앙, (0.5,1) = 하단 중앙.
		/// </summary>
		public Vector2 Pivot;

		/// <summary>카메라 뒤에 있을 때 자동으로 숨길지.</summary>
		public bool HideWhenBehindCamera;

		/// <summary>화면 밖으로 나갔을 때 자동으로 숨길지.</summary>
		public bool HideWhenOffScreen;

		public static FloatingOptions Default => new FloatingOptions
		{
			Mode = FloatingMode.Continuous,
			Pivot = new Vector2(0.5f, 0.5f),
			HideWhenBehindCamera = true,
			HideWhenOffScreen = false,
		};
	}

	/// <summary>
	/// VisualElement 를 3D 월드 좌표에 동기화하는 확장 메서드 모음.
	///
	/// 마운트는 UIManager.ShowAttached 로 기존과 동일하게 처리하고,
	/// 위치 동기화만 이 클래스가 담당한다.
	///
	/// 사용 예 (Presenter.OnShow 안):
	///   root.BindToWorld(character.transform, camera, new FloatingOptions {
	///       WorldOffset = Vector3.up * 2.2f,
	///       Pivot = new Vector2(0.5f, 1f),
	///   }).AddTo(Disposables);
	///
	/// 주의: BindToWorld 를 쓰는 UXML root 에는 USS 로 아래를 명시해야 한다.
	///   position: absolute;
	///   flex-grow: 0;
	/// (UIManager 가 UXML clone 시 flex-grow: 1 을 코드로 적용하므로 USS 가 우선 적용되지 않는다.
	///  BindToWorld 내부에서 코드로 덮어쓴다.)
	/// </summary>
	public static class Floating
	{
		/// <summary>
		/// Transform 타겟을 추적한다.
		/// Continuous 모드: 매 프레임 갱신. 타겟이 파괴되면 숨김 처리 후 대기.
		/// OneShot 모드: 호출 시점 1회 위치 계산. 빈 IDisposable 반환.
		/// </summary>
		public static IDisposable BindToWorld(
			this VisualElement _root,
			Transform _target,
			Camera _camera,
			FloatingOptions? _options = null)
		{
			var opts = _options ?? FloatingOptions.Default;

			_root.style.position = Position.Absolute;
			_root.style.flexGrow = 0;

			if (opts.Mode == FloatingMode.OneShot)
			{
				if (_target != null)
					ApplyPosition(_root, _target.position, _camera, opts);
				return EmptyDisposable.Instance;
			}

			return Observable.EveryUpdate(UnityFrameProvider.Update)
				.Subscribe(_ =>
				{
					if (_target == null)
					{
						_root.SetActiveAsDisplay(false);
						return;
					}
					ApplyPosition(_root, _target.position, _camera, opts);
				});
		}

		/// <summary>
		/// 고정 월드 좌표에 표시한다.
		/// Continuous 모드: 카메라가 움직여도 위치가 갱신된다.
		/// OneShot 모드: 호출 시점 1회만 계산한다 (이후 화면 고정 이펙트 용).
		/// </summary>
		public static IDisposable BindToWorld(
			this VisualElement _root,
			Vector3 _worldPosition,
			Camera _camera,
			FloatingOptions? _options = null)
		{
			var opts = _options ?? FloatingOptions.Default;

			_root.style.position = Position.Absolute;
			_root.style.flexGrow = 0;

			if (opts.Mode == FloatingMode.OneShot)
			{
				ApplyPosition(_root, _worldPosition, _camera, opts);
				return EmptyDisposable.Instance;
			}

			return Observable.EveryUpdate(UnityFrameProvider.Update)
				.Subscribe(_ => ApplyPosition(_root, _worldPosition, _camera, opts));
		}

		private static void ApplyPosition(VisualElement _root, Vector3 _worldPos, Camera _camera, FloatingOptions _opts)
		{
			if (_root.panel == null) return;

			Vector3 screen = _camera.WorldToScreenPoint(_worldPos + _opts.WorldOffset);
			bool inFront = screen.z > 0f;

			bool visible = inFront || !_opts.HideWhenBehindCamera;
			if (visible && _opts.HideWhenOffScreen)
				visible = screen.x >= 0f && screen.x <= Screen.width
					   && screen.y >= 0f && screen.y <= Screen.height;

			_root.SetActiveAsDisplay(visible);
			if (!visible) return;

			// Unity screen 좌표계: 좌하단 기준, y 위쪽
			// UI Toolkit panel 좌표계: 좌상단 기준, y 아래쪽
			Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(
				_root.panel,
				new Vector2(screen.x, Screen.height - screen.y));
			panelPos += _opts.ScreenOffset;

			// resolvedStyle 이 첫 layout 전엔 NaN 이므로 Pivot 보정은 준비된 이후에만 적용
			float w = _root.resolvedStyle.width;
			float h = _root.resolvedStyle.height;
			if (!float.IsNaN(w) && !float.IsNaN(h))
			{
				panelPos.x -= w * _opts.Pivot.x;
				panelPos.y -= h * _opts.Pivot.y;
			}

			_root.style.left = panelPos.x;
			_root.style.top = panelPos.y;
		}

		private sealed class EmptyDisposable : IDisposable
		{
			public static readonly EmptyDisposable Instance = new EmptyDisposable();
			public void Dispose() { }
		}
	}
}
