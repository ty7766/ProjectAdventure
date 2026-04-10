using UnityEngine;

public enum RequiredStageCondition { UnlockedByDefault, MustClearPreviousStage, MustHaveTotalClearStars }

[CreateAssetMenu(fileName = "StageData", menuName = "Game/Stage Data")]
public class StageData : ScriptableObject
{
    public int StageNumber;
    public RequiredStageCondition RequiredCondition;
    public int RequiredStarsValue;
    public GameObject StagePrefab;
}