using UnityEngine;
using System;
using System.Collections.Generic;
using GameManager.Singleton;


public class TutorialManager : MonoBehaviour
{
    [Header("튜토리얼 설정")]
    [SerializeField]
    private List<TutorialStepConfig> _steps;
    [SerializeField]
    private TutorialView _tutorialView;

    private int _currentStepIndex;
    private Action _onCompleteCallback;
    private bool _isActive;

    private void Update()
    {
        if(!_isActive)
        {
            return;
        }
        if(Input.GetMouseButtonDown(0))
        {
            AdvanceStep();
        }
    }

    /// <summary>
    /// 현재 씬에서 튜토리얼을 표시해야 하는지 확인합니다.
    /// </summary>
    public bool ShouldShowTutorial()
    {
        //디버깅용
        int stageNumber = GameSaveManager.Instance.CurrentStageNumber;
        bool isTutorialStage = stageNumber == 1 || stageNumber == 0;
        return isTutorialStage && !GameSaveManager.Instance.HasCompletedTutorial();

        //실제용
        //return GameSaveManager.Instance.CurrentStageNumber == 1
        //    && !GameSaveManager.Instance.HasCompletedTutorial();
    }

    /// <summary>
    /// 튜토리얼을 시작합니다. 완료 시 onComplete 콜백이 호출됩니다.
    /// </summary>
    public void StartTutorial(Action onComplete)
    {
        if (_steps == null || _steps.Count == 0)
        {
            CustomDebug.LogWarning("TutorialManager: 등록된 스텝이 없습니다.");
            onComplete?.Invoke();
            return;
        }

        _onCompleteCallback = onComplete;
        _currentStepIndex = 0;
        _isActive = true;

        _tutorialView.gameObject.SetActive(true);
        ShowCurrentStep();
    }

    private void ShowCurrentStep()
    {
        _tutorialView.ShowStep(_steps[_currentStepIndex]);
    }

    private void AdvanceStep()
    {
        _currentStepIndex++;

        if (_currentStepIndex >= _steps.Count)
        {
            CompleteTutorial();
            return;
        }

        ShowCurrentStep();
    }

    private void CompleteTutorial()
    {
        _isActive = false;
        _tutorialView.Hide();
        _tutorialView.gameObject.SetActive(false);
        GameSaveManager.Instance.SetTutorialCompleted();
        _onCompleteCallback?.Invoke();
        _onCompleteCallback = null;
    }

}
