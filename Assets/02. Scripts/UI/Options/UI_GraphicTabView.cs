using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class UI_GraphicTabView : MonoBehaviour
{
    [Header("Display Settings")]
    [SerializeField] private TMP_Dropdown _dropDownDisplayMode;
    [SerializeField] private TMP_Dropdown _dropDownResolution;
    [SerializeField] private TMP_Dropdown _dropDownAA;
    [SerializeField] private TMP_Dropdown _dropDownVSync;
    [SerializeField] private Slider _sliderGamma;

    [Header("Graphic Quality Settings")]
    [SerializeField] private Slider _sliderRenderScale;
    [SerializeField] private TMP_Dropdown _dropDownTextureQuality;
    [SerializeField] private TMP_Dropdown _dropDownTextureFiltering;
    [SerializeField] private TMP_Dropdown _dropDownShadowQuality;

    [Header("Post Processing Settings")]
    [SerializeField] private TMP_Dropdown _dropDownBloom;
    [SerializeField] private TMP_Dropdown _dropDownChromaticAberration;
    [SerializeField] private TMP_Dropdown _dropDownDepthOfField;

    private UI_GraphicTabPresenter _presenter;

    private void Awake()
    {
        _presenter = new UI_GraphicTabPresenter(this);
    }

    private void Start()
    {
        _presenter.Initialize(); // 초기 데이터 로드 및 이벤트 바인딩
    }

    // UI 이벤트를 프레젠터에 연결하기 위한 메서드들
    public void BindDisplaySettings(UnityAction<int> onDisplayMode, UnityAction<int> onRes, UnityAction<int> onAA, UnityAction<int> onVSync, UnityAction<float> onGamma)
    {
        _dropDownDisplayMode.onValueChanged.AddListener(onDisplayMode);
        _dropDownResolution.onValueChanged.AddListener(onRes);
        _dropDownAA.onValueChanged.AddListener(onAA);
        _dropDownVSync.onValueChanged.AddListener(onVSync);
        _sliderGamma.onValueChanged.AddListener(onGamma);
    }

    public void BindQualitySettings(UnityAction<float> onRenderScale, UnityAction<int> onTexture, UnityAction<int> onFiltering, UnityAction<int> onShadow)
    {
        _sliderRenderScale.onValueChanged.AddListener(onRenderScale);
        _dropDownTextureQuality.onValueChanged.AddListener(onTexture);
        _dropDownTextureFiltering.onValueChanged.AddListener(onFiltering);
        _dropDownShadowQuality.onValueChanged.AddListener(onShadow);
    }

    public void BindPostProcessSettings(UnityAction<int> onBloom, UnityAction<int> onCA, UnityAction<int> onDOF)
    {
        _dropDownBloom.onValueChanged.AddListener(onBloom);
        _dropDownChromaticAberration.onValueChanged.AddListener(onCA);
        _dropDownDepthOfField.onValueChanged.AddListener(onDOF);
    }

    public void UpdateResolutionOptions(System.Collections.Generic.List<string> options)
    {
        _dropDownResolution.ClearOptions();
        _dropDownResolution.AddOptions(options);
    }

    // Display Properties
    public int DisplayModeIndex { get => _dropDownDisplayMode.value; set => _dropDownDisplayMode.value = value; }
    public int ResolutionIndex { get => _dropDownResolution.value; set => _dropDownResolution.value = value; }
    public int AAIndex { get => _dropDownAA.value; set => _dropDownAA.value = value; }
    public int VSyncIndex { get => _dropDownVSync.value; set => _dropDownVSync.value = value; }
    public float GammaValue { get => _sliderGamma.value; set => _sliderGamma.value = value; }

    // Graphic Quality Properties
    public float RenderScale { get => _sliderRenderScale.value; set => _sliderRenderScale.value = value; }
    public int TextureQualityIndex { get => _dropDownTextureQuality.value; set => _dropDownTextureQuality.value = value; }
    public int TextureFilteringIndex { get => _dropDownTextureFiltering.value; set => _dropDownTextureFiltering.value = value; }
    public int ShadowQualityIndex { get => _dropDownShadowQuality.value; set => _dropDownShadowQuality.value = value; }

    // Post Processing Properties
    public int BloomIndex { get => _dropDownBloom.value; set => _dropDownBloom.value = value; }
    public int ChromaticAberrationIndex { get => _dropDownChromaticAberration.value; set => _dropDownChromaticAberration.value = value; }
    public int DepthOfFieldIndex { get => _dropDownDepthOfField.value; set => _dropDownDepthOfField.value = value; }
}