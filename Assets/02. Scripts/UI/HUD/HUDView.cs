using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class HUDView : MonoBehaviour
{
    //--- Settings ---//
    [Header("UI Components")]
    [Header("Health UI")]
    [SerializeField] private List<Image> _heartImages; // 하트 아이콘 리스트
    [SerializeField] private Sprite _fullHeart; // 꽉 찬 하트 이미지
    [SerializeField] private Sprite _emptyHeart; // 빈 하트 이미지

    [Header("Gem UI")]
    [SerializeField] private List<Image> _gemImages; // 보석 아이콘 리스트
    [SerializeField] private Color _fullGemColor; // 수집한 보석 색상
    [SerializeField] private Color _emptyGemColor; // 빈 보석 이미지 색상

    [Header("Buff UI")]
    [SerializeField] private Transform _contentParent;
    [SerializeField] private BuffSlotView _buffItemPrefab;

    [Header("Pause Menu")]
    [SerializeField] private KeyCode _pauseKey = KeyCode.Escape;
    [SerializeField] private GameObject _pauseMenu;
    [SerializeField] private UnityEngine.UI.Button _resumeButton;
    [SerializeField] private UnityEngine.UI.Button _pauseButton;
    [SerializeField] private UnityEngine.UI.Button _returnToMainMenuButton;
    [SerializeField] private UnityEngine.UI.Button _quitGameButton;

    //--- Events ---//
    public event Action OnResumeButtonClicked;
    public event Action OnPauseButtonClicked;
    public event Action OnReturnToMainMenuButtonClicked;
    public event Action OnQuitGameButtonClicked;

    //--- Fields ---//
    private bool _isPauseMenuActive = true;

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

    //--- Private Methods ---//
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
