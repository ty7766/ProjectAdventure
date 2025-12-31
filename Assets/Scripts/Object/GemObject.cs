using UnityEngine;
using UnityEngine.SceneManagement;

public class GemObject : SpecialObject
{
    [Header("Components")]
    [SerializeField] private StageManager stageManager;

    protected override void ApplyEffect(GameObject player)
    {
        if(stageManager == null)
        {
            Debug.LogWarning("StageManager reference is missing in GemObject.");
            return;
        }
        stageManager.CollectGem();
    }
}
