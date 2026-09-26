using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 맵 가장자리에서 위로 올라오는 빔(벽) 이펙트.
/// 맵의 콜라이더를 그리드로 샘플링해 실제 footprint 윤곽(마칭 스퀘어)을 계산하고,
/// 그 윤곽선을 따라 벽 빔 + 안쪽 링을 생성합니다. 윤곽 추출 실패 시 AABB 폴백.
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

    [Header("맵 가장자리로부터의 여유 마진 (월드 유닛)")]
    [SerializeField]
    private float _margin = 0.15f;

    [Header("footprint 샘플 격자 간격 (월드 유닛)")]
    [SerializeField]
    private float _samplingResolution = 0.5f;

    [Header("바닥 안쪽 링 너비 (월드 유닛)")]
    [SerializeField]
    private float _ringWidth = 0.4f;

    [Header("페이드 속도 (초)")]
    [SerializeField]
    private float _fadeDuration = 0.25f;

    private static readonly int BlockedID = Shader.PropertyToID("_Blocked");
    private static readonly int HeightID = Shader.PropertyToID("_Height");
    private static readonly int FadeID = Shader.PropertyToID("_Fade");

    // AABB 폴백용 임시 규격
    private Vector3 _fallbackTileSize = new Vector3(12f, 1f, 10f);

    private Mesh _mesh;
    private Material _material;
    private bool _blocked;
    private bool _hasFit;
    private Vector3 _fitBottomCenter;   // 월드 좌표, 빔 바닥 중심

    private Coroutine _fadeCoroutine;
    private float _fadeTarget = 1f;
    private bool _holdUntilInvisible;   // 페이드아웃 완료 전까지 위치 갱신 보류
    private bool _hasPendingFit;
    private GameObject _pendingMap;
    private Bounds _pendingBounds;
    private bool _pendingHasBounds;

    private static readonly Collider[] s_overlapBuffer = new Collider[32];

    private void LateUpdate()
    {
        // FloatingCursor 보빙과 무관하게, 측정된 빔 바닥에 고정
        if (!_hasFit || _mesh == null)
        {
            return;
        }
        transform.position = _fitBottomCenter;
    }

    public float BeamHeight
    {
        get => _beamHeight;
        set
        {
            _beamHeight = value;
            RebuildFromCurrentFit();
        }
    }

    /// <summary>
    /// AABB 폴백 규격을 설정합니다. 실제 맵 모양은 첫 <see cref="FitToMap"/>에서 결정됩니다.
    /// </summary>
    public void SetTileSize(Vector3 tileSize)
    {
        _fallbackTileSize = tileSize;
    }

    /// <summary>
    /// 맵 인스턴스의 콜라이더 footprint를 따라 빔을 생성합니다.
    /// 윤곽 추출에 실패하면 바운딩 박스 AABB로 폴백합니다.
    /// 페이드아웃 중(위치 홀드)에는 적용이 보류되고, 페이드아웃 완료 후(안 보일 때) 적용됩니다.
    /// </summary>
    public void FitToMap(GameObject mapInstance, Bounds worldBounds)
    {
        if (_holdUntilInvisible)
        {
            _pendingMap = mapInstance;
            _pendingBounds = worldBounds;
            _pendingHasBounds = true;
            _hasPendingFit = true;
            return;
        }

        if (mapInstance == null)
        {
            ApplyAabb(worldBounds);
            return;
        }

        // 물리 상태 동기화 (인스턴스 직후 쿼리 대비)
        Physics.SyncTransforms();

        List<Vector2> segments = ExtractFootprintSegments(mapInstance, worldBounds);
        if (segments == null || segments.Count == 0)
        {
            CustomDebug.Log($"[MapSelectionBeam] footprint 추출 실패 → AABB 폴백 ({mapInstance.name})");
            ApplyAabb(worldBounds);
            return;
        }

        float bottomY = worldBounds.min.y - 0.05f;
        ApplyContour(segments, bottomY, worldBounds.center);
    }

    /// <summary>
    /// 월드 바운딩 박스에 빔을 맞춥니다 (AABB 폴백). 마진만큼 사방을 확장합니다.
    /// </summary>
    public void FitToBounds(Bounds worldBounds)
    {
        if (_holdUntilInvisible)
        {
            _pendingMap = null;
            _pendingBounds = worldBounds;
            _pendingHasBounds = true;
            _hasPendingFit = true;
            return;
        }

        ApplyAabb(worldBounds);
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

    /// <summary>빔이 보이는 상태인지 여부.</summary>
    public bool IsVisible
    {
        get
        {
            if (_material == null)
            {
                return false;
            }
            return _material.GetFloat(FadeID) > 0.01f;
        }
    }

    /// <summary>
    /// 빔을 현재 위치에 고정한 채 서서히 사라지게 합니다.
    /// 페이드아웃 동안 들어오는 Fit 요청은 완전히 사라진 뒤(안 보일 때) 적용됩니다.
    /// </summary>
    public void FadeOut()
    {
        _holdUntilInvisible = true;
        StartFade(0f);
    }

    /// <summary>빔을 서서히 나타나게 합니다.</summary>
    public void FadeIn()
    {
        _holdUntilInvisible = false;
        StartFade(1f);
    }

    private void StartFade(float target)
    {
        if (_material == null)
        {
            return;
        }

        _fadeTarget = target;
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }
        _fadeCoroutine = StartCoroutine(FadeRoutine(target));
    }

    private System.Collections.IEnumerator FadeRoutine(float target)
    {
        float start = _material.GetFloat(FadeID);
        float elapsed = 0f;

        while (elapsed < _fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, _fadeDuration));
            _material.SetFloat(FadeID, Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t)));
            yield return null;
        }

        _material.SetFloat(FadeID, target);
        _fadeCoroutine = null;

        // 페이드아웃이 끝났고 보류된 Fit이 있으면, 안 보이는 지금 적용(스냅)
        if (target == 0f && _hasPendingFit)
        {
            _hasPendingFit = false;
            _holdUntilInvisible = false;
            if (_pendingHasBounds)
            {
                if (_pendingMap != null)
                {
                    FitToMap(_pendingMap, _pendingBounds);
                }
                else
                {
                    ApplyAabb(_pendingBounds);
                }
            }
            _pendingMap = null;
            _pendingHasBounds = false;
        }
    }

    private void Awake()
    {
        EnsureResources();
        // 초기 메시는 만들지 않는다 — MapManager.Start의 첫 Fit이 위치/형태를 결정한다.
        // 그 전까지는 렌더러만 비활성 상태로 둔다.
        GetComponent<MeshRenderer>().enabled = false;
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

    // ------------------------------------------------------------------
    // AABB 폴백
    // ------------------------------------------------------------------

    private void ApplyAabb(Bounds worldBounds)
    {
        float m = Mathf.Max(0f, _margin);
        Vector3 size = worldBounds.size + new Vector3(m * 2f, 0f, m * 2f);
        size.x = Mathf.Max(0.01f, size.x);
        size.z = Mathf.Max(0.01f, size.z);

        Vector3 bottomCenter = new Vector3(
            worldBounds.center.x,
            worldBounds.min.y - 0.05f,
            worldBounds.center.z);

        var aabbSegments = BuildAabbGeometry(size, bottomCenter);
        _fitBottomCenter = bottomCenter;
        _hasFit = true;
        GetComponent<MeshRenderer>().enabled = true;
        BuildMeshFromSegments(aabbSegments);
    }

    private List<Vector2> BuildAabbGeometry(Vector3 size, Vector3 bottomCenter)
    {
        // 반시계 방향 사각 윤곽 — 월드 XZ 절대 좌표로 생성 (다른 세그먼트와 좌표계 통일)
        float hx = size.x * 0.5f;
        float hz = size.z * 0.5f;
        var loop = new List<Vector2>(4)
        {
            new Vector2(bottomCenter.x - hx, bottomCenter.z - hz),
            new Vector2(bottomCenter.x + hx, bottomCenter.z - hz),
            new Vector2(bottomCenter.x + hx, bottomCenter.z + hz),
            new Vector2(bottomCenter.x - hx, bottomCenter.z + hz),
        };
        return ContourToSegments(new List<List<Vector2>> { loop });
    }

    // ------------------------------------------------------------------
    // Footprint 샘플링 + 마칭 스퀘어
    // ------------------------------------------------------------------

    /// <summary>
    /// 맵 콜라이더를 그리드로 샘플링해 footprint 윤곽 세그먼트(월드 XZ, 평탄 쌍)를 추출합니다.
    /// </summary>
    private List<Vector2> ExtractFootprintSegments(GameObject mapInstance, Bounds worldBounds)
    {
        var colliders = mapInstance.GetComponentsInChildren<Collider>();
        if (colliders == null || colliders.Length == 0)
        {
            return null;
        }

        // 마진을 더한 확장 영역을 샘플링
        Bounds sampleBounds = worldBounds;
        sampleBounds.Expand(new Vector3(_margin * 2f, 0f, _margin * 2f));

        float res = Mathf.Max(0.1f, _samplingResolution);

        // 셀 기반 샘플링: 각 셀의 중심에서 콜라이더 적중을 검사하고,
        // 밖(=false)셀과 안(=true)셀 사이에서 경계가 검출되도록 한 겹의 바깥 패딩을 둔다.
        int paddedCols = Mathf.CeilToInt(sampleBounds.size.x / res) + 2;
        int paddedRows = Mathf.CeilToInt(sampleBounds.size.z / res) + 2;

        // 과도한 해상도 방지
        int maxGrid = 96;
        if (paddedCols > maxGrid || paddedRows > maxGrid)
        {
            float scale = Mathf.Max((float)paddedCols / maxGrid, (float)paddedRows / maxGrid);
            res *= scale;
            paddedCols = Mathf.CeilToInt(sampleBounds.size.x / res) + 2;
            paddedRows = Mathf.CeilToInt(sampleBounds.size.z / res) + 2;
        }

        // 패딩 제외한 실제 샘플 영역: sampleBounds를 셀 중심들로 덮도록 조정
        float cellOriginX = sampleBounds.min.x + (sampleBounds.size.x - (paddedCols - 2) * res) * 0.5f - res * 0.5f;
        float cellOriginZ = sampleBounds.min.z + (sampleBounds.size.z - (paddedRows - 2) * res) * 0.5f - res * 0.5f;
        float centerY = sampleBounds.center.y;
        float halfHeight = Mathf.Max(0.5f, sampleBounds.extents.y);

        // 샘플: true면 셀이 맵 내부. 가장자리 1열/1행은 항상 false(바깥 보장)
        bool[,] inside = new bool[paddedRows, paddedCols];
        Vector3 half = new Vector3(res * 0.45f, halfHeight, res * 0.45f);

        for (int i = 1; i < paddedRows - 1; i++)
        {
            float z = cellOriginZ + (i - 0.5f) * res;
            for (int j = 1; j < paddedCols - 1; j++)
            {
                float x = cellOriginX + (j - 0.5f) * res;
                Vector3 center = new Vector3(x, centerY, z);

                int hitCount = Physics.OverlapBoxNonAlloc(center, half, s_overlapBuffer, Quaternion.identity);
                bool isInside = false;
                int checkCount = Mathf.Min(hitCount, s_overlapBuffer.Length);
                for (int k = 0; k < checkCount; k++)
                {
                    if (s_overlapBuffer[k] != null && s_overlapBuffer[k].transform.IsChildOf(mapInstance.transform))
                    {
                        isInside = true;
                        break;
                    }
                }
                inside[i, j] = isInside;
            }
        }

        // 마칭 스퀘어셀 기준 좌표: 셀 (i,j)의 코너는 원점 기반 격자 지점.
        // 마칭 스퀘어는 "샘플 지점" 그리드로 다시 해석: inside를 노드로 보고 노드 사이에서 경계.
        // 패딩(=false) 프레임이 있으므로 모든 외곽 경계가 검출된다.
        float nodeOriginX = cellOriginX - res * 0.5f;
        float nodeOriginZ = cellOriginZ - res * 0.5f;
        List<Vector2> contour = MarchingSquares(inside, nodeOriginX, nodeOriginZ, res);
        if (contour == null || contour.Count == 0)
        {
            return null;
        }

        return contour;
    }

    /// <summary>
    /// 이진 그리드에서 마칭 스퀘어로 윤곽 세그먼트를 추출합니다.
    /// 반환은 [a, b, a, b, ...] 형태의 월드 XZ 평탄 세그먼트 목록입니다.
    /// </summary>
    private static List<Vector2> MarchingSquares(bool[,] grid, float originX, float originZ, float res)
    {
        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);
        if (rows < 2 || cols < 2)
        {
            return null;
        }

        // 그리드 지점 → 월드 XZ
        Vector2 P(int i, int j) => new Vector2(originX + j * res, originZ + i * res);
        // 두 지점의 중점
        Vector2 M(int i0, int j0, int i1, int j1) => (P(i0, j0) + P(i1, j1)) * 0.5f;

        // 루프 추적: 각 내부 엣지(두 샘플 값이 다른 인접 쌍)를 노드로 연결
        // 구현 단순화를 위해 세그먼트 기반으로 수집한 뒤 연결한다.
        var segments = new List<KeyValuePair<Vector2, Vector2>>();

        for (int i = 0; i < rows - 1; i++)
        {
            for (int j = 0; j < cols - 1; j++)
            {
                bool tl = grid[i, j];
                bool tr = grid[i, j + 1];
                bool br = grid[i + 1, j + 1];
                bool bl = grid[i + 1, j];

                int code = (tl ? 1 : 0) | (tr ? 2 : 0) | (br ? 4 : 0) | (bl ? 8 : 0);
                if (code == 0 || code == 15)
                {
                    continue;
                }

                Vector2 top = M(i, j, i, j + 1);          // tl-tr
                Vector2 right = M(i, j + 1, i + 1, j + 1); // tr-br
                Vector2 bottom = M(i + 1, j, i + 1, j + 1);// bl-br
                Vector2 left = M(i, j, i + 1, j);          // tl-bl

                switch (code)
                {
                    case 1: segments.Add(new KeyValuePair<Vector2, Vector2>(left, top)); break;      // tl
                    case 2: segments.Add(new KeyValuePair<Vector2, Vector2>(top, right)); break;     // tr
                    case 3: segments.Add(new KeyValuePair<Vector2, Vector2>(left, right)); break;    // tl+tr
                    case 4: segments.Add(new KeyValuePair<Vector2, Vector2>(right, bottom)); break;  // br
                    case 5: segments.Add(new KeyValuePair<Vector2, Vector2>(top, left));             // tl+br (모호)
                            segments.Add(new KeyValuePair<Vector2, Vector2>(bottom, right)); break;
                    case 6: segments.Add(new KeyValuePair<Vector2, Vector2>(top, bottom)); break;    // tr+br
                    case 7: segments.Add(new KeyValuePair<Vector2, Vector2>(left, top)); break;      // tl 제외
                    case 8: segments.Add(new KeyValuePair<Vector2, Vector2>(bottom, left)); break;   // bl
                    case 9: segments.Add(new KeyValuePair<Vector2, Vector2>(bottom, top)); break;    // tl+bl
                    case 10: segments.Add(new KeyValuePair<Vector2, Vector2>(top, right));           // tr+bl (모호)
                             segments.Add(new KeyValuePair<Vector2, Vector2>(bottom, left)); break;
                    case 11: segments.Add(new KeyValuePair<Vector2, Vector2>(right, bottom)); break; // br 제외
                    case 12: segments.Add(new KeyValuePair<Vector2, Vector2>(left, right)); break;   // bl+br
                    case 13: segments.Add(new KeyValuePair<Vector2, Vector2>(top, right)); break;    // tr 제외
                    case 14: segments.Add(new KeyValuePair<Vector2, Vector2>(bottom, left)); break;  // bl 제외
                }
            }
        }

        if (segments.Count == 0)
        {
            return null;
        }

        // 루프 연결 없이 세그먼트를 그대로 사용한다.
        // 벽 쿼드는 개별 선분만으로 만들 수 있고, 링의 안쪽 판정은 pivot 방향으로 하므로
        // 닫힌 루프 완성에 의존하지 않는다 (epsilion 연결 실패 방지).
        var flat = new List<Vector2>(segments.Count * 2);
        foreach (var seg in segments)
        {
            flat.Add(seg.Key);
            flat.Add(seg.Value);
        }
        return flat;
    }

    // ------------------------------------------------------------------
    // 지오메트리 생성
    // ------------------------------------------------------------------

    private List<Vector2> ContourToSegments(List<List<Vector2>> loops)
    {
        var flat = new List<Vector2>();
        foreach (var loop in loops)
        {
            for (int i = 0; i < loop.Count; i++)
            {
                Vector2 a = loop[i];
                Vector2 b = loop[(i + 1) % loop.Count];
                flat.Add(a);
                flat.Add(b);
            }
        }
        return flat;
    }

    /// <summary>
    /// 윤곽 세그먼트(월드 XZ 평탄 쌍)로 벽 빔 + 안쪽 링 메시를 만듭니다.
    /// </summary>
    private void ApplyContour(List<Vector2> segments, float bottomY, Vector3 boundsCenter)
    {
        Vector3 pivot = new Vector3(boundsCenter.x, bottomY, boundsCenter.z);
        _fitBottomCenter = pivot;
        _hasFit = true;
        GetComponent<MeshRenderer>().enabled = true;
        BuildMeshFromSegments(segments, pivot);
    }

    /// <summary>
    /// 세그먼트 목록(월드 XZ 평탄 쌍)을 pivot 기준 로컬 메시로 변환해 적용합니다.
    /// </summary>
    private void BuildMeshFromSegments(List<Vector2> segments)
    {
        BuildMeshFromSegments(segments, _fitBottomCenter);
    }

    /// <summary>
    /// 세그먼트 목록(월드 XZ 평탄 쌍)을 pivot 기준 로컬 메시로 변환해 적용합니다.
    /// </summary>
    private void BuildMeshFromSegments(List<Vector2> segments, Vector3 pivot)
    {
        if (_mesh == null)
        {
            return;
        }

        var walls = new List<Vector3>(segments.Count * 4);
        var wallNormals = new List<Vector3>(segments.Count * 4);
        var uvs = new List<Vector2>(segments.Count * 4);
        var tris = new List<int>(segments.Count * 12);

        // 벽: 세그먼트마다 쿼드 1개. UV.x는 0.5 고정(스캔만 사용), UV.y는 높이.
        for (int s = 0; s < segments.Count; s += 2)
        {
            Vector2 a = segments[s];
            Vector2 b = segments[s + 1];

            Vector2 dir = b - a;
            float len = dir.magnitude;
            if (len < 0.0001f)
            {
                continue;
            }
            Vector3 normal = new Vector3(-dir.y, 0f, dir.x) / len;

            int startVertex = walls.Count;
            walls.Add(new Vector3(a.x - pivot.x, 0f, a.y - pivot.z));
            walls.Add(new Vector3(b.x - pivot.x, 0f, b.y - pivot.z));
            walls.Add(new Vector3(b.x - pivot.x, _beamHeight, b.y - pivot.z));
            walls.Add(new Vector3(a.x - pivot.x, _beamHeight, a.y - pivot.z));

            for (int k = 0; k < 4; k++)
            {
                wallNormals.Add(normal);
            }

            uvs.Add(new Vector2(0.5f, 0f));
            uvs.Add(new Vector2(0.5f, 0f));
            uvs.Add(new Vector2(0.5f, 1f));
            uvs.Add(new Vector2(0.5f, 1f));

            tris.Add(startVertex + 0);
            tris.Add(startVertex + 1);
            tris.Add(startVertex + 2);
            tris.Add(startVertex + 0);
            tris.Add(startVertex + 2);
            tris.Add(startVertex + 3);
        }

        // 안쪽 링: 벽 안쪽으로 _ringWidth 만큼 들어온 평행 쿼드 (바닥 하이라이트)
        float ringY = 0.02f;
        for (int s = 0; s < segments.Count; s += 2)
        {
            Vector2 a = segments[s];
            Vector2 b = segments[s + 1];

            Vector2 dir = b - a;
            float len = dir.magnitude;
            if (len < 0.0001f || _ringWidth <= 0.001f)
            {
                continue;
            }
            Vector2 perp = new Vector2(-dir.y, dir.x) / len;

            // 루프 중심을 향하는 쪽이 안쪽
            Vector2 mid = (a + b) * 0.5f;
            Vector3 toCenter3 = pivot - new Vector3(mid.x, 0f, mid.y);
            if (Vector2.Dot(perp, new Vector2(toCenter3.x, toCenter3.z)) < 0f)
            {
                perp = -perp;
            }

            Vector2 aIn = a + perp * _ringWidth;
            Vector2 bIn = b + perp * _ringWidth;

            int startVertex = walls.Count;
            walls.Add(new Vector3(a.x - pivot.x, ringY, a.y - pivot.z));
            walls.Add(new Vector3(b.x - pivot.x, ringY, b.y - pivot.z));
            walls.Add(new Vector3(bIn.x - pivot.x, ringY, bIn.y - pivot.z));
            walls.Add(new Vector3(aIn.x - pivot.x, ringY, aIn.y - pivot.z));

            for (int k = 0; k < 4; k++)
            {
                wallNormals.Add(Vector3.up); // 쉐이더의 floor(링) 브랜치 사용
            }

            uvs.Add(new Vector2(0f, 0.5f));
            uvs.Add(new Vector2(1f, 0.5f));
            uvs.Add(new Vector2(1f, 0.5f));
            uvs.Add(new Vector2(0f, 0.5f));

            tris.Add(startVertex + 0);
            tris.Add(startVertex + 1);
            tris.Add(startVertex + 2);
            tris.Add(startVertex + 0);
            tris.Add(startVertex + 2);
            tris.Add(startVertex + 3);
        }

        if (tris.Count == 0)
        {
            return;
        }

        _mesh.Clear();
        _mesh.SetVertices(walls);
        _mesh.SetNormals(wallNormals);
        _mesh.SetUVs(0, uvs);
        _mesh.SetTriangles(tris, 0);
        _mesh.RecalculateBounds();

        if (_material != null && _material.HasProperty(HeightID))
        {
            _material.SetFloat(HeightID, _beamHeight);
        }
    }

    /// <summary>현재 Fit 상태에서 메시를 다시 만듭니다 (높이 변경 등).</summary>
    private void RebuildFromCurrentFit()
    {
        // 윤곽 정보를 지니고 있지 않으므로, 높이 동기화만 수행.
        // 다음 Fit 호출 시 최신 높이가 반영됨.
        if (_material != null && _material.HasProperty(HeightID))
        {
            _material.SetFloat(HeightID, _beamHeight);
        }
    }
}
