using System;
using System.Collections.Generic;
using DG.Tweening;
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
    void PlayLockedSlotFeedback(int index);
}

public class StageSelectView : MonoBehaviour, IStageSelectView, IUIScreenTransition
{
    private StageSelectPresenter _presenter;

    [SerializeField]
    List<StageSlotView> _stageSlots;
    [SerializeField]
    Button _pageNextBtn;
    [SerializeField]
    Button _pagePrevBtn;

    [Header("Slot Sequence")]
    [SerializeField, Range(0.1f, 1f), Tooltip("슬롯 하나가 등장하는 시간")]
    private float _slotEnterDuration = 0.35f;
    [SerializeField, Range(0f, 0.3f), Tooltip("스테이지 1 -> 8 순서로 등장하는 간격")]
    private float _slotEnterInterval = 0.05f;
    [SerializeField, Range(0.1f, 1f), Tooltip("등장 시작 크기 (1 = 원래 크기)")]
    private float _slotStartScale = 0.6f;

    private Sequence _slotSequence;
    private CanvasGroup[] _slotCanvasGroups;

    //--- Unity Lifecycle --- //
    private void Awake()
    {
        _presenter = new StageSelectPresenter(this);
        CacheSlotCanvasGroups();
        EnsureButtonFeedback(_pageNextBtn);
        EnsureButtonFeedback(_pagePrevBtn);
        UIDefaultSelection.Ensure(gameObject, GetDefaultSelectable);
    }

    private void Start()
    {
        _presenter.UpdateStageView();
        AddButtonListeners();
    }

    private void OnDestroy()
    {
        _slotSequence?.Kill();
        DisposeButtonListeners();
    }

    //--- IUIScreenTransition ---//
    /// <summary>
    /// 스테이지 1 -> 8 순서로 슬롯이 작게 시작해 튀어나오며 등장 (OutBack + 페이드 인)
    /// </summary>
    public void PlayEnter(Action onComplete)
    {
        PlaySlotSequence(onComplete);
    }

    /// <summary>
    /// 역순으로 슬롯이 작아지며 사라짐
    /// </summary>
    public void PlayExit(Action onComplete)
    {
        _slotSequence?.Kill();
        _slotSequence = DOTween.Sequence().SetUpdate(true);

        int order = 0;
        for (int i = _stageSlots.Count - 1; i >= 0; i--)
        {
            if (!IsSlotVisible(i))
            {
                continue;
            }
            float delay = order++ * _slotEnterInterval * 0.5f;
            _slotSequence.Insert(delay, _stageSlots[i].transform.DOScale(_slotStartScale, _slotEnterDuration * 0.6f).SetEase(Ease.InBack));
            _slotSequence.Insert(delay, _slotCanvasGroups[i].DOFade(0f, _slotEnterDuration * 0.6f).SetEase(Ease.InQuad));
        }
        _slotSequence.OnComplete(() => onComplete?.Invoke());
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

    public void PlayLockedSlotFeedback(int index)
    {
        if (index < 0 || index >= _stageSlots.Count || _stageSlots[index] == null)
        {
            return;
        }
        _stageSlots[index].PlayLockedFeedback();
    }

    public void HideAllSlots()
    {
        foreach (var slot in _stageSlots)
        {
            slot.gameObject.SetActive(false);
        }
    }

    //--- Private Methods ---//
    private void CacheSlotCanvasGroups()
    {
        _slotCanvasGroups = new CanvasGroup[_stageSlots.Count];
        for (int i = 0; i < _stageSlots.Count; i++)
        {
            if (_stageSlots[i] == null)
            {
                continue;
            }
            if (!_stageSlots[i].TryGetComponent(out _slotCanvasGroups[i]))
            {
                _slotCanvasGroups[i] = _stageSlots[i].gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    private bool IsSlotVisible(int index)
    {
        return _stageSlots[index] != null && _stageSlots[index].gameObject.activeSelf;
    }

    private void PlaySlotSequence(Action onComplete)
    {
        _slotSequence?.Kill();

        // 전부 숨긴 상태로 초기화한 뒤 활성 슬롯만 순서대로 등장시킨다
        for (int i = 0; i < _stageSlots.Count; i++)
        {
            if (_stageSlots[i] == null)
            {
                continue;
            }
            _stageSlots[i].transform.localScale = Vector3.one * _slotStartScale;
            _slotCanvasGroups[i].alpha = 0f;
        }

        _slotSequence = DOTween.Sequence().SetUpdate(true);
        int order = 0;
        for (int i = 0; i < _stageSlots.Count; i++)
        {
            if (!IsSlotVisible(i))
            {
                continue;
            }
            float delay = order++ * _slotEnterInterval;
            _slotSequence.Insert(delay, _stageSlots[i].transform.DOScale(1f, _slotEnterDuration).SetEase(Ease.OutBack));
            _slotSequence.Insert(delay, _slotCanvasGroups[i].DOFade(1f, _slotEnterDuration * 0.6f).SetEase(Ease.OutQuad));
        }

        // 페이지 버튼은 마지막 슬롯과 함께 등장
        float pageButtonDelay = Mathf.Max(0, order - 1) * _slotEnterInterval;
        InsertPageButtonPop(_pagePrevBtn, pageButtonDelay);
        InsertPageButtonPop(_pageNextBtn, pageButtonDelay);

        _slotSequence.OnComplete(() => onComplete?.Invoke());
    }

    private void InsertPageButtonPop(Button button, float delay)
    {
        if (button == null || !button.gameObject.activeSelf)
        {
            return;
        }
        Transform target = button.transform;
        target.localScale = Vector3.zero;
        _slotSequence.Insert(delay, target.DOScale(1f, _slotEnterDuration).SetEase(Ease.OutBack));
    }

    private void OnPagePrevClicked()
    {
        _presenter.OnPagePrevButtonClicked();
        PlaySlotSequence(null);
    }

    private void OnPageNextClicked()
    {
        _presenter.OnPageNextButtonClicked();
        PlaySlotSequence(null);
    }

    private Selectable GetDefaultSelectable()
    {
        Selectable firstActive = null;
        foreach (var slot in _stageSlots)
        {
            if (slot == null || !slot.gameObject.activeInHierarchy)
            {
                continue;
            }
            if (slot.IsUnlocked)
            {
                return slot.StageButton;
            }
            firstActive ??= slot.StageButton;
        }
        return firstActive;
    }

    private static void EnsureButtonFeedback(Button button)
    {
        if (button != null && button.GetComponent<UIButtonFeedback>() == null)
        {
            button.gameObject.AddComponent<UIButtonFeedback>();
        }
    }

    private void AddButtonListeners()
    {
        int idx = 0;
        foreach (var slot in _stageSlots)
        {
            int captureIdx = idx;

            slot?.StageButton?.onClick.AddListener(() => _presenter.OnStageButtonClicked(captureIdx));
            idx++;
        }
        _pagePrevBtn?.onClick.AddListener(OnPagePrevClicked);
        _pageNextBtn?.onClick.AddListener(OnPageNextClicked);
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
