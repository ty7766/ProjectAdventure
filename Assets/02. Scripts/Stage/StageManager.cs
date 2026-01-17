using System;
using System.Collections;
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
    private Coroutine timeScaleCoroutine;

    //--- Events ---//
    public event Action<int, int> OnGemCountChanged;
    public event Action OnStageCleared;

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

    /// <summary>
    /// 게임을 부드럽게 일시정지합니다.
    /// </summary>
    /// <param name="duration"></param>
    public void PauseGameSmoothly(float duration = 0.5f) => SmoothTimeScale(0f, duration);

    /// <summary>
    /// 게임을 부드럽게 재개합니다.
    /// </summary>
    /// <param name="duration"></param>
    public void ResumeGameSmoothly(float duration = 0.5f) => SmoothTimeScale(1f, duration);

    /// <summary>
    /// 타임스케일을 부드럽게 변경합니다.
    /// </summary>
    /// <param name="targetScale">목표 타임스케일 (0f = 정지, 1f = 정상)</param>
    /// <param name="duration">변화에 걸리는 시간(초)</param>
    public void SmoothTimeScale(float targetScale, float duration)
    {
        if (timeScaleCoroutine != null)
            StopCoroutine(timeScaleCoroutine);

        timeScaleCoroutine = StartCoroutine(ChangeTimeScale(targetScale, duration));
    }




    //--- Private Helpers ---//
    private IEnumerator ChangeTimeScale(float targetScale, float duration)
    {
        float startScale = Time.timeScale;
        float initialFixedDeltaTime = 0.02f; // 유니티 기본값
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            float currentScale = Mathf.Lerp(startScale, targetScale, t);

            Time.timeScale = currentScale;
            Time.fixedDeltaTime = initialFixedDeltaTime * currentScale;

            yield return null;
        }

        Time.timeScale = targetScale;
        Time.fixedDeltaTime = initialFixedDeltaTime * targetScale;

        if (Time.timeScale <= 0)
        {
            Time.fixedDeltaTime = initialFixedDeltaTime;
        }
    }

    private void ClearStage()
    {
        CustomDebug.Log("Stage Cleared!");
        DisablePlayerControl();
        OnStageCleared?.Invoke();
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
