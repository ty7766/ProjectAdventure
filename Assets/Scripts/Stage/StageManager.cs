using UnityEngine;

public class StageManager : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private PlayerController playerController;

    [Header("Stage Settings")]
    [SerializeField] private int requiredGemsToClear = 1;
    private int collectedGems = 0;

    //--- Public Methods ---//
    public void CollectGem()
    {
        collectedGems++;
        if (collectedGems >= requiredGemsToClear)
        {
            ClearStage();
        }
    }

    //--- Private Helpers ---//
    private void ClearStage()
    {
        // Implement stage clear logic here (e.g., show UI, stop player movement)
        Debug.Log("Stage Cleared!");
        if(playerController != null)
        { 
            playerController.DisablePlayerControl();
        }
        else
        {
            Debug.LogWarning("PlayerController reference is missing in StageManager.");
        }
    }
}
