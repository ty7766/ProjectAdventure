using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(PlayerProperties))]
public class PlayerEffectController : MonoBehaviour
{
    //--- Components ---//
    private PlayerProperties _properties;

    //--- Fields ---//
    private List<ActiveEffect> _activeEffects = new List<ActiveEffect>();
    private float _baseSpeed;

    //--- Unity Methods ---//
    private void Awake()
    {
        TryGetComponent<PlayerProperties>(out _properties);
        _baseSpeed = _properties.Speed;
    }
    private void Update()
    {
        HandleEffectsDuration();
    }

    //--- Public Methods ---// 
    /// <summary>
    /// 일반 아이템이나 스킬 등에 의해 상태 효과를 적용합니다. (즉시 발동, N 초후 사라지는 효과)
    /// </summary>
    /// <param name="effectData"></param>
    public void ApplyEffect(StatusEffect effectData)
    {
        var existing = _activeEffects.Find(e => e.data == effectData);
        if (existing != null && !effectData.isStackable)
        {
            existing.RefreshDuration();
        }
        else
        {
            _activeEffects.Add(new ActiveEffect(effectData));
        }
        UpdateFinalStats();
    }
    /// <summary>
    /// 발판 위에 있는 동안 지속되는 상태 효과를 시작합니다. (영구 효과)
    /// </summary>
    /// <param name="effectData"></param>
    public void StartPermanentEffect(StatusEffect effectData)
    {
        var effect = new ActiveEffect(effectData, true);
        _activeEffects.Add(effect);
        UpdateFinalStats();
    }
    /// <summary>
    /// 발판 위에서 벗어났을 때 영구 상태 효과를 중지하고 N초 카운트다운을 시작합니다.
    /// </summary>
    /// <param name="effectData"></param>
    public void StopPermanentEffect(StatusEffect effectData)
    {
        var effect = _activeEffects.Find(e => e.data == effectData && e.isPermanent);
        if (effect != null)
        {
            effect.isPermanent = false;
            effect.RefreshDuration(); // 이때부터 N초 카운트다운
        }
    }

    //--- Private Methods ---//
    private void HandleEffectsDuration()
    {
        bool changed = false;
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            if (_activeEffects[i].isPermanent) continue;

            _activeEffects[i].remainingDuration -= Time.deltaTime;
            if (_activeEffects[i].remainingDuration <= 0)
            {
                _activeEffects.RemoveAt(i);
                changed = true;
            }
        }

        if (changed) UpdateFinalStats();
    }
    private void UpdateFinalStats()
    {
        // 최종 이동속도 값 계산, 합연산 적용
        float totalSpeedMod = _activeEffects
            .Where(e => e.data.effectName == "SpeedBoost")
            .Sum(e => e.data.value);
        _properties.Speed = _baseSpeed + totalSpeedMod;

        //TODO : 다른 스탯들도 여기서 처리
    }
}
