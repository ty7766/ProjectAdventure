using UnityEngine;

[CreateAssetMenu(fileName = "NewEffect", menuName = "Effects/Status Effect")]
public class StatusEffect : ScriptableObject
{
    public string effectName;
    public float value; // 증가/감소 수치 (예: 속도 +2.0)
    public float duration; // 지속 시간 (-1이면 무한)
    public bool isStackable; // 중첩 가능 여부
}

[System.Serializable]
public class ActiveEffect
{
    public StatusEffect data;
    public float remainingDuration;
    public bool isPermanent; // 발판 위에 있는 동안 true

    public ActiveEffect(StatusEffect data, bool isPermanent = false)
    {
        this.data = data;
        this.remainingDuration = data.duration;
        this.isPermanent = isPermanent;
    }

    public void RefreshDuration() => remainingDuration = data.duration;
}