using UnityEngine;

/// <summary>
/// 맵 타일 가장자리에서 위로 올라오는 빔(벽) 이펙트.
/// 4개의 쿼드로 각 변 벽을 구성하고, Custom/MapSelectionBeam 쉐이더로 그립니다.
/// 플레이어가 맵 위에 있어 교체가 불가능할 때는 <see cref="SetBlocked"/>로 빔을 붉게 만듭니다.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MapSelectionBeam : MonoBehaviour
{
    [Header("빔 머티리얼 (Custom/MapSelectionBeam)")]
    [SerializeField]
    private Material _beamMaterial;

    [Header("빔 높이 (월드 유닛)")]
    [SerializeField]
    private float _beamHeight = 2.5f;

    [Header("타일 크기 (MapManager._tileSize와 동일)")]
    [SerializeField]
    private Vector3 _tileSize = new Vector3(12f, 1f, 10f);

    private static readonly int BlockedID = Shader.PropertyToID("_Blocked");

    private Mesh _mesh;
    private Material _material;
    private bool _blocked;

    [Header("빔 바닥 Y 고정 (MapManager가 지면 Y를 주입)")]
    [SerializeField]
    private bool _stickToGround = true;

    /// <summary>빔 바닥이 붙을 월드 Y (타일 바닥). MapManager가 설정.</summary>
    public float GroundY { get; set; }

    private void LateUpdate()
    {
        // FloatingCursor의 보빙에 따라 빔이 공중에 뜨지 않도록 지면에 고정
        if (_stickToGround && _mesh != null)
        {
            Vector3 pos = transform.position;
            pos.y = GroundY;
            transform.position = pos;
        }
    }

    public float BeamHeight
    {
        get => _beamHeight;
        set
        {
            _beamHeight = value;
            Rebuild();
        }
    }

    /// <summary>타일 크기 설정(XZ 크기 사용).</summary>
    public void SetTileSize(Vector3 tileSize)
    {
        _tileSize = tileSize;
        Rebuild();
    }

    /// <summary>
    /// 맵 교체 불가 상태 표시. true면 빔이 붉은색으로 바뀝니다.
    /// </summary>
    public void SetBlocked(bool blocked)
    {
        if (_blocked == blocked)
        {
            return;
        }

        _blocked = blocked;

        if (_material != null)
        {
            _material.SetFloat(BlockedID, blocked ? 1f : 0f);
        }
    }

    private void Awake()
    {
        EnsureResources();
        Rebuild();
    }

    private void OnDestroy()
    {
        // 런타임 생성 리소스 정리 (머티리얼 에셋은 인스턴스 복제본만 파괴)
        if (_mesh != null)
        {
            Destroy(_mesh);
            _mesh = null;
        }
        if (_material != null)
        {
            Destroy(_material);
            _material = null;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 편집 모드에서는 리소스를 만들지 않는다 (프리팹에 런타임 메시/머티리얼이 직렬화되는 것을 방지)
        // 런타임에서 Awake가 메시/머티리얼을 생성한다.
    }
#endif

    private void EnsureResources()
    {
        var meshFilter = GetComponent<MeshFilter>();

        if (_mesh == null)
        {
            // 인스턴스 전용 메시 생성 (에셋 오염 방지)
            _mesh = new Mesh { name = "MapSelectionBeamMesh" };
            _mesh.MarkDynamic();
        }
        meshFilter.sharedMesh = _mesh;

        if (_material == null)
        {
            if (_beamMaterial == null)
            {
                Debug.LogError($"[MapSelectionBeam] '{name}'에 빔 머티리얼이 할당되지 않았습니다.");
                return;
            }
            // 머티리얼 에셋을 복제해 사용 (SetFloat가 에셋을 오염하지 않도록)
            _material = Instantiate(_beamMaterial);
            _material.name = _beamMaterial.name + " (instance)";
        }

        var meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = _material;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    /// <summary>
    /// 타일 XZ 크기 × _beamHeight 높이의 "벽 4면 + 바닥" 메시를 생성합니다.
    /// 벽 UV: x = 변 방향 0..1, y = 높이 0(바닥)..1(꼭대기) / 바닥 UV: 0..1 평면
    /// </summary>
    private void Rebuild()
    {
        if (_mesh == null)
        {
            return;
        }

        float hx = Mathf.Max(0.01f, _tileSize.x * 0.5f);
        float hz = Mathf.Max(0.01f, _tileSize.z * 0.5f);

        // 벽 4개(4정점 2삼각형 × 4) + 바닥 1개(4정점 2삼각형)
        Vector3[] verts = new Vector3[20];
        Vector3[] normals = new Vector3[20];
        Vector2[] uvs = new Vector2[20];
        int[] tris = new int[5 * 6];

        // 벽 정의: (중심 오프셋 방향, 로컬 폭 축(u), 폭 길이)
        (Vector3 offset, Vector3 uAxis, float width)[] walls =
        {
            (Vector3.back * hz, Vector3.right, _tileSize.x),
            (Vector3.right * hx, Vector3.forward, _tileSize.z),
            (Vector3.forward * hz, Vector3.left, _tileSize.x),
            (Vector3.left * hx, Vector3.back, _tileSize.z),
        };

        for (int w = 0; w < 4; w++)
        {
            Vector3 origin = -walls[w].uAxis * (walls[w].width * 0.5f) + walls[w].offset;
            int b = w * 4;
            verts[b + 0] = origin;
            verts[b + 1] = origin + walls[w].uAxis * walls[w].width;
            verts[b + 2] = verts[b + 1] + Vector3.up * _beamHeight;
            verts[b + 3] = origin + Vector3.up * _beamHeight;

            normals[b + 0] = walls[w].offset.normalized;
            normals[b + 1] = walls[w].offset.normalized;
            normals[b + 2] = walls[w].offset.normalized;
            normals[b + 3] = walls[w].offset.normalized;

            uvs[b + 0] = new Vector2(0f, 0f);
            uvs[b + 1] = new Vector2(1f, 0f);
            uvs[b + 2] = new Vector2(1f, 1f);
            uvs[b + 3] = new Vector2(0f, 1f);

            int tb = w * 6;
            // Cull Off이므로 한 방향 인덱스만으로 안/밖 모두 보임
            tris[tb + 0] = b + 0;
            tris[tb + 1] = b + 1;
            tris[tb + 2] = b + 2;
            tris[tb + 3] = b + 0;
            tris[tb + 4] = b + 2;
            tris[tb + 5] = b + 3;
        }

        // 바닥 쿼드 (index 16..19) — 위를 향함
        int f = 16;
        verts[f + 0] = new Vector3(-hx, 0f, -hz);
        verts[f + 1] = new Vector3(hx, 0f, -hz);
        verts[f + 2] = new Vector3(hx, 0f, hz);
        verts[f + 3] = new Vector3(-hx, 0f, hz);

        normals[f + 0] = Vector3.up;
        normals[f + 1] = Vector3.up;
        normals[f + 2] = Vector3.up;
        normals[f + 3] = Vector3.up;

        uvs[f + 0] = new Vector2(0f, 0f);
        uvs[f + 1] = new Vector2(1f, 0f);
        uvs[f + 2] = new Vector2(1f, 1f);
        uvs[f + 3] = new Vector2(0f, 1f);

        int tf = 24;
        tris[tf + 0] = f + 0;
        tris[tf + 1] = f + 1;
        tris[tf + 2] = f + 2;
        tris[tf + 3] = f + 0;
        tris[tf + 4] = f + 2;
        tris[tf + 5] = f + 3;

        _mesh.Clear();
        _mesh.vertices = verts;
        _mesh.normals = normals;
        _mesh.uv = uvs;
        _mesh.triangles = tris;
        _mesh.RecalculateBounds();
    }
}
