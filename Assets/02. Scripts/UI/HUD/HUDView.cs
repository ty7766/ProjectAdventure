using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using DG.Tweening;

public class HUDView : MonoBehaviour
{
    //--- Settings ---//
    [Header("UI Components")]
    [SerializeField] private GameObject _hudPanel;

    [Header("Show/Hide Animation")]
    [SerializeField] private float _showHideDuration = 0.2f;

    [Header("Icon Feedback")]
    [SerializeField, Tooltip("하트를 잃었을 때 흔들림 세기 (캔버스 단위)")] private float _heartLostShake = 8f;
    [SerializeField, Tooltip("보석 획득 시 튀어오르는 크기")] private float _gemCollectPunch = 0.45f;

    [Header("Sprites")]
    [SerializeField]
    private Sprite _starFilledSprite;
    [SerializeField]
    private Sprite _starEmptySprite;
    [SerializeField]
    private Sprite _fullHeart;
    [SerializeField]
    private Sprite _emptyHeart;

    [Header("Health UI")]
    [SerializeField]
    private List<Image> _heartImages;


    [Header("Gem UI")]
    [SerializeField] 
    private List<Image> _gemImages;
    [SerializeField]
    private Color _fullGemColor;
    [SerializeField]
    private Color _emptyGemColor;

    [Header("Buff UI")]
    [SerializeField]
    private Transform _contentParent;
    [SerializeField]
    private BuffSlotView _buffItemPrefab;

    private BuffSlotPool _buffSlotPool;

    [Header("Timer")]
    [SerializeField]
    private TextMeshProUGUI _timerText;

    [Header("Pause Menu")]
    [SerializeField]
    private GameObject _pauseMenu;
    [SerializeField]
    private Button _resumeButton;
    [SerializeField]
    private Button _pauseButton;
    [SerializeField]
    private Button _returnToMainMenuButton;
    [SerializeField]
    private Button _quitGameButton;
    [SerializeField]
    private Button _retryButtonPauseMenu;

    [Header("Stage Start UI")]
    [SerializeField]
    private GameObject _stageStartPanel;
    [SerializeField]
    private TextMeshProUGUI _stageCountDownText;

    [Header("Stage Object UI")]
    [SerializeField]
    private List<StageObjectUIView> _stageObjectViews;

    [Header("StageClearUI")]
    [SerializeField]
    private GameObject _stageClearPanel;
    [SerializeField]
    private TextMeshProUGUI _stageClearTimeText;
    [SerializeField]
    private TextMeshProUGUI _stageClearNumberText;
    [SerializeField]
    private List<Image> _stageClearStarImages;
    [SerializeField]
    private Button _retryButton;
    [SerializeField]
    private Button _goToNextStageButton;

    [Header("StageFailUI")]
    [SerializeField]
    private GameObject _stageFailPanel;
    [SerializeField]
    private Button _retryButtonStageFail;
    [SerializeField]
    private Button _returnToMainMenuButtonStageFail;

    [Header("Platform Specific")]
    [SerializeField] private GameObject _touchControlsRoot; // TouchControls 오브젝트

    [Header("Map Control Buttons (Touch)")]
    [SerializeField] private Button _mapSelectLeftButton;
    [SerializeField] private Button _mapSelectRightButton;
    [SerializeField] private Button _mapSwapButton;
    [SerializeField] private Image _mapSwapCooldownImage; // Fill 1->0 연출용 이미지 (Filled)

    public void SetTouchControlsVisible(bool visible)
        => _touchControlsRoot?.SetActive(visible);

    //--- Button Events ---//
    public event Action OnResumeButtonClicked;
    public event Action OnPauseButtonClicked;
    public event Action OnReturnToMainMenuButtonClicked;
    public event Action OnQuitGameButtonClicked;
    public event Action OnRetryButtonClicked;
    public event Action OnGoToNextStageButtonClicked;
    public event Action OnMapSelectLeftClicked;
    public event Action OnMapSelectRightClicked;
    public event Action OnMapSwapClicked;

    //--- Fields ---//
    private bool _isPauseMenuActive = true;
    private Coroutine _fontSizeCoroutine;
    private Func<float> _mapChangeCooldownProvider;
    private StageClearSequence _stageClearSequence;
    private StageCountdownSequence _stageCountdownSequence;
    private int _lastHealth = -1;
    private int _lastCollectedGems = -1;

    //--- Properties ---//
    public bool IsPauseMenuActive
    {
        get { return _isPauseMenuActive; }
        set { _isPauseMenuActive = value; }
    }

