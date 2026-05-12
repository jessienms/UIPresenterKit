using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace UILib
{
    /// <summary>
    /// 전역 2차 캐시. window instance 의 순수 storage 역할만 한다.
    /// Presenter 생성 / Inject / OnDetached 호출은 하지 않는다.
    /// Release 전 presenter.OnDetached() 는 UIManager 가 책임진다.
    /// </summary>
    public sealed class UIPoolingManager : IDisposable
    {
        private readonly IAssetLoader assetLoader;
        private readonly Dictionary<string, Stack<WindowInstance>> pool = new();
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
            {
                throw new InvalidOperationException($"[UILib] '{_key}' 로드 결과가 null입니다. Window prefab GameObject 를 반환해야 합니다.");
            }

            if (!prefab.TryGetComponent<UIDocument>(out _))
            {
                throw new InvalidOperationException($"[UILib] '{_key}' prefab root 에 UIDocument 가 없습니다. Window prefab 의 루트 GameObject 에 UIDocument 를 추가하세요.");
            }

            var go = UnityEngine.Object.Instantiate(prefab, poolRoot);
            go.SetActive(false);
            return go;
        }

        /// <summary>2차 pool 에서 window instance 를 꺼낸다. 없으면 null.</summary>
        internal WindowInstance Acquire(string _key)
        {
            if (pool.TryGetValue(_key, out var stack) && stack.Count > 0)
                return stack.Pop();
            return null;
        }

        /// <summary>
        /// window instance 를 2차 pool 에 반환한다.
        /// 전제: presenter.OnDetached() 는 이미 호출됨.
        /// </summary>
        internal void Release(string _key, WindowInstance _pair)
        {
            // SetActive 불필요 — UIManager.Hide 가 이미 display:none 으로 처리.
            if (!pool.TryGetValue(_key, out var stack))
            {
                stack = new Stack<WindowInstance>();
                pool[_key] = stack;
            }
            stack.Push(_pair);
        }

        public void Dispose()
        {
            foreach (var stack in pool.Values)
            {
                while (stack.TryPop(out var pair))
                {
                    pair.Presenter.Dispose();
                    if (pair.Document != null)
                        UnityEngine.Object.Destroy(pair.Document.gameObject);
                }
            }
            pool.Clear();

            if (poolRoot != null)
            {
                UnityEngine.Object.Destroy(poolRoot.gameObject);
                poolRoot = null;
            }
        }
    }
}
