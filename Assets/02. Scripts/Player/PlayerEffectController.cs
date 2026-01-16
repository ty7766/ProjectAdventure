using System;
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

    //--- Events ---//
    public event Action<List<ActiveEffect>> OnBuffListChanged;

    //--- Unity Methods ---//
    private void Awake()
    {
        GetPlayerComponents();
        InitializeFields();
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
        var existing = _activeEffects.Find(e => e.Data == effectData);
        if (existing != null && !effectData.IsStackable)
        {
            existing.RefreshDuration();
        }
        else
        {
            _activeEffects.Add(new ActiveEffect(effectData));
            OnBuffListChanged?.Invoke(_activeEffects);
        }
        UpdateFinalStats();
    }

    /// <summary>
    /// 발판 위에 있는 동안 지속되는 상태 효과를 시작합니다. (영구 효과)
    /// </summary>
    /// <param name="effectData"></param>
    public void StartPermanentEffect(StatusEffect effectData)
    {
        var existing = _activeEffects.Find(e => e.Data == effectData);

        if (existing != null && !effectData.IsStackable)
        {

            existing.IsPermanent = true;
            existing.RefreshDuration();
        }
        else
        {
            var effect = new ActiveEffect(effectData, true);
            _activeEffects.Add(effect);
            OnBuffListChanged?.Invoke(_activeEffects);
        }

        UpdateFinalStats();
    }

    /// <summary>
    /// 발판 위에서 벗어났을 때 영구 상태 효과를 중지하고 N초 카운트다운을 시작합니다.
    /// </summary>
    /// <param name="effectData"></param>
    public void StopPermanentEffect(StatusEffect effectData)
    {
        var effect = _activeEffects.Find(e => e.Data == effectData && e.IsPermanent);
        if (effect != null)
        {
            effect.IsPermanent = false;
            effect.RefreshDuration(); // 이때부터 N초 카운트다운
        }
    }

    //--- Private Methods ---//
    private void InitializeFields()
    {
        if(_properties == null) return;
        _baseSpeed = _properties.Speed;
    }

    private void GetPlayerComponents()
    {
        _properties = GetComponent<PlayerProperties>();
        if(_properties == null)
        {
            CustomDebug.LogError($"PlayerProperties component is missing.", this);
            enabled = false;
        }
    }

    private void HandleEffectsDuration()
    {
        bool changed = false;
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            if (_activeEffects[i].IsPermanent) continue;

            _activeEffects[i].RemainingDuration -= Time.deltaTime;
            if (_activeEffects[i].RemainingDuration <= 0)
            {
                _activeEffects.RemoveAt(i);
                changed = true;
            }
        }

        if (changed)
        {
            UpdateFinalStats();
            OnBuffListChanged?.Invoke(_activeEffects);
        }
    }

    private void CalculateSpeed()
    {
        float totalMultiplier = _activeEffects
            .Where(e => e.Data.Type == EffectType.Speed)
            .Sum(e => e.Data.MultiplierValue);

        float totalAdditive = _activeEffects
            .Where(e => e.Data.Type == EffectType.Speed)
            .Sum(e => e.Data.AdditiveValue);

        // 최종 이동 속도 계산 수식 : (기본속도 * (1 + 증가율 효과 합)) + 고정증가량 합
        _properties.Speed = (_baseSpeed * (1.0f + totalMultiplier)) + totalAdditive;
    }

    private void UpdateFinalStats()
    {
        CalculateSpeed();

        //TODO : 다른 스탯들도 여기에 추가
    }
}