    //--- Unity Methods ---//
    private void Awake()
    {
        AddButtonListeners();
        EnsureMenuButtonFeedback();
        _buffSlotPool = new BuffSlotPool(_contentParent, _buffItemPrefab);
        if (_stageClearPanel != null)
        {
            _stageClearPanel.TryGetComponent(out _stageClearSequence);
        }
        if (_stageStartPanel != null)
        {
            _stageStartPanel.TryGetComponent(out _stageCountdownSequence);
        }
    }

    private void Update()
    {
        HandleInput();
        UpdateMapSwapCooldownUI();
    }

    private void UpdateMapSwapCooldownUI()
    {
        if (_mapSwapCooldownImage == null || _mapChangeCooldownProvider == null)
        {
            return;
        }

        float remaining01 = _mapChangeCooldownProvider();
        if (remaining01 <= 0f)
        {
            if (_mapSwapCooldownImage.enabled)
            {
                _mapSwapCooldownImage.fillAmount = 0f;
                _mapSwapCooldownImage.enabled = false;
            }
            return;
        }

        _mapSwapCooldownImage.enabled = true;
        _mapSwapCooldownImage.fillAmount = remaining01;
    }

    /// <summary>
    /// 맵 교체 쿨다운 UI를 활성화합니다. 이후 터치 쿨다운 UI는 뷰가 매 프레임 갱신합니다.
    /// </summary>
    /// <param name="remainingProvider">쿨다운 잔여 비율(0~1, 1 = 방금 교체됨)를 반환하는 함수</param>
    public void StartMapSwapCooldownWatch(Func<float> remainingProvider)
    {
        _mapChangeCooldownProvider = remainingProvider;
    }

    /// <summary>
    /// 맵 교체 쿨다운 UI 표시를 중단합니다.
    /// </summary>
    public void StopMapSwapCooldownWatch()
    {
        _mapChangeCooldownProvider = null;
        if (_mapSwapCooldownImage != null)
        {
            _mapSwapCooldownImage.enabled = false;
        }
    }

    private void OnDestroy()
    {
        RemoveAllButtonListeners();
    }

    //--- Public Methods ---//
    /// <summary>
    /// UI의 체력 표시를 업데이트 합니다.
    /// </summary>
    /// <param name="currentHealth"></param>
    public void UpdateHealthUI(int currentHealth)
    {
        for (int i = 0; i < _heartImages.Count; i++)
        {
            UpdateHealthIcons(currentHealth, i);
        }

        if (_lastHealth >= 0 && currentHealth != _lastHealth)
        {
            AnimateHealthChange(_lastHealth, currentHealth);
        }
        _lastHealth = currentHealth;
    }

    /// <summary>
    /// 보석 수집 UI를 업데이트 합니다.
    /// </summary>
    /// <param name="collectedGems">수집한 보석 개수</param>
    /// <param name="requiredGems">스테이지 클리어에 필요한 보석 개수</param>
    public void UpdateGemUI(int collectedGems, int requiredGems)
    {
        int safeRequired = Mathf.Clamp(requiredGems, 0, _gemImages.Count);
        for (int i=0; i< safeRequired; i++)
        {
            UpdateGemIcons(collectedGems, i);
        }
        for (int i = safeRequired; i< _gemImages.Count; i++)
        {
            if (_gemImages[i].gameObject.activeSelf)
            {
                _gemImages[i].gameObject.SetActive(false);
            }
        }

        // 새로 채워진 보석만 튀어오름 (스테이지 재시작으로 줄어드는 경우는 연출 없음)
        if (_lastCollectedGems >= 0)
        {
            for (int i = Mathf.Max(_lastCollectedGems, 0); i < Mathf.Min(collectedGems, safeRequired); i++)
            {
                PlayIconPunch(_gemImages[i].rectTransform, _gemCollectPunch);
            }
        }
        _lastCollectedGems = collectedGems;
    }

    /// <summary>
    /// 버프 리스트 업데이트
    /// </summary>
    /// <param name="activeEffects"></param>
    public void RefreshBuffs(List<ActiveEffect> activeEffects)
    {
        _buffSlotPool.Refresh(activeEffects);
    }

    /// <summary>
    /// HUD 패널을 표출합니다.
    /// </summary>
    public void ShowHUD()
    {
        FadePanel(_hudPanel, true);
    }

    /// <summary>
    /// HUD 패널을 감춥니다.
    /// </summary>
    public void HideHUD()
    {
        FadePanel(_hudPanel, false);
    }

