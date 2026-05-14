using System;
using R3;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace UILib
{
	public static class UILibUtils
	{
		public static void SetActiveAsDisplay(this UIDocument _uiDocument, bool _active)
		{
			if (!_uiDocument)
			{
				Debug.LogError("SetActiveAsDisplay - UIDocument is null.");
				return;
			}

			_uiDocument.rootVisualElement.SetActiveAsDisplay(_active);
		}

		public static void SetActiveAsDisplay(this VisualElement _uiElement, bool _active)
		{
			if (_uiElement == null)
			{
				Debug.LogError("SetActiveAsDisplay - VisualElement is null.");
				return;
			}

			_uiElement.style.display = _active ? DisplayStyle.Flex : DisplayStyle.None;
		}

		public static T HideOnHide<T>(this T _presenterBase, PresenterBase _base) where T : PresenterBase
		{
			if (_presenterBase == null)
			{
				Debug.LogError("HideOnHide - presenterBase is null.");
				return null;
			}

			if (_base == null)
			{
				Debug.LogError("HideOnHide - base is null.");
				return _presenterBase;
			}

			var subscribeDisposable = _base.OnHideAsObservable.Subscribe(_ => _presenterBase.RequestHide());
			_base.AddTo(subscribeDisposable);

			return _presenterBase as T;
		}

		public static void DisposeOnHide(this IDisposable _disposable, PresenterBase _base)
		{
			if (_base == null)
			{
				Debug.LogError("DisposeOnHide - base is null.");
				return;
			}
			
			var subscribeDisposable = _base.OnHideAsObservable.Subscribe(_ => _disposable.Dispose());
			_base.AddTo(subscribeDisposable);
		}
	}
}
