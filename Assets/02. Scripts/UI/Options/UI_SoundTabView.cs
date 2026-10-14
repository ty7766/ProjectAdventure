using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class UI_SoundTabView : MonoBehaviour
{
    [Header("Sound Volume Settings")]
    [SerializeField] private Slider _sliderBGMVolume;
    [SerializeField] private TextMeshProUGUI _textBGMVolumeValue;
    [SerializeField] private Slider _sliderSFXVolume;
    [SerializeField] private TextMeshProUGUI _textSFXVolumeValue;

    [Header("Haptics Settings (Touch UI only)")]
    [SerializeField, Tooltip("진동 설정 섹션(헤더 + 행). 터치 UI 플랫폼에서만 표시된다")]
    private List<GameObject> _hapticsSection;
    [SerializeField, Tooltip("0 = 끔, 1 = 켬")] private TMP_Dropdown _dropDownHaptics;

    private UI_SoundTabPresenter _presenter;

    public bool IsHapticsSectionVisible { get; private set; }

    private void Awake()
    {
        ApplyHapticsSectionVisibility();
        _presenter = new UI_SoundTabPresenter(this);
    }

    private void Start()
    {
        _presenter.Initialize();
        if (_sliderBGMVolume != null) _sliderBGMVolume.onValueChanged.AddListener(_ => OnSliderValueChanged());
        if (_sliderSFXVolume != null) _sliderSFXVolume.onValueChanged.AddListener(_ => OnSliderValueChanged());

        SetupSFXVolumeDragEnd();
    }

    private void SetupSFXVolumeDragEnd()
    {
        if (_sliderSFXVolume == null) return;

        EventTrigger trigger = _sliderSFXVolume.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = _sliderSFXVolume.gameObject.AddComponent<EventTrigger>();
        }

        EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry();
        pointerUpEntry.eventID = EventTriggerType.PointerUp;
        pointerUpEntry.callback.AddListener((data) => _onSFXVolumeDragEnd?.Invoke());
        trigger.triggers.Add(pointerUpEntry);
    }

    private UnityAction _onSFXVolumeDragEnd;

    public void BindSFXVolumeDragEnd(UnityAction onDragEnd)
    {
        _onSFXVolumeDragEnd = onDragEnd;
    }

    private void OnDestroy()
    {
        if (_sliderBGMVolume != null) _sliderBGMVolume.onValueChanged.RemoveAllListeners();
        if (_sliderSFXVolume != null) _sliderSFXVolume.onValueChanged.RemoveAllListeners();
        if (_dropDownHaptics != null) _dropDownHaptics.onValueChanged.RemoveAllListeners();
    }

    /// <summary>
    /// 진동 설정은 터치 UI 플랫폼 전용. 실기기에서는 진동 모터가 없는 기기(일부 태블릿)도 숨긴다.
    /// (에디터 터치 시뮬레이션에서는 진동은 안 나지만 UI 확인을 위해 표시)
    /// </summary>
    private void ApplyHapticsSectionVisibility()
    {
        IsHapticsSectionVisible = PlatformCapability.UseTouchUI && (Application.isEditor || Haptics.IsSupported);
        if (_hapticsSection == null) return;
        foreach (var row in _hapticsSection)
        {
            if (row != null)
            {
                row.SetActive(IsHapticsSectionVisible);
            }
        }
    }

    public void BindHapticsSetting(UnityAction<int> onHaptics)
    {
        if (onHaptics != null) _dropDownHaptics?.onValueChanged.AddListener(onHaptics);
    }

    public int HapticsIndex
    {
        get => _dropDownHaptics != null ? _dropDownHaptics.value : 0;
        set { if (_dropDownHaptics != null) _dropDownHaptics.SetValueWithoutNotify(value); }
    }

    public void BindSoundSettings(UnityAction<float> onBGMVolume, UnityAction<float> onSFXVolume)
    {
        if (onBGMVolume != null) _sliderBGMVolume?.onValueChanged.AddListener(onBGMVolume);
        if (onSFXVolume != null) _sliderSFXVolume?.onValueChanged.AddListener(onSFXVolume);
    }

    public float BGMVolume
    {
        get => _sliderBGMVolume != null ? _sliderBGMVolume.value : 0f;
        set
        {
            if (_sliderBGMVolume != null) _sliderBGMVolume.value = value;
            if (_textBGMVolumeValue != null) _textBGMVolumeValue.text = $"{value * 100f:F0}%";
        }
    }

    public float SFXVolume
    {
        get => _sliderSFXVolume != null ? _sliderSFXVolume.value : 0f;
        set
        {
            if (_sliderSFXVolume != null) _sliderSFXVolume.value = value;
            if (_textSFXVolumeValue != null) _textSFXVolumeValue.text = $"{value * 100f:F0}%";
        }
    }

    private void OnValidate()
    {
        if (_sliderBGMVolume != null && _textBGMVolumeValue != null)
        {
            _textBGMVolumeValue.text = $"{_sliderBGMVolume.value * 100f:F0}%";
        }
        if (_sliderSFXVolume != null && _textSFXVolumeValue != null)
        {
            _textSFXVolumeValue.text = $"{_sliderSFXVolume.value * 100f:F0}%";
        }
    }

    private void OnSliderValueChanged()
    {
        if (_sliderBGMVolume != null && _textBGMVolumeValue != null)
        {
            _textBGMVolumeValue.text = $"{_sliderBGMVolume.value * 100f:F0}%";
        }
        if (_sliderSFXVolume != null && _textSFXVolumeValue != null)
        {
            _textSFXVolumeValue.text = $"{_sliderSFXVolume.value * 100f:F0}%";
        }
    }
}