    /// <summary>
    /// 일시정지 메뉴를 표출합니다.
    /// </summary>
    public void ShowPauseMenu()
    {
        if (_pauseMenu == null || !IsPauseMenuActive)
        {
            return;
        }
        FadePanel(_pauseMenu, true);
    }

    /// <summary>
    /// 일시정지 메뉴를 감춥니다.
    /// </summary>
    public void HidePauseMenu()
    {
        if (_pauseMenu == null || !IsPauseMenuActive)
        {
            return;
        }
        FadePanel(_pauseMenu, false);
    }

    /// <summary>
    /// HUD 타이머 UI를 업데이트 합니다.
    /// </summary>
    /// <param name="timeInSeconds">초 단위 시간</param>
    public void UpdateTimerUI(float timeInSeconds)
    {
        if(_timerText == null)
        {
            CustomDebug.LogWarning("UpdateTimerUI: Timer Text component is not assigned.");
            return;
        }

        TimeSpan timeSpan = TimeSpan.FromSeconds(timeInSeconds);
        _timerText.text = string.Format("<mspace=0.7em>{0:D2}:{1:D2}.</mspace><mspace=0.5em><size=50%>{2:D3}</size></mspace>",
            timeSpan.Minutes,
            timeSpan.Seconds,
            timeSpan.Milliseconds);
    }

    /// <summary>
    /// 스테이지 도전과제 UI를 업데이트 합니다.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="text"></param>
    public void UpdateStageObjectUI(int index, string text, bool isCleared)
    {
        foreach(var item in _stageObjectViews)
        {
            item.UpdateStageObjectUI(index, text, isCleared, _starFilledSprite, _starEmptySprite);
        }
    }

    /// <summary>
    /// 스테이지 카운트다운 텍스트 UI를 업데이트 합니다.
    /// </summary>
    /// <param name="text"></param>
    public void UpdateStageCountDownContent(string text)
    {
        if(_stageCountDownText == null)
        {
            CustomDebug.LogWarning("UpdateStageCountDown: Stage Count Down Text component is not assigned.");
            return;
        }
        _stageCountDownText.text = text;
    }

    /// <summary>
    /// 글자 크기 애니메이션 적용
    /// </summary>
    /// <param name="targetFontSize"></param>
    /// <param name="duration"></param>
    public void ApplyStageCountDownAnimation(float targetFontSize, float duration)
    {
        if (_stageCountDownText == null)
        {
            CustomDebug.LogWarning("UpdateStageCountDownAnimation: Stage Count Down Text component is not assigned.");
            return;
        }

        if (_fontSizeCoroutine != null)
        {
            StopCoroutine(_fontSizeCoroutine);
        }

        _fontSizeCoroutine = StartCoroutine(AnimateFontSize(targetFontSize, duration));
    }

    /// <summary>
    /// 카운트다운 준비 문구를 표출합니다.
    /// </summary>
    public void PlayCountdownReady(string text)
    {
        if (_stageCountdownSequence != null)
        {
            _stageCountdownSequence.PlayReady(text);
            return;
        }
        UpdateStageCountDownContent(text);
        ApplyStageCountDownAnimation(80f, 1.0f);
    }

    /// <summary>
    /// 카운트다운 숫자 한 박자를 표출합니다.
    /// </summary>
    public void PlayCountdownTick(string text)
    {
        if (_stageCountdownSequence != null)
        {
            _stageCountdownSequence.PlayTick(text);
            return;
        }
        UpdateStageCountDownContent(text);
        ApplyStageCountDownAnimation(128f, 0.5f);
    }

    /// <summary>
    /// 카운트다운 시작 신호(GO)를 표출합니다.
    /// </summary>
    public void PlayCountdownGo(string text)
    {
        if (_stageCountdownSequence != null)
        {
            _stageCountdownSequence.PlayGo(text);
            return;
        }
        UpdateStageCountDownContent(text);
        ApplyStageCountDownAnimation(120f, 0.2f);
    }

    public void ShowStageStartPanel()
    {
        FadePanel(_stageStartPanel, true);
    }

    public void HideStageStartPanel()
    {
        FadePanel(_stageStartPanel, false);
    }

    public void ShowStageClearPanel()
    {
        FadePanel(_stageClearPanel, true);
    }

    public void HideStageClearPanel()
    {
        FadePanel(_stageClearPanel, false);
    }

    public void ShowStageFailPanel()
    {
        FadePanel(_stageFailPanel, true);
    }

    public void HideStageFailPanel()
    {
        FadePanel(_stageFailPanel, false);
    }

