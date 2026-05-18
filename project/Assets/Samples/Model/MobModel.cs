using UIPresenterKit.Samples.Game;
using UnityEngine;

namespace UIPresenterKit.Samples.Model
{
    public sealed class MobModel
    {
        public GameObject GameObject { get; }
        public MobHealth Health { get; }

        public MobModel(GameObject _go, MobHealth _health)
        {
            GameObject = _go;
            Health = _health;
        }
    }
}
