using UnityEngine;
using UnityEngine.SceneManagement;

public class GemObject : SpecialObject
{
    [Header("Components")]
    [SerializeField] private StageManager stageManager;

    protected override void ApplyEffect(GameObject player)
    {
        stageManager.CollectGem();
    }
}
