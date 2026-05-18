using R3;
using UnityEngine;
using UnityEngine.InputSystem;
using Unit = R3.Unit;

namespace UIPresenterKit.Samples.Game
{
    public sealed class MobHealth : MonoBehaviour
    {
        public int MaxHp { get; } = 100;
        public ReactiveProperty<int> CurrentHp { get; private set; }

        // CurrentHp.Where(v => v <= 0).Take(1) 이 OnDied 역할을 하지만
        // 여러 subscriber 가 공유할 Subject 로 노출해 one-shot 보장.
        private readonly Subject<Unit> onDied = new();
        public Observable<Unit> OnDied => onDied;

        private bool dead;

        private void Awake()
        {
            CurrentHp = new ReactiveProperty<int>(MaxHp);
        }

        private void Update()
        {
            if (dead) return;
            if (!Mouse.current.leftButton.wasPressedThisFrame) return;
            if (Camera.main == null) return;
            var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out var hit) || hit.collider.gameObject != gameObject) return;

            CurrentHp.Value = Mathf.Max(0, CurrentHp.Value - Random.Range(25, 61));
            if (CurrentHp.Value <= 0)
            {
                dead = true;
                onDied.OnNext(Unit.Default);
                onDied.OnCompleted();
            }
        }

        private void OnDestroy()
        {
            CurrentHp.Dispose();
            onDied.Dispose();
        }
    }
}
