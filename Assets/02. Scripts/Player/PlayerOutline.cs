using UnityEngine;

/// <summary>
/// 플레이어 렌더러를 오클루전 아웃라인 전용 레이어에 배치하고,
/// 오버라이드 머티리얼(M_PlayerOccluded)의 외곽선 색/강도 등을 제어합니다.
/// 실제 외곽선 렌더링은 URP Renderer Feature(Render Objects, ZTest=Greater)가 담당합니다.
/// </summary>
[DisallowMultipleComponent]
public class PlayerOutline : MonoBehaviour
{
    private static readonly int ColorId        = Shader.PropertyToID("_Color");
    private static readonly int IntensityId    = Shader.PropertyToID("_Intensity");
    private static readonly int AlphaId        = Shader.PropertyToID("_Alpha");
    private static readonly int RimPowerId     = Shader.PropertyToID("_RimPower");
    private static readonly int RimThresholdId = Shader.PropertyToID("_RimThreshold");
    private static readonly int RimSoftnessId  = Shader.PropertyToID("_RimSoftness");
    private static readonly int FillBiasId     = Shader.PropertyToID("_FillBias");

    //--- Renderer Setup ---//
    [Header("Renderer Setup")]
    [SerializeField, Tooltip("Render Objects 피처가 그릴 레이어 이름. Project Settings > Tags and Layers 에 추가되어 있어야 합니다.")]
    private string _occludedLayerName = "PlayerOccluded";

    [SerializeField, Tooltip("레이어를 적용할 렌더러 목록. 비어 있으면 자식 렌더러를 자동 수집합니다.")]
    private Renderer[] _targetRenderers;

    //--- Outline Material ---//
    [Header("Outline Material")]
    [SerializeField, Tooltip("Render Objects 피처의 Override Material 과 동일한 머티리얼을 지정 (예: M_PlayerOccluded).")]
    private Material _outlineMaterial;

    //--- Outline Look ---//
    [Header("Outline Look")]
    [SerializeField, ColorUsage(true, true), Tooltip("외곽선 컬러 (HDR 가능).")]
    private Color _color = new Color(0.2f, 0.8f, 1.0f, 1.0f);

    [SerializeField, Range(0f, 8f), Tooltip("전반적인 글로우 강도 배수.")]
    private float _intensity = 2.5f;

    [SerializeField, Range(0f, 1f), Tooltip("최종 알파 배수.")]
    private float _alpha = 0.9f;

    [SerializeField, Range(0.1f, 16f), Tooltip("프레넬 곡선의 가파름. 클수록 림이 얇아짐.")]
    private float _rimPower = 5f;

    [SerializeField, Range(0f, 1f), Tooltip("림이 그려지기 시작하는 임계값. 클수록 가장자리에만 남음.")]
    private float _rimThreshold = 0.35f;

    [SerializeField, Range(0.001f, 1f), Tooltip("림 경계의 부드러움(페더링 폭).")]
    private float _rimSoftness = 0.25f;

    [SerializeField, Range(0f, 1f), Tooltip("0 이면 가장자리만, 1 이면 몸체 전체에 채움.")]
    private float _fillBias = 0f;

    //--- Public Properties ---//
    public Color Color
    {
        get => _color;
        set { _color = value; ApplyMaterialProperties(); }
    }

    public float Intensity
    {
        get => _intensity;
        set { _intensity = value; ApplyMaterialProperties(); }
    }

    public float Alpha
    {
        get => _alpha;
        set { _alpha = value; ApplyMaterialProperties(); }
    }

    public float RimPower
    {
        get => _rimPower;
        set { _rimPower = value; ApplyMaterialProperties(); }
    }

    public float RimThreshold
    {
        get => _rimThreshold;
        set { _rimThreshold = value; ApplyMaterialProperties(); }
    }

    public float RimSoftness
    {
        get => _rimSoftness;
        set { _rimSoftness = value; ApplyMaterialProperties(); }
    }

    public float FillBias
    {
        get => _fillBias;
        set { _fillBias = value; ApplyMaterialProperties(); }
    }

    //--- Unity Methods ---//
    private void Reset()
    {
        _targetRenderers = GetComponentsInChildren<Renderer>(true);
    }

    private void Awake()
    {
        AssignOccludedLayer();
        ApplyMaterialProperties();
    }

    private void OnValidate()
    {
        ApplyMaterialProperties();
    }

    //--- Private Methods ---//
    private void AssignOccludedLayer()
    {
        if (_targetRenderers == null || _targetRenderers.Length == 0)
        {
            _targetRenderers = GetComponentsInChildren<Renderer>(true);
        }

        int layer = LayerMask.NameToLayer(_occludedLayerName);
        if (layer < 0)
        {
            Debug.LogError($"[PlayerOutline] '{_occludedLayerName}' 레이어가 없습니다. Project Settings > Tags and Layers 에서 추가해 주세요.", this);
            return;
        }

        for (int i = 0; i < _targetRenderers.Length; i++)
        {
            if (_targetRenderers[i] != null)
            {
                _targetRenderers[i].gameObject.layer = layer;
            }
        }
    }

    private void ApplyMaterialProperties()
    {
        if (_outlineMaterial == null)
        {
            return;
        }

        _outlineMaterial.SetColor(ColorId, _color);
        _outlineMaterial.SetFloat(IntensityId, _intensity);
        _outlineMaterial.SetFloat(AlphaId, _alpha);
        _outlineMaterial.SetFloat(RimPowerId, _rimPower);
        _outlineMaterial.SetFloat(RimThresholdId, _rimThreshold);
        _outlineMaterial.SetFloat(RimSoftnessId, _rimSoftness);
        _outlineMaterial.SetFloat(FillBiasId, _fillBias);
    }
}
