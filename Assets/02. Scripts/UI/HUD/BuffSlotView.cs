using Unity.VisualScripting;
using UnityEngine;

public class BuffSlotView : MonoBehaviour
{
    //--- Settings ---//
    [Header("References")]
    [SerializeField] private UnityEngine.UI.Image _iconImage;
    [SerializeField] private UnityEngine.UI.Image _durationFillImage;
    [SerializeField] private TMPro.TextMeshProUGUI _descriptionText;

    //--- Fields ---//
    private ActiveEffect _targetEffect;

    //--- Unity Methods ---//
    private void Update()
    {
        if (_targetEffect == null) return;
        UpdateDurationFill();
    }

    //--- Public Methods ---//
    /// <summary>
    /// 초기 데이터를 세팅합니다.
    /// </summary>
    /// <param name="effect"></param>
    public void Setup(ActiveEffect effect)
    {
        if(effect?.Data == null)
        {
            CustomDebug.LogWarning("BuffSlotView Setup called with null effect data.");
            return;
        }

        _targetEffect = effect;
        _iconImage.sprite = effect.Data.Icon;
        _descriptionText.text = effect.Data.Description;

        if (effect.IsPermanent)
        {
            _durationFillImage.fillAmount = 1f;
        }
        else
        {
            UpdateDurationFill();
        }
    }

    private void UpdateDurationFill()
    {
        float ratio = _targetEffect.RemainingDuration / _targetEffect.Data.Duration;
        _durationFillImage.fillAmount = ratio;
    }

}

