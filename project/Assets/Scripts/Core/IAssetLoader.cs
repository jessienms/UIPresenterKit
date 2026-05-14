using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIPresenterKit.Core
{
    public interface IAssetLoader
    {
        UniTask<GameObject> LoadAsync(string _key);
        UniTask<VisualTreeAsset> LoadUxmlAsync(string _key);
    }
}
