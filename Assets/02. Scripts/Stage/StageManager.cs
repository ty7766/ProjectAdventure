using System;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    //--- Components & Settings ---//
    [Header("Components")]
    [SerializeField] private PlayerController playerController;

    [Header("Stage Settings")]
    [SerializeField] private int requiredGemsToClear = 1;

    //--- Fields ---//
    private int _collectedGems = 0;

    //--- Events ---//
    public event Action<int, int> OnGemCountChanged;

    //--- Unity Methods ---//
    private void Start()
    {
        OnGemCountChanged?.Invoke(_collectedGems, requiredGemsToClear);
    }

    //--- Public Methods ---//
    /// <summary>
    /// 스테이지 클리어 조건인 보석 수집을 처리하고 클리어 목표를 달성한 경우 스테이지를 클리어합니다.
    /// </summary>
    public void CollectGem()
    {
        _collectedGems++;
        OnGemCountChanged?.Invoke(_collectedGems, requiredGemsToClear);
        if (_collectedGems >= requiredGemsToClear)
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
