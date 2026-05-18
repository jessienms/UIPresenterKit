using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UIPresenterKit.Core;
using UIPresenterKit.Samples.Model;
using UIPresenterKit.Samples.Scope;
using UIPresenterKit.Samples.UI;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace UIPresenterKit.Samples.Game
{
    public sealed class MobProcessor : IStartable, IDisposable
    {
        private readonly UIManager uiManager;
        private readonly MobListModel model;
        private readonly GameObject prefab;
        private readonly Camera mainCamera;
        private readonly FloatingDocumentRef floatingDocRef;

        private readonly Dictionary<MobModel, HpBarPresenter> bars = new();
        private readonly Dictionary<MobModel, IDisposable> diedSubs = new();

        [Inject]
        public MobProcessor(
            UIManager _uiManager,
            MobListModel _model,
            MobPrefabRef _prefabRef,
            MainCameraRef _cameraRef,
            FloatingDocumentRef _floatingRef)
        {
            uiManager = _uiManager;
            model = _model;
            prefab = _prefabRef.Prefab;
            mainCamera = _cameraRef.Camera;
            floatingDocRef = _floatingRef;
        }

        public void Start() { }

        public void SpawnOne()
        {
            var pos = new Vector3(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f));
            var go = Object.Instantiate(prefab, pos, Quaternion.identity);
            var health = go.GetComponent<MobHealth>();
            var mob = new MobModel(go, health);

            model.Add(mob);
            AttachBar(mob).Forget();

            var cd = new CompositeDisposable();
            health.OnDied.Subscribe(_ => Destroy(mob)).AddTo(cd);

            var prevHp = health.MaxHp;
            health.CurrentHp.Subscribe(hp =>
            {
                var dmg = prevHp - hp;
                prevHp = hp;
                if (dmg > 0)
                    SpawnDamageText(mob.GameObject.transform.position, dmg).Forget();
            }).AddTo(cd);

            diedSubs[mob] = cd;
        }

        private async UniTaskVoid SpawnDamageText(Vector3 _worldPos, int _damage)
        {
            var floatingRoot = floatingDocRef.Document.rootVisualElement;
            var args = new DamageTextArgs(_worldPos, _damage, mainCamera);
            await uiManager.ShowAttached<DamageTextPresenter>(floatingRoot, args);
        }

        public void RemoveOldest()
        {
            if (model.First != null)
                Destroy(model.First);
        }

        private async UniTaskVoid AttachBar(MobModel _mob)
        {
            try { await AttachBarAsync(_mob); }
            catch (Exception e) { Debug.LogError($"[AttachBar] {e}"); }
        }

        private async UniTask AttachBarAsync(MobModel _mob)
        {
            var floatingRoot = floatingDocRef.Document.rootVisualElement;
            Debug.Log($"[AttachBar] floatingRoot={(floatingRoot == null ? "NULL" : floatingRoot.ToString())}");
            var args = new HpBarArgs(_mob.GameObject.transform, mainCamera, _mob.Health.CurrentHp, _mob.Health.MaxHp);
            var bar = await uiManager.ShowAttached<HpBarPresenter>(floatingRoot, args);
            Debug.Log($"[AttachBar] bar={bar}");
            bars[_mob] = bar;
        }

        private void Destroy(MobModel _mob)
        {
            if (diedSubs.Remove(_mob, out var sub)) sub.Dispose();
            if (bars.Remove(_mob, out var bar)) uiManager.Hide(bar);
            model.Remove(_mob);
            if (_mob.GameObject) Object.Destroy(_mob.GameObject);
        }

        public void Dispose()
        {
            foreach (var sub in diedSubs.Values) sub.Dispose();
            diedSubs.Clear();

            // UIManager 가 이미 dispose 됐을 수 있으므로 bars 는 dictionary clear 만
            bars.Clear();

            // 씬에 남은 mob GameObject 정리
            foreach (var mob in model.Mobs)
                if (mob.GameObject) Object.Destroy(mob.GameObject);
        }
    }
}