    public void UpdateStageClearStageNumberText(int number)
    {
        if(_stageClearNumberText != null)
        {
            _stageClearNumberText.text = $"스테이지 {number}";
        }
    }

    public void UpdateStageClearTimeRecordText(TimeSpan timeSpan)
    {
        if(_stageClearTimeText != null)
        {
            _stageClearTimeText.text = string.Format("<mspace=0.7em>{0:D2}:{1:D2}.</mspace><mspace=0.5em><size=50%>{2:D3}</size></mspace>",
            timeSpan.Minutes,
            timeSpan.Seconds,
            timeSpan.Milliseconds);
        }
    }

    public void UpdateStageClearStarSprite(int index, bool isCleared)
    {
        if(index < 0 || index >= _stageClearStarImages.Count)
        {
            CustomDebug.LogWarning("UpdateStageClearStarSprite: Index out of range.");
            return;
        }
        if (_stageClearSequence != null)
        {
            // 연출이 빈 별 위에 채워진 별을 박아 넣으므로 바탕은 항상 빈 별
            _stageClearStarImages[index].sprite = _starEmptySprite;
            _stageClearSequence.SetStarEarned(index, isCleared);
            return;
        }
        _stageClearStarImages[index].sprite = isCleared ? _starFilledSprite : _starEmptySprite;
    }

    //--- Private Methods ---//
    private void FadePanel(GameObject panel, bool show)
    {
        if (panel == null)
        {
            return;
        }

        // 화면 전용 시퀀스 연출이 있으면 단순 페이드 대신 사용
        if (panel.TryGetComponent(out IUIScreenTransition transition))
        {
            if (show)
            {
                transition.PlayEnter(null);
            }
            else
            {
                transition.PlayExit(null);
            }
            return;
        }

        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = panel.AddComponent<CanvasGroup>();
        }

        DOTween.Kill(canvasGroup);
        canvasGroup.interactable = show;
        canvasGroup.blocksRaycasts = show;

        if (show)
        {
            panel.SetActive(true);
            canvasGroup.alpha = 0f;
        }

