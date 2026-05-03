using GameManager.Singleton;
using UnityEngine;

public class StageSelectPresenter
{
    private IStageSelectView _view;
    private int _pageIndex = 0;
    private const int STAGES_PER_PAGE = 8; // 페이지당 슬롯 수

    public StageSelectPresenter(IStageSelectView view)
    {
        _view = view;
    }

    public void UpdateStageView()
    {
        var saveManager = GameSaveManager.Instance;
        int totalStages = saveManager.GetTotalStageNumber();

        _view.HideAllSlots();
        UpdateViewSlots(saveManager, totalStages);
        ControlPageButtonVisibility(totalStages);
    }

    public void OnPageNextButtonClicked()
    {
        _pageIndex++;
        UpdateStageView();
    }

    public void OnPagePrevButtonClicked()
    {
        _pageIndex--;
        UpdateStageView();
    }

    public void OnStageButtonClicked(int slotIndex)
    {
        int stageNumber = (_pageIndex * STAGES_PER_PAGE) + slotIndex + 1;

        if (stageNumber > GameSaveManager.Instance.GetTotalStageNumber())
        {
            return;
        }

        if (GameSaveManager.Instance.CheckStageUnlockRequirement(stageNumber))
        {
            GameSaveManager.Instance.LoadStage(stageNumber);
        }
        else
        {
            ShowUnlockConditionPopup(stageNumber);
        }
    }

    private void ShowUnlockConditionPopup(int stageNumber)
    {
        var stageData = GameSaveManager.Instance.GetStageData(stageNumber);
        if (stageData == null) return;

        string message = GetUnlockConditionMessage(stageData);

        GlobalUICanvasView.Instance.Presenter.ShowPopup(
            "스테이지 미해금",
            message,
            ("확인", () => GlobalUICanvasView.Instance.Presenter.HidePopup())
        );
    }

    private string GetUnlockConditionMessage(StageData stageData)
    {
        switch (stageData.RequiredCondition)
        {
            case RequiredStageCondition.MustClearPreviousStage:
                return $"스테이지 {stageData.StageNumber - 1}을(를) 클리어해야 합니다.";

            case RequiredStageCondition.MustHaveTotalClearStars:
                int currentStars = GameSaveManager.Instance.GetTotalAcquiredStars();
                return $"총 {stageData.RequiredStarsValue}개의 별을 모아야 합니다.\n(현재: {currentStars}개)";

            default:
                return "이 스테이지는 아직 해금되지 않았습니다.";
        }
    }

    //--- Private Helper ---//
    private void UpdateViewSlots(GameSaveManager stageManager, int totalStages)
    {
        int startIndex = _pageIndex * STAGES_PER_PAGE;

        for (int i = 0; i < STAGES_PER_PAGE; i++)
        {
            int currentStageIndex = startIndex + i;

            if (currentStageIndex < totalStages)
            {
                int stageNumber = currentStageIndex + 1;
                var record = stageManager.GetSaveRecord(stageNumber);
                bool isUnlocked = stageManager.CheckStageUnlockRequirement(stageNumber);
                _view.UpdateStageSlotByIndex(i, (int)record?.StageNumber, (int)record?.AcquiredStars, isUnlocked);
            }
        }
    }

    private void ControlPageButtonVisibility(int totalStages)
    {
        if (_pageIndex > 0)
        {
            _view.ShowPagePrevButton();
        }
        else
        {
            _view.HidePagePrevButton();
        }

        int nextPageStartIndex = (_pageIndex + 1) * STAGES_PER_PAGE;
        if (nextPageStartIndex < totalStages)
        {
            _view.ShowPageNextButton();
        }
        else
        {
            _view.HidePageNextButton();
        }
    }
}
