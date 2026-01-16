using UnityEngine;

public enum EffectType
{
    None,
    Speed
}

[CreateAssetMenu(fileName = "NewEffect", menuName = "Effects/Status Effect")]
public class StatusEffect : ScriptableObject
{
    [Header("이펙트 데이터")]
    /// <summary>
    /// 효과의 종류를 정의합니다. (예: 이동 속도, 공격력 등)
    /// </summary>
    public EffectType Type;

    [Tooltip("합연산 시 더해질 수치 (예: 2.0)")]
    /// <summary>
    /// 합연산 시 기본값에 더해질 절대적인 수치입니다.
    /// </summary>
    public float AdditiveValue;
    [Tooltip("배율연산 시 사용 (예: 0.3이면 30% 증가)")]
    /// <summary>
    /// 배율 연산 시 사용되는 계수입니다. (예: 0.3은 30% 증가를 의미)
    /// </summary>
    public float MultiplierValue;
    public float Duration;
    [Tooltip("완전히 동일한 효과 2개를 먹었을 때, 효과가 중첩될지 혹은 시간만 연장될지")]
    public bool IsStackable;

    [Header("UI 표시용")]
    public Sprite Icon;
    public string Description;
}

[System.Serializable]
public class ActiveEffect
{
    public StatusEffect Data;
    public float RemainingDuration;
    public bool IsPermanent; // 발판 위에 있는 동안 true

    /// <summary>
    /// 새로운 <see cref="ActiveEffect"/>의 인스턴스를 초기화 합니다.
    /// </summary>
    /// <param name="data">적용할 효과의 <see cref="StatusEffect"/> 데이터. <c>Null</c>이어서는 안됩니다.</param>
    /// <param name="isPermanent">발판의 효과인 경우 <see langword="true"/> 즉발성 아이템/스킬의 효과라면 <see langword="false"/></param>
    public ActiveEffect(StatusEffect data, bool isPermanent = false)
    {
        if(data == null)
        {
            Debug.LogError("ActiveEffect initialized with null StatusEffect data.");
            return;
        }
        Data = data;
        RemainingDuration = data.Duration;
        IsPermanent = isPermanent;
    }

    public void RefreshDuration() => RemainingDuration = Data.Duration;
}