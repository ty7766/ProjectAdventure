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

    private UI_SoundTabPresenter _presenter;

    private void Awake()
    {
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
