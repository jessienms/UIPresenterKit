using System;
using R3;
using UIPresenterKit.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIPresenterKit.Samples.UI
{
    public readonly struct DamageTextArgs : IPresenterArgs<DamageTextPresenter>
    {
        public Vector3 WorldPosition { get; }
        public int Damage { get; }
        public Camera Camera { get; }

        public DamageTextArgs(Vector3 worldPosition, int damage, Camera camera)
        {
            WorldPosition = worldPosition;
            Damage = damage;
            Camera = camera;
        }

        public void InvokeOnShow(DamageTextPresenter _presenter) => _presenter.OnShow(this);
    }

    [Window("UI/DamageText")]
    public sealed class DamageTextPresenter : PresenterBase<DamageTextArgs>
    {
        private VisualElement root;
        private Label label;
        private float elapsed;

        private const float Duration = 0.8f;
        private const float RisePixels = 65f;

        public override void OnViewReady(VisualElement _root)
        {
            root = _root;
            label = _root.Q<Label>("damage-label");
        }

        public override void OnShow(DamageTextArgs _arg)
        {
            base.OnShow(_arg);

            label.text = _arg.Damage.ToString();
            elapsed = 0f;
            root.style.opacity = 1f;

            root.BindToWorld(_arg.WorldPosition, _arg.Camera, new FloatingOptions
            {
                Mode = FloatingMode.OneShot,
                WorldOffset = Vector3.up * 1.5f,
                Pivot = new Vector2(0.5f, 1f),
            });

            var baseTop = root.style.top.value.value;

            Observable.EveryUpdate(UnityFrameProvider.Update)
                .Subscribe(_ =>
                {
                    elapsed += Time.deltaTime;
                    var t = Mathf.Clamp01(elapsed / Duration);
                    root.style.top = baseTop - RisePixels * EaseOut(t);
                    root.style.opacity = 1f - t;
                })
                .AddTo(Disposables);

            Observable.Timer(TimeSpan.FromSeconds(Duration + 0.05f))
                .Subscribe(_ => RequestHide())
                .AddTo(Disposables);
        }

        public override void OnCleared()
        {
            root = null;
            label = null;
        }

        private static float EaseOut(float _t) => 1f - (1f - _t) * (1f - _t);
    }
}