        canvasGroup.DOFade(show ? 1f : 0f, _showHideDuration)
            .SetUpdate(true)
            .SetEase(show ? Ease.OutQuad : Ease.InQuad)
            .OnComplete(() =>
            {
                if (!show)
                {
                    panel.SetActive(false);
                }
            });
    }

    private IEnumerator AnimateFontSize(float targetSize, float time)
    {
        float startSize = _stageCountDownText.fontSize;
        float elapsed = 0f;

        while (elapsed < time)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / time;

            _stageCountDownText.fontSize = Mathf.Lerp(startSize, targetSize, progress);
            yield return null;
        }

        _stageCountDownText.fontSize = targetSize;
    }

    private void AddButtonListeners()
    {
        _resumeButton?.onClick.AddListener(() => { PlayClickSound(); OnResumeButtonClicked?.Invoke(); });
        _pauseButton?.onClick.AddListener(() => { PlayClickSound(); OnPauseButtonClicked?.Invoke(); });
        _returnToMainMenuButton?.onClick.AddListener(() => { PlayClickSound(); OnReturnToMainMenuButtonClicked?.Invoke(); });
        _quitGameButton?.onClick.AddListener(() => { PlayClickSound(); OnQuitGameButtonClicked?.Invoke(); });
        _retryButton?.onClick.AddListener(() => { PlayClickSound(); OnRetryButtonClicked?.Invoke(); });
        _goToNextStageButton?.onClick.AddListener(() => { PlayClickSound(); OnGoToNextStageButtonClicked?.Invoke(); });
        _retryButtonPauseMenu?.onClick.AddListener(() => { PlayClickSound(); OnRetryButtonClicked?.Invoke(); });
        _retryButtonStageFail?.onClick.AddListener(() => { PlayClickSound(); OnRetryButtonClicked?.Invoke(); });
        _returnToMainMenuButtonStageFail?.onClick.AddListener(() => { PlayClickSound(); OnReturnToMainMenuButtonClicked?.Invoke(); });
        _mapSelectLeftButton?.onClick.AddListener(() => { PlayClickSound(); OnMapSelectLeftClicked?.Invoke(); });
        _mapSelectRightButton?.onClick.AddListener(() => { PlayClickSound(); OnMapSelectRightClicked?.Invoke(); });
        _mapSwapButton?.onClick.AddListener(() => { PlayClickSound(); OnMapSwapClicked?.Invoke(); });
    }

    private void RemoveAllButtonListeners()
    {
        _resumeButton?.onClick.RemoveAllListeners();
        _pauseButton?.onClick.RemoveAllListeners();
        _returnToMainMenuButton?.onClick.RemoveAllListeners();
        _quitGameButton?.onClick.RemoveAllListeners();
        _retryButton?.onClick.RemoveAllListeners();
        _goToNextStageButton?.onClick.RemoveAllListeners();
        _retryButtonPauseMenu?.onClick.RemoveAllListeners();
        _retryButtonStageFail?.onClick.RemoveAllListeners();
        _returnToMainMenuButtonStageFail?.onClick.RemoveAllListeners();
        _mapSelectLeftButton?.onClick.RemoveAllListeners();
        _mapSelectRightButton?.onClick.RemoveAllListeners();
        _mapSwapButton?.onClick.RemoveAllListeners();
    }

    private void HandleInput()
    {   
        if (TutorialManager.IsActive || SceneTransitionManager.IsBusy)
        {
            return;
        }

        HandlePauseKey();
    }

    private void HandlePauseKey()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && IsPauseMenuActive)
        {
            if (GlobalUICanvasView.Instance != null && GlobalUICanvasView.Instance.PopupPresenter != null)
            {
                if (GlobalUICanvasView.Instance.IsPopupShown())
                {
                    GlobalUICanvasView.Instance.PopupPresenter.HidePopup();
                    return;
                }
            }
            else
            {
                Debug.LogWarning("[HUDView] GlobalUICanvasView.Instance or PopupPresenter is not initialized.");
            }
            TogglePauseMenu();
        }
    }

    private void TogglePauseMenu()
    {
        if (_pauseMenu == null || !IsPauseMenuActive)
        {
            return;
        }

        if (_pauseMenu.activeSelf)
        {
            OnResumeButtonClicked?.Invoke();
        }
        else
        {
            OnPauseButtonClicked?.Invoke();
        }
    }

    private void UpdateHealthIcons(int currentHealth, int i)
    {
        if (i < currentHealth)
        {
            _heartImages[i].sprite = _fullHeart;
        }
        else
        {
            _heartImages[i].sprite = _emptyHeart;
        }
    }

    private void UpdateGemIcons(int collectedGems, int i)
    {
        _gemImages[i].gameObject.SetActive(true);
        if (i < collectedGems)
        {
            _gemImages[i].color = _fullGemColor;
        }
        else
        {
            _gemImages[i].color = _emptyGemColor;
        }
    }

    /// <summary>
    /// 하트 증감 반응. 잃은 하트는 움찔하며 흔들리고, 회복한 하트는 작게 시작해 튕기며 차오른다.
    /// </summary>
    private void AnimateHealthChange(int previousHealth, int currentHealth)
    {
        int count = _heartImages.Count;
        if (currentHealth < previousHealth)
        {
            for (int i = Mathf.Clamp(currentHealth, 0, count); i < Mathf.Clamp(previousHealth, 0, count); i++)
            {
                RectTransform heart = _heartImages[i].rectTransform;
                heart.DOComplete();
                heart.DOPunchScale(Vector3.one * -0.35f, 0.35f, 8, 0.5f).SetUpdate(true);
                heart.DOShakeAnchorPos(0.35f, _heartLostShake, 20, 90f, false, true).SetUpdate(true);
            }
        }
        else
        {
            for (int i = Mathf.Clamp(previousHealth, 0, count); i < Mathf.Clamp(currentHealth, 0, count); i++)
            {
                RectTransform heart = _heartImages[i].rectTransform;
                heart.DOComplete();
                heart.localScale = Vector3.one * 0.4f;
                heart.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
            }
        }
    }

    private static void PlayIconPunch(RectTransform icon, float punch)
    {
        icon.DOComplete();
        icon.DOPunchScale(Vector3.one * punch, 0.35f, 6, 0.5f).SetUpdate(true);
    }

    private void EnsureMenuButtonFeedback()
    {
        Button[] menuButtons =
        {
            _resumeButton, _returnToMainMenuButton, _quitGameButton, _retryButtonPauseMenu,
            _retryButton, _goToNextStageButton, _retryButtonStageFail, _returnToMainMenuButtonStageFail,
        };
        foreach (var button in menuButtons)
        {
            if (button != null && button.GetComponent<UIButtonFeedback>() == null)
            {
                button.gameObject.AddComponent<UIButtonFeedback>();
            }
        }
    }

    private void PlayClickSound()
    {
        SoundManager.Instance.PlaySFX(SoundType.SFX_ButtonClick);
    }
}
