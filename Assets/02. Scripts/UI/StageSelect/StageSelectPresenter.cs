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

        if (stageNumber <= GameSaveManager.Instance?.GetTotalStageNumber())
        {
            GameSaveManager.Instance?.LoadStage(stageNumber);
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
