using UnityEngine;

public class StageManager : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private PlayerController playerController;

    [Header("Stage Settings")]
    [SerializeField] private int requiredGemsToClear = 1;

    private int collectedGems = 0;

    //--- Public Methods ---//
    /// <summary>
    /// 스테이지 클리어 조건인 보석 수집을 처리하고 클리어 목표를 달성한 경우 스테이지를 클리어합니다.
    /// </summary>
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
        CustomDebug.Log("Stage Cleared!");
        DisablePlayerControl();
    }

    private void DisablePlayerControl()
    {
        if (playerController != null)
        {
            playerController.DisablePlayerControl();
        }
        else
        {
            CustomDebug.LogWarning("PlayerController reference is missing in StageManager.");
        }
    }
}
