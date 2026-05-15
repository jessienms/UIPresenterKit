using UnityEditor;
using UnityEngine;

namespace UIPresenterKit.Samples
{
    /// <summary>
    /// UIPresenterKit > Sample > Terrain Builder 로 열리는 EditorWindow.
    /// 높이 0에 플레인을 깔고 가장자리에 마인크래프트 스타일 블록 산을 생성한다.
    /// </summary>
    public class SampleTerrainBuilder : EditorWindow
    {
        private const int GridHalf = 12;
        private const int FlatHalf = 7;
        private const int MinHeight = 0;
        private const int MaxHeight = 4;
        private const float NoiseScale = 0.38f;
        private const float NoiseSeed = 73.1f;

        [SerializeField] private Gradient heightGradient;

        // ─────────────────────────────────────────────────────────

        [MenuItem("UIPresenterKit/Sample/Terrain Builder")]
        static void Open() => GetWindow<SampleTerrainBuilder>("Terrain Builder");

        void OnEnable()
        {
            if (heightGradient == null)
                heightGradient = DefaultGradient();
        }

        void OnGUI()
        {
            GUILayout.Label("산 블록 색상", EditorStyles.boldLabel);
            heightGradient = EditorGUILayout.GradientField(
                new GUIContent("높이 그라디언트", "왼쪽 = 낮은 블록, 오른쪽 = 높은 블록"),
                heightGradient);
            EditorGUILayout.HelpBox("왼쪽 끝 → 낮은 층, 오른쪽 끝 → 높은 층", MessageType.None);

            GUILayout.Space(12);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Build Terrain")) BuildTerrain();
                if (GUILayout.Button("Clear Terrain")) ClearTerrain();
            }
        }

        // ─────────────────────────────────────────────────────────

        void BuildTerrain()
        {
            var existing = GameObject.Find("SampleTerrain");
            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog("지형 생성",
                        "기존 SampleTerrain을 삭제하고 새로 만듭니다.", "확인", "취소"))
                    return;
                Undo.DestroyObjectImmediate(existing);
            }

            var root = new GameObject("SampleTerrain");
            Undo.RegisterCreatedObjectUndo(root, "Build Sample Terrain");

            CreateGround(root.transform);
            int count = CreateMountains(root.transform);

            Debug.Log($"[SampleTerrainBuilder] 완료 — 블록 {count}개");
        }

        static void ClearTerrain()
        {
            var existing = GameObject.Find("SampleTerrain");
            if (existing == null) { Debug.Log("[SampleTerrainBuilder] SampleTerrain 없음"); return; }
            Undo.DestroyObjectImmediate(existing);
        }

        // ─────────────────────────────────────────────────────────

        static void CreateGround(Transform parent)
        {
            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "Ground";
            plane.transform.SetParent(parent);
            plane.transform.localPosition = Vector3.zero;
            plane.transform.localScale = new Vector3(2.5f, 1f, 2.5f);
            SetStatic(plane);
        }

        int CreateMountains(Transform parent)
        {
            var mountains = new GameObject("Mountains");
            mountains.transform.SetParent(parent);

            var materials = BuildMaterials();

            int count = 0, processed = 0;
            int total = (GridHalf * 2 + 1) * (GridHalf * 2 + 1);

            for (int x = -GridHalf; x <= GridHalf; x++)
            for (int z = -GridHalf; z <= GridHalf; z++)
            {
                processed++;
                if (processed % 50 == 0)
                    EditorUtility.DisplayProgressBar("지형 생성 중…",
                        $"{processed}/{total}", (float)processed / total);

                if (Mathf.Abs(x) <= FlatHalf && Mathf.Abs(z) <= FlatHalf) continue;

                int h = ComputeHeight(x, z);
                for (int y = 0; y < h; y++)
                {
                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.SetParent(mountains.transform);
                    cube.transform.localPosition = new Vector3(x, y + 0.5f, z);
                    // 높이 레벨에 맞는 머티리얼 공유 (같은 높이는 동일 인스턴스)
                    cube.GetComponent<MeshRenderer>().sharedMaterial =
                        materials[Mathf.Clamp(y, 0, MaxHeight)];
                    SetStatic(cube);
                    count++;
                }
            }

            EditorUtility.ClearProgressBar();
            return count;
        }

        // 높이 단계별 머티리얼을 미리 생성 — 같은 높이의 큐브끼리 공유
        Material[] BuildMaterials()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard");

            var mats = new Material[MaxHeight + 1];
            for (int i = 0; i <= MaxHeight; i++)
            {
                float t = MaxHeight > 0 ? (float)i / MaxHeight : 0f;
                var color = heightGradient.Evaluate(t);

                var mat = new Material(shader) { name = $"TerrainBlock_H{i}" };
                // Standard shader: _Color / URP Lit: _BaseColor — 양쪽 모두 설정
                mat.color = color;
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", color);

                mats[i] = mat;
            }
            return mats;
        }

        static int ComputeHeight(int x, int z)
        {
            float dx = Mathf.Max(0f, Mathf.Abs(x) - FlatHalf);
            float dz = Mathf.Max(0f, Mathf.Abs(z) - FlatHalf);
            float dist = Mathf.Max(dx, dz);
            float t = Mathf.Clamp01(dist / (GridHalf - FlatHalf));
            float noise = Mathf.PerlinNoise(
                (x + NoiseSeed) * NoiseScale,
                (z + NoiseSeed) * NoiseScale);
            float heightF = Mathf.Lerp(MinHeight, MaxHeight, t * 0.55f + noise * 0.45f);
            return Mathf.Max(1, Mathf.RoundToInt(heightF));
        }

        static void SetStatic(GameObject go)
        {
            GameObjectUtility.SetStaticEditorFlags(go,
                StaticEditorFlags.BatchingStatic |
                StaticEditorFlags.OccludeeStatic |
                StaticEditorFlags.OccluderStatic);
        }

        static Gradient DefaultGradient()
        {
            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.30f, 0.25f, 0.20f), 0.0f), // 낮은 층: 어두운 흙
                    new GradientColorKey(new Color(0.55f, 0.55f, 0.55f), 0.5f), // 중간: 회색 바위
                    new GradientColorKey(Color.white, 1.0f),                     // 높은 층: 눈
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f),
                });
            return g;
        }
    }
}
