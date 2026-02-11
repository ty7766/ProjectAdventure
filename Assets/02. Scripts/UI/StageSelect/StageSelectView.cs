using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface IStageSelectView
{
    void UpdateStageSlotByIndex(int index, int stageNumber, int starCount, bool isUnlocked);
    void HideAllSlots();
    void ShowPageNextButton();
    void ShowPagePrevButton();
    void HidePageNextButton();
    void HidePagePrevButton();
}

public class StageSelectView : MonoBehaviour, IStageSelectView
{
    private StageSelectPresenter _presenter;

    [SerializeField]
    List<StageSlotView> _stageSlots;
    [SerializeField]
    Button _pageNextBtn;
    [SerializeField]
    Button _pagePrevBtn;

    //--- Unity Lifecycle --- //
    private void Awake()
    {
        _presenter = new StageSelectPresenter(this);
    }

    private void Start()
    {
        _presenter.UpdateStageView();
        AddButtonListeners();
    }

    private void OnDestroy()
    {
        DisposeButtonListeners();
    }

    //--- Public Methods ---//
    public void UpdateStageSlotByIndex(int index, int stageNumber, int starCount, bool isUnlocked)
    {
        if (index < 0 || index >= _stageSlots.Count)
        {
            return;
        }
        _stageSlots[index].gameObject.SetActive(true);
        _stageSlots[index].UpdateStageNumber(stageNumber);
        _stageSlots[index].UpdateStarFilled(starCount);
        _stageSlots[index].UpdateLockedBlokcer(isUnlocked);
    }

    public void ShowPageNextButton()
    {
        _pageNextBtn?.gameObject.SetActive(true);
    }

    public void ShowPagePrevButton()
    {
        _pagePrevBtn?.gameObject.SetActive(true);
    }

    public void HidePageNextButton()
    {
        _pageNextBtn?.gameObject.SetActive(false);
    }

    public void HidePagePrevButton()
    {
        _pagePrevBtn?.gameObject.SetActive(false);
    }

    public void HideAllSlots()
    {
        foreach (var slot in _stageSlots)
        {
            slot.gameObject.SetActive(false);
        }
    }

    //--- Private Methods ---//
    private void AddButtonListeners()
    {
        int idx = 0;
        foreach (var slot in _stageSlots)
        {
            int captureIdx = idx;

            slot?.StageButton?.onClick.AddListener(() => _presenter.OnStageButtonClicked(captureIdx));
            idx++;
        }
        _pagePrevBtn?.onClick.AddListener(_presenter.OnPagePrevButtonClicked);
        _pageNextBtn?.onClick.AddListener(_presenter.OnPageNextButtonClicked);
    }

    private void DisposeButtonListeners()
    {
        foreach (var slot in _stageSlots)
        {
            slot?.StageButton?.onClick.RemoveAllListeners();
        }
        _pagePrevBtn?.onClick.RemoveAllListeners();
        _pageNextBtn?.onClick.RemoveAllListeners();
    }
}
