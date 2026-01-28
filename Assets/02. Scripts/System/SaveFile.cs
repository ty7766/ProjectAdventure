using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public List<StageSaveRecord> StageRecords = new List<StageSaveRecord>();
}

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
public class StageEntryRuntime
{
    public StageData Data { get; private set; }
    public bool IsCleared { get; set; }

    private int _acquiredStars;
    public int AcquiredStars
    {
        get => _acquiredStars;
        set => _acquiredStars = Mathf.Clamp(value, 0, 3);
    }

    public StageEntryRuntime(StageData data, bool cleared, int stars)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException(nameof(data));
        }
        Data = data;
        IsCleared = cleared;
        AcquiredStars = stars;
    }
}