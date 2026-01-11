using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class GemObject : SpecialObject
{
    [Header("Components")]
    [SerializeField] private StageManager stageManager;

    protected override void ApplyEffect(GameObject player)
    {
        CollectGem();
    }

    private void CollectGem()
    {
        if (stageManager != null)
        {
            stageManager.CollectGem();
        }
        else
        {
            Debug.LogWarning("StageManager reference is missing in GemObject.");
        }
    }
}
