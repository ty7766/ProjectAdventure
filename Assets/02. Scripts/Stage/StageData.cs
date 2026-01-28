using UnityEngine;

public enum RequiredStageCondition { None, ClearPreviousStage, TotalClearStars }

[CreateAssetMenu(fileName = "StageData", menuName = "Game/Stage Data")]
public class StageData : ScriptableObject
{
    public int StageNumber;
    public RequiredStageCondition RequiredCondition;
    public int RequiredStarsValue;
    public string SceneName;
}