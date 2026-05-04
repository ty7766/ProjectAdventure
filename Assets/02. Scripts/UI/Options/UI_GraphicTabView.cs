using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class UI_GraphicTabView : MonoBehaviour
{
    [Header("Display Settings")]
    [SerializeField] private TMP_Dropdown _dropDownDisplayMode;
    [SerializeField] private TMP_Dropdown _dropDownResolution;
    [SerializeField] private TMP_Dropdown _dropDownAA;
    [SerializeField] private TMP_Dropdown _dropDownVSync;
    [SerializeField] private Slider _sliderGamma;
    [SerializeField] private TextMeshProUGUI _textGammaValue;

    [Header("Graphic Quality Settings")]
    [SerializeField] private Slider _sliderRenderScale;
    [SerializeField] private TextMeshProUGUI _textRenderScaleValue;
    [SerializeField] private TMP_Dropdown _dropDownTextureQuality;
    [SerializeField] private TMP_Dropdown _dropDownTextureFiltering;
    [SerializeField] private TMP_Dropdown _dropDownShadowQuality;

    [Header("Post Processing Settings")]
    [SerializeField] private TMP_Dropdown _dropDownBloom;
    [SerializeField] private TMP_Dropdown _dropDownChromaticAberration;
    [SerializeField] private TMP_Dropdown _dropDownDepthOfField;

    [Header("Performance Monitor Settings")]
    [SerializeField] private TMP_Dropdown _dropDownFpsMonitor;

    private UI_GraphicTabPresenter _presenter;

    private void Awake()
    {
        _presenter = new UI_GraphicTabPresenter(this);
    }

    private void Start()
    {
        _presenter.Initialize();
        if (_sliderGamma != null) _sliderGamma.onValueChanged.AddListener(_ => OnSliderValueChanged());
        if (_sliderRenderScale != null) _sliderRenderScale.onValueChanged.AddListener(_ => OnSliderValueChanged());

        SetupRenderScaleDragEnd();
    }

    private void SetupRenderScaleDragEnd()
    {
        if (_sliderRenderScale == null) return;

        EventTrigger trigger = _sliderRenderScale.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = _sliderRenderScale.gameObject.AddComponent<EventTrigger>();
        }

        EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry();
        pointerUpEntry.eventID = EventTriggerType.PointerUp;
        pointerUpEntry.callback.AddListener((data) => _onRenderScaleDragEnd?.Invoke(_sliderRenderScale.value));
        trigger.triggers.Add(pointerUpEntry);
    }

    private UnityAction<float> _onRenderScaleDragEnd;

    public void BindRenderScaleDragEnd(UnityAction<float> onDragEnd)
    {
        _onRenderScaleDragEnd = onDragEnd;
    }

    private void OnDestroy()
    {
        if (_dropDownDisplayMode != null) _dropDownDisplayMode.onValueChanged.RemoveAllListeners();
        if (_dropDownResolution != null) _dropDownResolution.onValueChanged.RemoveAllListeners();
        if (_dropDownAA != null) _dropDownAA.onValueChanged.RemoveAllListeners();
        if (_dropDownVSync != null) _dropDownVSync.onValueChanged.RemoveAllListeners();
        if (_sliderGamma != null) _sliderGamma.onValueChanged.RemoveAllListeners();
        if (_sliderRenderScale != null) _sliderRenderScale.onValueChanged.RemoveAllListeners();
        if (_dropDownTextureQuality != null) _dropDownTextureQuality.onValueChanged.RemoveAllListeners();
        if (_dropDownTextureFiltering != null) _dropDownTextureFiltering.onValueChanged.RemoveAllListeners();
        if (_dropDownShadowQuality != null) _dropDownShadowQuality.onValueChanged.RemoveAllListeners();
        if (_dropDownBloom != null) _dropDownBloom.onValueChanged.RemoveAllListeners();
        if (_dropDownChromaticAberration != null) _dropDownChromaticAberration.onValueChanged.RemoveAllListeners();
        if (_dropDownDepthOfField != null) _dropDownDepthOfField.onValueChanged.RemoveAllListeners();
        if (_dropDownFpsMonitor != null) _dropDownFpsMonitor.onValueChanged.RemoveAllListeners();
    }

    public void BindDisplaySettings(UnityAction<int> onDisplayMode, UnityAction<int> onRes, UnityAction<int> onAA, UnityAction<int> onVSync, UnityAction<float> onGamma)
    {
        if (onDisplayMode != null) _dropDownDisplayMode?.onValueChanged.AddListener(onDisplayMode);
        if (onRes != null) _dropDownResolution?.onValueChanged.AddListener(onRes);
        if (onAA != null) _dropDownAA?.onValueChanged.AddListener(onAA);
        if (onVSync != null) _dropDownVSync?.onValueChanged.AddListener(onVSync);
        if (onGamma != null) _sliderGamma?.onValueChanged.AddListener(onGamma);
    }

    public void BindQualitySettings(UnityAction<float> onRenderScale, UnityAction<int> onTexture, UnityAction<int> onFiltering, UnityAction<int> onShadow)
    {
        if (onRenderScale != null) _sliderRenderScale?.onValueChanged.AddListener(onRenderScale);
        if (onTexture != null) _dropDownTextureQuality?.onValueChanged.AddListener(onTexture);
        if (onFiltering != null) _dropDownTextureFiltering?.onValueChanged.AddListener(onFiltering);
        if (onShadow != null) _dropDownShadowQuality?.onValueChanged.AddListener(onShadow);
    }

    public void BindPostProcessSettings(UnityAction<int> onBloom, UnityAction<int> onCA, UnityAction<int> onDOF)
    {
        if (onBloom != null) _dropDownBloom?.onValueChanged.AddListener(onBloom);
        if (onCA != null) _dropDownChromaticAberration?.onValueChanged.AddListener(onCA);
        if (onDOF != null) _dropDownDepthOfField?.onValueChanged.AddListener(onDOF);
    }

    public void BindPerformanceMonitorSettings(UnityAction<int> onFpsMonitorDropdown)
    {
        if (onFpsMonitorDropdown != null) _dropDownFpsMonitor?.onValueChanged.AddListener(onFpsMonitorDropdown);
    }

    public void UpdateResolutionOptions(System.Collections.Generic.List<string> options)
    {
        if (_dropDownResolution != null)
        {
            _dropDownResolution.ClearOptions();
            if (options != null) _dropDownResolution.AddOptions(options);
        }
    }

    public int DisplayModeIndex
    {
        get => _dropDownDisplayMode != null ? _dropDownDisplayMode.value : 0;
        set { if (_dropDownDisplayMode != null) _dropDownDisplayMode.value = value; }
    }
    public int ResolutionIndex
    {
        get => _dropDownResolution != null ? _dropDownResolution.value : 0;
        set { if (_dropDownResolution != null) _dropDownResolution.value = value; }
    }
    public int AAIndex
    {
        get => _dropDownAA != null ? _dropDownAA.value : 0;
        set { if (_dropDownAA != null) _dropDownAA.value = value; }
    }
    public int VSyncIndex
    {
        get => _dropDownVSync != null ? _dropDownVSync.value : 0;
        set { if (_dropDownVSync != null) _dropDownVSync.value = value; }
    }
    public float GammaValue
    {
        get => _sliderGamma != null ? _sliderGamma.value : 0f;
        set
        {
            if (_sliderGamma != null) _sliderGamma.value = value;
            if (_textGammaValue != null) _textGammaValue.text = $"{value:F2}";
        }
    }

    public float RenderScale
    {
        get => _sliderRenderScale != null ? _sliderRenderScale.value : 0f;
        set
        {
            if (_sliderRenderScale != null) _sliderRenderScale.value = value;
            if (_textRenderScaleValue != null) _textRenderScaleValue.text = $"{value:F0}%";
        }
    }
    public int TextureQualityIndex
    {
        get => _dropDownTextureQuality != null ? _dropDownTextureQuality.value : 0;
        set { if (_dropDownTextureQuality != null) _dropDownTextureQuality.value = value; }
    }
    public int TextureFilteringIndex
    {
        get => _dropDownTextureFiltering != null ? _dropDownTextureFiltering.value : 0;
        set { if (_dropDownTextureFiltering != null) _dropDownTextureFiltering.value = value; }
    }
    public int ShadowQualityIndex
    {
        get => _dropDownShadowQuality != null ? _dropDownShadowQuality.value : 0;
        set { if (_dropDownShadowQuality != null) _dropDownShadowQuality.value = value; }
    }

    public int BloomIndex
    {
        get => _dropDownBloom != null ? _dropDownBloom.value : 0;
        set { if (_dropDownBloom != null) _dropDownBloom.value = value; }
    }
    public int ChromaticAberrationIndex
    {
        get => _dropDownChromaticAberration != null ? _dropDownChromaticAberration.value : 0;
        set { if (_dropDownChromaticAberration != null) _dropDownChromaticAberration.value = value; }
    }
    public int DepthOfFieldIndex
    {
        get => _dropDownDepthOfField != null ? _dropDownDepthOfField.value : 0;
        set { if (_dropDownDepthOfField != null) _dropDownDepthOfField.value = value; }
    }

    public int FpsMonitorIndex
    {
        get => _dropDownFpsMonitor != null ? _dropDownFpsMonitor.value : 0;
        set { if (_dropDownFpsMonitor != null) _dropDownFpsMonitor.value = value; }
    }

    private void OnValidate()
    {
        // 에디터에서 슬라이더 값이 변경될 때 텍스트 업데이트
        if (_sliderGamma != null && _textGammaValue != null)
        {
            _textGammaValue.text = $"{_sliderGamma.value:F2}";
        }
        if (_sliderRenderScale != null && _textRenderScaleValue != null)
        {
            _textRenderScaleValue.text = $"{_sliderRenderScale.value:F0}%";
        }
    }

    private void OnSliderValueChanged()
    {
        // 슬라이더 값이 변경될 때 텍스트 업데이트
        if (_sliderGamma != null && _textGammaValue != null)
        {
            _textGammaValue.text = $"{_sliderGamma.value:F2}";
        }
        if (_sliderRenderScale != null && _textRenderScaleValue != null)
        {
            _textRenderScaleValue.text = $"{_sliderRenderScale.value:F0}%";
        }
    }
}