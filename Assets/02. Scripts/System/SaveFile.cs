using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StageSaveRecord : ISerializationCallbackReceiver
{
    public int StageNumber;
    public bool IsCleared;
    public int AcquiredStars;

    public StageSaveRecord(int number, bool cleared, int stars)
    {
        StageNumber = number;
        IsCleared = cleared;
        AcquiredStars = Mathf.Clamp(stars, 0, 3);
    }

    public void OnBeforeSerialize()
    {

    }

    public void OnAfterDeserialize()
    {
        AcquiredStars = Mathf.Clamp(AcquiredStars, 0, 3);
    }
}

[System.Serializable]
public class GameSaveData
{
    public bool HasCompletedTutorial;
    public List<StageSaveRecord> StageRecords;
}