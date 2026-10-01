using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 맵 가장자리에서 위로 올라오는 빔(벽) 이펙트의 뷰.
/// 맵 footprint 윤곽(지오메트리 계산은 <see cref="MapSelectionBeamGeometry"/>)을 따라 벽 빔 + 안쪽 링을 그리고,
/// 페이드/교체 불가(붉은색) 상태를 표시합니다. 언제 무엇을 표시할지는 <see cref="MapSelectionBeamPresenter"/>가 결정한다.
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

    private const float BottomOffset = 0.05f;

    private static readonly int BlockedID = Shader.PropertyToID("_Blocked");
    private static readonly int HeightID = Shader.PropertyToID("_Height");
    private static readonly int FadeID = Shader.PropertyToID("_Fade");

    private Mesh _mesh;
    private Material _material;
    private MeshRenderer _meshRenderer;
    private bool _blocked;
    private bool _hasFit;
    private Vector3 _fitBottomCenter;   // 월드 좌표, 빔 바닥 중심
    private List<Vector2> _currentSegments; // 높이 변경 시 재생성용

    private Coroutine _fadeCoroutine;
    private bool _holdUntilInvisible;   // 페이드아웃 완료 전까지 위치 갱신 보류
    private bool _hasPendingFit;
    private GameObject _pendingMap;
    private Bounds _pendingBounds;

    //--- Properties ---//
    public float BeamHeight
    {
        get => _beamHeight;
        set
        {
            _beamHeight = value;
            RebuildFromCurrentFit();
        }
    }

    //--- Unity Methods ---//
    private void Awake()
    {
        EnsureResources();
        // 초기 메시는 만들지 않는다 — 첫 Fit이 위치/형태를 결정한다.
        // 그 전까지는 렌더러만 비활성 상태로 둔다.
        _meshRenderer.enabled = false;
    }

    private void LateUpdate()
    {
        // FloatingCursor 보빙과 무관하게, 측정된 빔 바닥에 고정
        if (!_hasFit || _mesh == null)
        {
            return;
        }
        transform.position = _fitBottomCenter;
    }

    private void OnDisable()
    {
        // 비활성화되면 진행 중이던 페이드 코루틴은 Unity가 중단하므로 참조만 정리한다.
        // 페이드 상태 복구는 Presenter가 ResetFade로 처리한다.
        _fadeCoroutine = null;
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

    //--- Public Methods ---//
    /// <summary>
    /// 맵 인스턴스의 콜라이더 footprint를 따라 빔을 생성합니다.
    /// 윤곽 추출에 실패하면 바운딩 박스 AABB로 폴백합니다.
    /// 페이드아웃 중(위치 홀드)에는 적용이 보류되고, 페이드아웃 완료 후(안 보일 때) 적용됩니다.
    /// </summary>
    public void FitToMap(GameObject mapInstance, Bounds worldBounds)
    {
        if (_holdUntilInvisible)
        {
            SetPendingFit(mapInstance, worldBounds);
            return;
        }

        if (mapInstance == null)
        {
            ApplyAabb(worldBounds);
            return;
        }

        // 물리 상태 동기화 (인스턴스 직후 쿼리 대비)
        Physics.SyncTransforms();

        List<Vector2> segments = MapSelectionBeamGeometry.ExtractFootprintSegments(mapInstance, worldBounds, _margin, _samplingResolution);
        if (segments == null)
        {
            CustomDebug.Log($"[MapSelectionBeam] footprint 추출 실패 → AABB 폴백 ({mapInstance.name})");
            ApplyAabb(worldBounds);
            return;
        }

        Vector3 pivot = new Vector3(worldBounds.center.x, worldBounds.min.y - BottomOffset, worldBounds.center.z);
        ApplySegments(segments, pivot);
    }

    /// <summary>
    /// 월드 바운딩 박스에 빔을 맞춥니다 (AABB 폴백). 마진만큼 사방을 확장합니다.
    /// </summary>
    public void FitToBounds(Bounds worldBounds)
    {
        if (_holdUntilInvisible)
        {
            SetPendingFit(null, worldBounds);
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

    /// <summary>
    /// 빔을 현재 위치에 고정한 채 서서히 사라지게 합니다.
    /// 페이드아웃 동안 들어오는 Fit 요청은 완전히 사라진 뒤(안 보일 때) 적용됩니다.
    /// </summary>
    public void FadeOut()
    {
        if (_material == null)
        {
            return;
        }
        _holdUntilInvisible = true;
        StartFade(0f);
    }

    /// <summary>빔을 서서히 나타나게 합니다.</summary>
    public void FadeIn()
    {
        if (_material == null)
        {
            return;
        }
        // 페이드아웃 도중 FadeIn이 오면 보류된 Fit을 먼저 적용해 최신 위치에서 나타나게 한다
        _holdUntilInvisible = false;
        ApplyPendingFit();
        StartFade(1f);
    }

    /// <summary>
    /// 페이드/보류 상태를 즉시 초기화하고 빔을 완전히 보이는 상태로 되돌립니다.
    /// (스테이지 재시작 등으로 페이드 코루틴이 중단된 경우의 복구용)
    /// </summary>
    public void ResetFade()
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }
        _holdUntilInvisible = false;
        _hasPendingFit = false;
        _pendingMap = null;

        if (_material != null)
        {
            _material.SetFloat(FadeID, 1f);
        }
    }

    //--- Private Methods ---//
    private void EnsureResources()
    {
        var meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();

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

        _meshRenderer.sharedMaterial = _material;
        _meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _meshRenderer.receiveShadows = false;
    }

    private void SetPendingFit(GameObject mapInstance, Bounds worldBounds)
    {
        _pendingMap = mapInstance;
        _pendingBounds = worldBounds;
        _hasPendingFit = true;
    }

    private void ApplyPendingFit()
    {
        if (!_hasPendingFit)
        {
            return;
        }
        _hasPendingFit = false;
        GameObject map = _pendingMap;
        _pendingMap = null;

        if (map != null)
        {
            FitToMap(map, _pendingBounds);
        }
        else
        {
            ApplyAabb(_pendingBounds);
        }
    }

    private void StartFade(float target)
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }

        if (!isActiveAndEnabled)
        {
            // 비활성 상태에서는 코루틴을 돌릴 수 없으므로 즉시 목표 상태로 스냅
            _material.SetFloat(FadeID, target);
            OnFadeFinished(target);
            return;
        }
        _fadeCoroutine = StartCoroutine(FadeRoutine(target));
    }

    private System.Collections.IEnumerator FadeRoutine(float target)
    {
        float start = _material.GetFloat(FadeID);
        float elapsed = 0f;
        float duration = Mathf.Max(0.001f, _fadeDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _material.SetFloat(FadeID, Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t)));
            yield return null;
        }

        _material.SetFloat(FadeID, target);
        _fadeCoroutine = null;
        OnFadeFinished(target);
    }

    private void OnFadeFinished(float target)
    {
        // 페이드아웃이 끝났고 보류된 Fit이 있으면, 안 보이는 지금 적용(스냅)
        if (target <= 0f && _hasPendingFit)
        {
            _holdUntilInvisible = false;
            ApplyPendingFit();
        }
    }

    private void ApplyAabb(Bounds worldBounds)
    {
        float m = Mathf.Max(0f, _margin);
        Vector2 size = new Vector2(
            Mathf.Max(0.01f, worldBounds.size.x + m * 2f),
            Mathf.Max(0.01f, worldBounds.size.z + m * 2f));

        Vector3 pivot = new Vector3(worldBounds.center.x, worldBounds.min.y - BottomOffset, worldBounds.center.z);
        List<Vector2> segments = MapSelectionBeamGeometry.BuildRectangleSegments(new Vector2(pivot.x, pivot.z), size);
        ApplySegments(segments, pivot);
    }

    private void ApplySegments(List<Vector2> segments, Vector3 pivot)
    {
        if (_mesh == null)
        {
            return;
        }

        if (!MapSelectionBeamGeometry.BuildMesh(_mesh, segments, pivot, _beamHeight, _ringWidth))
        {
            return;
        }

        _currentSegments = segments;
        _fitBottomCenter = pivot;
        _hasFit = true;
        _meshRenderer.enabled = true;
        SyncHeightToMaterial();
    }

    /// <summary>현재 Fit 윤곽으로 메시를 다시 만듭니다 (높이 변경 등).</summary>
    private void RebuildFromCurrentFit()
    {
        if (_hasFit && _currentSegments != null && _mesh != null)
        {
            MapSelectionBeamGeometry.BuildMesh(_mesh, _currentSegments, _fitBottomCenter, _beamHeight, _ringWidth);
        }
        SyncHeightToMaterial();
    }

    private void SyncHeightToMaterial()
    {
        if (_material != null && _material.HasProperty(HeightID))
        {
            _material.SetFloat(HeightID, _beamHeight);
        }
    }
}
