using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDView : MonoBehaviour
{
    //--- Settings ---//
    [Header("UI Components")]
    [SerializeField] private GameObject _hudPanel;

    [Header("Health UI")]
    [SerializeField]
    private List<Image> _heartImages;
    [SerializeField]
    private Sprite _fullHeart; 
    [SerializeField]
    private Sprite _emptyHeart;

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

    [Header("Timer")]
    [SerializeField]
    private TextMeshProUGUI _timerText;

    [Header("Pause Menu")]
    [SerializeField]
    private KeyCode _pauseKey = KeyCode.Escape;
    [SerializeField]
    private GameObject _pauseMenu;
    [SerializeField]
    private UnityEngine.UI.Button _resumeButton;
    [SerializeField]
    private UnityEngine.UI.Button _pauseButton;
    [SerializeField]
    private UnityEngine.UI.Button _returnToMainMenuButton;
    [SerializeField]
    private UnityEngine.UI.Button _quitGameButton;

    [Header("Stage Start UI")]
    [SerializeField]
    private GameObject _stageStartPanel;
    [SerializeField]
    private TextMeshProUGUI _stageCountDownText;

    [Header("Stage Object UI")]
    [SerializeField]
    private GameObject _stageObjectPanel;
    [SerializeField]
    private List<TextMeshProUGUI> _stageObjectTexts;
    [SerializeField]
    private List<Image> _stageObjectStarImages;
    [SerializeField]
    private Sprite _starFilledSprite;
    [SerializeField]
    private Sprite _starEmptySprite;


    //--- Events ---//
    public event Action OnResumeButtonClicked;
    public event Action OnPauseButtonClicked;
    public event Action OnReturnToMainMenuButtonClicked;
    public event Action OnQuitGameButtonClicked;

    //--- Fields ---//
    private bool _isPauseMenuActive = true;
    private Coroutine _fontSizeCoroutine;

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
    }

    private void Update()
    {
        HandlePauseKeyInput();
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
            _gemImages[i].gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 버프 리스트 업데이트
    /// </summary>
    /// <param name="activeEffects"></param>
    public void RefreshBuffs(List<ActiveEffect> activeEffects)
    {
        // TODO : 성능면에서 조금 아쉬울것 같은데
        // 1. 기존 아이콘 싹 청소
        foreach (Transform child in _contentParent)
        {
            Destroy(child.gameObject);
        }

        // 2. 새 리스트대로 생성
        foreach (var effect in activeEffects)
        {
            BuffSlotView newItem = Instantiate(_buffItemPrefab, _contentParent);
            newItem.Setup(effect);
        }
    }

    /// <summary>
    /// HUD 패널을 표출합니다.
    /// </summary>
    public void ShowHUD()
    {
        if( _hudPanel != null)
        {
            _hudPanel.SetActive(true);
        }
    }

    /// <summary>
    /// HUD 패널을 감춥니다.
    /// </summary>
    public void HideHUD()
    {
        if (_hudPanel != null)
        {
            _hudPanel.SetActive(false);
        }
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
        _pauseMenu.SetActive(true);
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
        _pauseMenu.SetActive(false);
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
        if(index < 0 || index >= _stageObjectTexts.Count)
        {
            CustomDebug.LogWarning("UpdateStageObjectUI: Index out of range.");
            return;
        }
        if(index < 0 || index >= _stageObjectStarImages.Count)
        {
            CustomDebug.LogWarning("UpdateStageObjectUI: Index out of range for star images.");
            return;
        }

        _stageObjectTexts[index].text = text;
        _stageObjectStarImages[index].sprite = isCleared ? _starFilledSprite : _starEmptySprite;
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

    public void ShowStageStartPanel()
    {
        if(_stageStartPanel != null)
        {
            _stageStartPanel.SetActive(true);
        }
    }

    public void HideStageStartPanel()
    {
        if (_stageStartPanel != null)
        {
            _stageStartPanel.SetActive(false);
        }
    }

    public void ShowStageObjectView()
    {
        if(_stageObjectPanel != null)
        {
            _stageObjectPanel.SetActive(true);
        }
    }

    public void HideStageObjectView()
    {
        if (_stageObjectPanel != null)
        {
            _stageObjectPanel.SetActive(false);
        }
    }

    //--- Private Methods ---//
    private IEnumerator AnimateFontSize(float targetSize, float time)
    {
        float startSize = _stageCountDownText.fontSize;
        float elapsed = 0f;

        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / time;

            _stageCountDownText.fontSize = Mathf.Lerp(startSize, targetSize, progress);
            yield return null;
        }

        _stageCountDownText.fontSize = targetSize;
    }

    private void AddButtonListeners()
    {
        _resumeButton?.onClick.AddListener(() => OnResumeButtonClicked?.Invoke());
        _pauseButton?.onClick.AddListener(() => OnPauseButtonClicked?.Invoke());
        _returnToMainMenuButton?.onClick.AddListener(() => OnReturnToMainMenuButtonClicked?.Invoke());
        _quitGameButton?.onClick.AddListener(() => OnQuitGameButtonClicked?.Invoke());
    }

    private void HandlePauseKeyInput()
    {
        if (Input.GetKeyDown(_pauseKey) && IsPauseMenuActive)
        {
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
}
