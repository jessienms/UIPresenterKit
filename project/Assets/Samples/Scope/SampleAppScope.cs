using UIPresenterKit.Core;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UIPresenterKit.Samples
{
    /// <summary>
    /// 앱 루트 LifetimeScope. UIPoolingManager 를 전역 singleton 으로 등록한다.
    ///
    /// [씬 설정]
    /// 1. 씬에 빈 GameObject 를 만들고 이 컴포넌트를 붙인다.
    /// 2. prefabRegistry 에 PrefabRegistry 에셋을 연결한다.
    ///    (Create > UILib > PrefabRegistry 로 생성 후 key → prefab 매핑 입력)
    /// 3. SampleSceneScope 의 ParentLifetimeScope 를 이 오브젝트로 지정한다.
    /// </summary>
    public sealed class SampleAppScope : LifetimeScope
    {
        [SerializeField] private PrefabRegistry prefabRegistry;

        protected override void Configure(IContainerBuilder _builder)
        {
            _builder.RegisterInstance<IAssetLoader>(prefabRegistry);
            _builder.Register<UIPoolingManager>(Lifetime.Singleton);
        }
    }
}
