using UnityEngine;

[CreateAssetMenu(fileName = "NewEffect", menuName = "Effects/Status Effect")]
public class StatusEffect : ScriptableObject
{
    public string EffectName;

    [Header("합연산 시 더해질 수치 (예: 2.0)")]
    public float AdditiveValue;
    [Header("배율연산 시 사용 (예: 0.3이면 30% 증가)")]
    public float MultiplierValue;

    [Header("효과 지속 시간 (초)")]
    public float Duration;
    [Header("중첩 가능 여부")]
    [Tooltip("완전히 동일한 효과 2개를 먹었을 때, 효과가 중첩될지 혹은 시간만 연장될지")]
    public bool IsStackable;
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
        Data = data;
        RemainingDuration = data.Duration;
        IsPermanent = isPermanent;
    }

    public void RefreshDuration() => RemainingDuration = Data.Duration;
}