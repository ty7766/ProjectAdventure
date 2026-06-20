using System;
using GameManager.Singleton;
using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialManager : MonoBehaviour
{
    //--- Serialized Fields ---//
    [Header("튜토리얼 설정")]
    [SerializeField]
    private TutorialStepConfig[] _steps;
    [SerializeField]
    private TutorialView _tutorialView;
    [SerializeField]
    private TutorialCameraController _cameraController;

    private int _currentStepIndex;
    private Action _onCompleteCallback;

    public static bool IsActive { get; private set; }

    private void OnDisable()
    {
        IsActive = false;
    }

    private void Update()
    {
        if (!IsActive)
        {
            return;
        }
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            AdvanceStep();
        }
    }

    //--- Public Methods ---//
    /// <summary>
    /// 현재 씬에서 튜토리얼을 표시해야 하는지 확인합니다.
    /// </summary>
    public bool ShouldShowTutorial()
    {
        //디버깅용
        //int stageNumber = GameSaveManager.Instance.CurrentStageNumber;
        //bool isTutorialStage = stageNumber == 1 || stageNumber == 0;
        //return isTutorialStage && !GameSaveManager.Instance.HasCompletedTutorial();

        //실제용
        return GameSaveManager.Instance.CurrentStageNumber == 1
        && !GameSaveManager.Instance.HasCompletedTutorial();
    }

    /// <summary>
    /// 튜토리얼을 시작합니다. 완료 시 onComplete 콜백이 호출됩니다.
    /// </summary>
    public void StartTutorial(Action onComplete)
    {
        if (_steps == null || _steps.Length == 0)
        {
            CustomDebug.LogWarning("TutorialManager: 등록된 스텝이 없습니다.");
            onComplete?.Invoke();
            return;
        }

        _onCompleteCallback = onComplete;
        _currentStepIndex = 0;
        IsActive = true;

        _tutorialView.gameObject.SetActive(true);
        ShowCurrentStep();
    }

    //--- Private Methods ---//
    private void ShowCurrentStep()
    {
        TutorialStepConfig config = _steps[_currentStepIndex];

        if (config.MoveCameraToTarget)
        {
            Transform anchor = TutorialAnchorRegistry.GetTransform(config.WorldAnchorID);
            if (anchor != null)
            {
                _tutorialView.HideCurrentStep();
                _cameraController.MoveToTarget(anchor, () => _tutorialView.ShowStep(config));
                return;
            }
            CustomDebug.LogWarning($"TutorialManager: '{config.WorldAnchorID}'를 찾지 못하여 카메라 이동 없이 진행합니다");
        }
        _tutorialView.ShowStep(config);
    }

    private void AdvanceStep()
    {
        _currentStepIndex++;

        if (_currentStepIndex >= _steps.Length)
        {
            CompleteTutorial();
            return;
        }

        ShowCurrentStep();
    }

    private void CompleteTutorial()
    {
        IsActive = false;
        _cameraController.RestoreFollow();
        _tutorialView.Hide();
        _tutorialView.gameObject.SetActive(false);
        GameSaveManager.Instance.SetTutorialCompleted();
        _onCompleteCallback?.Invoke();
        _onCompleteCallback = null;
    }
}
