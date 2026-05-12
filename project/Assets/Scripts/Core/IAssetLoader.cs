using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UILib
{
    public interface IAssetLoader
    {
        UniTask<GameObject> LoadAsync(string _key);
    }
}
