using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIPresenterKit
{
    /// <summary>
    /// key → prefab / UXML 매핑을 Inspector 에서 직접 지정하는 IAssetLoader 구현체.
    /// Addressables 도입 전 개발 단계에서 사용한다.
    /// Assets 메뉴: Create > UILib > PrefabRegistry
    /// </summary>
    [CreateAssetMenu(fileName = "PrefabRegistry", menuName = "UILib/PrefabRegistry")]
    public sealed class PrefabRegistry : ScriptableObject, IAssetLoader
    {
        [Serializable]
        private struct Entry
        {
            public string key;
            public GameObject prefab;
        }
		

        [Serializable]
        private struct UxmlEntry
        {
            public string key;
            public VisualTreeAsset uxml;
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();
        [SerializeField] private UxmlEntry[] uxmlEntries = Array.Empty<UxmlEntry>();

        private Dictionary<string, GameObject> lookup;
        private Dictionary<string, VisualTreeAsset> uxmlLookup;

        public UniTask<GameObject> LoadAsync(string _key)
        {
            if (lookup == null)
            {
                lookup = new Dictionary<string, GameObject>(entries.Length);
                foreach (var e in entries)
                    if (!string.IsNullOrEmpty(e.key) && e.prefab != null)
                        lookup[e.key] = e.prefab;
            }

            if (!lookup.TryGetValue(_key, out var prefab))
                throw new KeyNotFoundException($"[UILib] PrefabRegistry: '{_key}' 에 등록된 prefab 이 없습니다.");

            return UniTask.FromResult(prefab);
        }

        public UniTask<VisualTreeAsset> LoadUxmlAsync(string _key)
        {
            if (uxmlLookup == null)
            {
                uxmlLookup = new Dictionary<string, VisualTreeAsset>(uxmlEntries.Length);
                foreach (var e in uxmlEntries)
                    if (!string.IsNullOrEmpty(e.key) && e.uxml != null)
                        uxmlLookup[e.key] = e.uxml;
            }

            if (!uxmlLookup.TryGetValue(_key, out var uxml))
                throw new KeyNotFoundException($"[UILib] PrefabRegistry: '{_key}' 에 등록된 UXML 이 없습니다.");

            return UniTask.FromResult(uxml);
        }
    }
}
