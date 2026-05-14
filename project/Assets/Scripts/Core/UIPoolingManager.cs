using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIPresenterKit.Core
{
    /// <summary>
    /// 전역 2차 캐시. presenter instance 의 순수 storage 역할만 한다.
    /// Presenter 생성 / Inject / OnCleared 호출은 하지 않는다.
    /// Release 전 presenter.OnCleared() 는 UIManager 가 책임진다.
    /// </summary>
    public sealed class UIPoolingManager : IDisposable
    {
        private readonly IAssetLoader assetLoader;
        private readonly Dictionary<string, Stack<DocumentInstance>> documentPool = new();
        private readonly Dictionary<string, Stack<ElementInstance>> elementPool = new();
        private Transform poolRoot;

        public UIPoolingManager(IAssetLoader _assetLoader)
        {
            assetLoader = _assetLoader;

            var go = new GameObject("[UIPool]");
            UnityEngine.Object.DontDestroyOnLoad(go);
            poolRoot = go.transform;
        }

        /// <summary>prefab 을 로드해 새 인스턴스를 생성한다. 비활성 상태로 반환된다.</summary>
        internal async UniTask<GameObject> Spawn(string _key)
        {
            var prefab = await assetLoader.LoadAsync(_key);
            if (prefab == null)
                throw new InvalidOperationException($"[UILib] '{_key}' 로드 결과가 null입니다. Window prefab GameObject 를 반환해야 합니다.");

            if (!prefab.TryGetComponent<UIDocument>(out _))
                throw new InvalidOperationException($"[UILib] '{_key}' prefab root 에 UIDocument 가 없습니다. Window prefab 의 루트 GameObject 에 UIDocument 를 추가하세요.");

            var go = UnityEngine.Object.Instantiate(prefab, poolRoot);
            go.SetActive(false);
            return go;
        }

        /// <summary>2차 pool 에서 DocumentInstance 를 꺼낸다. 없으면 null.</summary>
        internal DocumentInstance AcquireDocument(string _key)
        {
            if (documentPool.TryGetValue(_key, out var stack) && stack.Count > 0)
                return stack.Pop();
            return null;
        }

        /// <summary>2차 pool 에서 ElementInstance 를 꺼낸다. 없으면 null.</summary>
        internal ElementInstance AcquireElement(string _key)
        {
            if (elementPool.TryGetValue(_key, out var stack) && stack.Count > 0)
                return stack.Pop();
            return null;
        }

        /// <summary>
        /// DocumentInstance 를 2차 pool 에 반환한다.
        /// 전제: presenter.OnCleared() 는 이미 호출됨.
        /// </summary>
        internal void ReleaseDocument(string _key, DocumentInstance _instance)
        {
            if (!documentPool.TryGetValue(_key, out var stack))
            {
                stack = new Stack<DocumentInstance>();
                documentPool[_key] = stack;
            }
            stack.Push(_instance);
        }

        /// <summary>
        /// ElementInstance 를 2차 pool 에 반환한다.
        /// 전제: presenter.OnCleared() 는 이미 호출됨.
        /// </summary>
        internal void ReleaseElement(string _key, ElementInstance _instance)
        {
            if (!elementPool.TryGetValue(_key, out var stack))
            {
                stack = new Stack<ElementInstance>();
                elementPool[_key] = stack;
            }
            stack.Push(_instance);
        }

        public void Dispose()
        {
            foreach (var stack in documentPool.Values)
            {
                while (stack.TryPop(out var inst))
                {
                    inst.Presenter.Dispose();
                    UnityEngine.Object.Destroy(inst.Document.gameObject);
                }
            }
            documentPool.Clear();

            foreach (var stack in elementPool.Values)
            {
                while (stack.TryPop(out var inst))
                    inst.Presenter.Dispose();
            }
            elementPool.Clear();

            if (poolRoot != null)
            {
                UnityEngine.Object.Destroy(poolRoot.gameObject);
                poolRoot = null;
            }
        }
    }
}
