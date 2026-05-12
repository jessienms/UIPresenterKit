using UnityEngine;
using UnityEngine.UIElements;

namespace UILib
{
	public static class UIDocumentExtensions
	{
		public static void SetActiveAsDisplay(this UIDocument _uiDocument, bool _active)
		{
			if (!_uiDocument)
			{
				Debug.LogError("SetActiveAsDisplay - UIDocument is null.");
				return;
			}
			
			_uiDocument.rootVisualElement.style.display = _active ? DisplayStyle.Flex : DisplayStyle.None;
		}
    
	}
}
