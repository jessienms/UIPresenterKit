using R3;
using UIPresenterKit.Core;
using UIPresenterKit.Samples.Game;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIPresenterKit.Samples.UI
{
    public readonly struct HpBarArgs : IPresenterArgs<HpBarPresenter>
    {
        public Transform Target { get; }
        public Camera Camera { get; }
        public ReactiveProperty<int> CurrentHp { get; }
        public int MaxHp { get; }

        public HpBarArgs(Transform _target, Camera _camera, ReactiveProperty<int> _currentHp, int _maxHp)
        {
            Target = _target;
            Camera = _camera;
            CurrentHp = _currentHp;
            MaxHp = _maxHp;
        }

        public void InvokeOnShow(HpBarPresenter _presenter) => _presenter.OnShow(this);
    }

    [Window("UI/HpBar")]
    public sealed class HpBarPresenter : PresenterBase<HpBarArgs>
    {
        private VisualElement root;
        private ProgressBar bar;

        public override void OnViewReady(VisualElement _root)
        {
            root = _root;
            bar = _root.Q<ProgressBar>("hp-bar");
        }

        public override void OnShow(HpBarArgs _arg)
        {
            base.OnShow(_arg);

            bar.lowValue = 0f;
            bar.highValue = _arg.MaxHp;
            bar.value = _arg.CurrentHp.Value;

            _arg.CurrentHp
                .Subscribe(_v => bar.value = _v)
                .AddTo(Disposables);

            root.BindToWorld(_arg.Target, _arg.Camera, new FloatingOptions
            {
                Mode = FloatingMode.Continuous,
                WorldOffset = Vector3.up * 1.2f,
                Pivot = new Vector2(0.5f, 1f),
                HideWhenBehindCamera = true,
            }).AddTo(Disposables);
        }

        public override void OnCleared()
        {
            root = null;
            bar = null;
        }
    }
}
