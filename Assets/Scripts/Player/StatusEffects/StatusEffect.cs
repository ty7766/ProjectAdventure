using UnityEngine;

[CreateAssetMenu(fileName = "NewEffect", menuName = "Effects/Status Effect")]
public class StatusEffect : ScriptableObject
{
    public string EffectName;
    public float Value; // 증가/감소 수치 (예: 속도 +2.0)
    public float Duration; // 지속 시간 (-1이면 무한)
    public bool IsStackable; // 중첩 가능 여부
}

[System.Serializable]
public class ActiveEffect
{
    public StatusEffect Data;
    public float RemainingDuration;
    public bool IsPermanent; // 발판 위에 있는 동안 true

    public ActiveEffect(StatusEffect data, bool isPermanent = false)
    {
        Data = data;
        RemainingDuration = data.Duration;
        IsPermanent = isPermanent;
    }

    public void RefreshDuration() => RemainingDuration = Data.Duration;
}