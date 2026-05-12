using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UILib
{
    /// <summary>
    /// 전역 2차 캐시. pair 의 순수 storage 역할만 한다.
    /// Presenter 생성 / Inject / OnDetached 호출은 하지 않는다.
    /// Release 전 presenter.OnDetached() 는 UIManager 가 책임진다.
    /// </summary>
    public sealed class UIPoolingManager : IDisposable
    {
        private readonly IAssetLoader assetLoader;
        private readonly Dictionary<string, Stack<WindowPair>> pool = new();
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
            var go = UnityEngine.Object.Instantiate(prefab, poolRoot);
            go.SetActive(false);
            return go;
        }

        /// <summary>2차 pool 에서 pair 를 꺼낸다. 없으면 null.</summary>
        internal WindowPair Acquire(string _key)
        {
            if (pool.TryGetValue(_key, out var stack) && stack.Count > 0)
                return stack.Pop();
            return null;
        }

        /// <summary>
        /// pair 를 2차 pool 에 반환한다.
        /// 전제: presenter.OnDetached() 는 이미 호출됨.
        /// </summary>
        internal void Release(string _key, WindowPair _pair)
        {
            _pair.Go.SetActive(false);
            if (!pool.TryGetValue(_key, out var stack))
            {
                stack = new Stack<WindowPair>();
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
                    if (pair.Go != null)
                        UnityEngine.Object.Destroy(pair.Go);
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
