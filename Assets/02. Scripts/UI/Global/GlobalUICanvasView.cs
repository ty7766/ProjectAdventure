using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GlobalUICanvasView : Singleton<GlobalUICanvasView>
{
    #region Common Elements
    [Header("Background Blocker")]
    [SerializeField] private CanvasGroup _bgBlockerCanvasGroup;
    [SerializeField, Range(0.1f, 1f)] private float _fadeDuration = 0.3f;

    private Tween _bgBlockerTween;
    #endregion

    #region Popup Components
    [Header("Popup Components")]
    [SerializeField] private CanvasGroup _popupCanvasGroup;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private Transform _buttonParent;
    [SerializeField] private Button _buttonPrefab;

    [Header("Popup Animation")]
    [SerializeField, Range(0.5f, 1f)] private float _popupStartScale = 0.85f;
    [SerializeField, Range(0.05f, 1f)] private float _popupShowDuration = 0.28f;
    [SerializeField, Range(0.05f, 1f)] private float _popupHideDuration = 0.18f;

    private Sequence _popupSequence;
    private bool _isPopupShown;
    private GameObject _selectionBeforePopup;
    #endregion

    #region FPS Monitor Components
    [Header("FPS Monitor Components")]
    [SerializeField] private CanvasGroup _fpsMonitorCanvasGroup;
    [SerializeField] private TextMeshProUGUI _currentFpsText;
    [SerializeField] private TextMeshProUGUI _avgFpsText;
    [SerializeField] private TextMeshProUGUI _minFpsText;
    [SerializeField] private TextMeshProUGUI _maxFpsText;
    [SerializeField] private TextMeshProUGUI _lowFpsText;
    [SerializeField] private TextMeshProUGUI _frameTimeText;
    #endregion

    private GlobalUICanvasPopupPresenter _popupPresenter;
    private GlobalUICanvasFPSMonitorPresenter _fpsMonitorPresenter;

    public GlobalUICanvasPopupPresenter PopupPresenter => _popupPresenter;
    public GlobalUICanvasFPSMonitorPresenter FPSMonitorPresenter => _fpsMonitorPresenter;

    protected override void Awake()
    {
        base.Awake();
        _popupPresenter = new GlobalUICanvasPopupPresenter(this);
        _fpsMonitorPresenter = new GlobalUICanvasFPSMonitorPresenter(this);
        Initialize();
    }

    private void Initialize()
    {
        // [1] gameObject는 항상 활성 상태로 유지
        // 다른 구현에서 gameObject를 비활성화하면 안됨!
        gameObject.SetActive(true);

        // [2] 모든 Canvas Group 초기화 (alpha로만 표출 제어)
        _bgBlockerCanvasGroup.alpha = 0f;
        _bgBlockerCanvasGroup.blocksRaycasts = false;

        // [3] 팝업 자동 감추기 (에디터에서 실수로 켜둔 것 방지)
        _popupCanvasGroup.alpha = 0f;
        _popupCanvasGroup.blocksRaycasts = false;

        // [4] FPS 모니터 초기 상태 설정
        if (_fpsMonitorCanvasGroup != null)
        {
            _fpsMonitorCanvasGroup.alpha = 0f;
            _fpsMonitorCanvasGroup.blocksRaycasts = false;
        }

        // [5] 저장된 FPS 모니터 설정에 따라 활성화/비활성화
        bool fpsMonitorEnabled = PlayerPrefs.GetInt("FpsMonitorEnabled", 0) == 1;
        if (fpsMonitorEnabled)
        {
            _fpsMonitorPresenter.ShowMonitor();
        }
    }

    private void Update()
    {
        _fpsMonitorPresenter?.UpdatePerformanceMetrics();
    }

    #region Common Methods
    public void ShowBgBlocker()
    {
        _bgBlockerTween?.Kill();
        _bgBlockerCanvasGroup.blocksRaycasts = true;
        _bgBlockerTween = _bgBlockerCanvasGroup.DOFade(1f, _fadeDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    public void HideBgBlocker()
    {
        _bgBlockerTween?.Kill();
        _bgBlockerTween = _bgBlockerCanvasGroup.DOFade(0f, _fadeDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .OnComplete(() => _bgBlockerCanvasGroup.blocksRaycasts = false);
    }
    #endregion

    #region Popup Methods
    public void SetPopupContent(string title, string message)
    {
        _titleText.text = title;
        _messageText.text = message;
    }

    public void CreateButton(string buttonText, System.Action onClickCallback)
    {
        Button newButton = Instantiate(_buttonPrefab, _buttonParent);
        newButton.gameObject.SetActive(true);

        TextMeshProUGUI buttonLabel = newButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonLabel != null)
        {
            buttonLabel.text = buttonText;
        }

        UIButtonFeedback.Ensure(newButton);

        newButton.onClick.AddListener(() => onClickCallback?.Invoke());
    }

    public void ClearButtons()
    {
        int childCount = _buttonParent.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            Destroy(_buttonParent.GetChild(i).gameObject);
        }
    }

    public void ShowPopup()
    {
        gameObject.SetActive(true);

        if (!_isPopupShown)
        {
            _selectionBeforePopup = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        }
        _isPopupShown = true;

        ShowBgBlocker();

        Transform panel = _popupCanvasGroup.transform;
        _popupSequence?.Kill();
        _popupCanvasGroup.blocksRaycasts = true;
        if (_popupCanvasGroup.alpha <= 0f)
        {
            panel.localScale = Vector3.one * _popupStartScale;
        }

        _popupSequence = DOTween.Sequence().SetUpdate(true)
            .Join(panel.DOScale(1f, _popupShowDuration).SetEase(Ease.OutBack))
            .Join(_popupCanvasGroup.DOFade(1f, _popupShowDuration * 0.7f).SetEase(Ease.OutQuad));

        SelectFirstPopupButton();
    }

    public void HidePopup()
    {
        _isPopupShown = false;
        HideBgBlocker();

        Transform panel = _popupCanvasGroup.transform;
        _popupSequence?.Kill();
        _popupCanvasGroup.blocksRaycasts = false;

        _popupSequence = DOTween.Sequence().SetUpdate(true)
            .Join(panel.DOScale(_popupStartScale, _popupHideDuration).SetEase(Ease.InBack))
            .Join(_popupCanvasGroup.DOFade(0f, _popupHideDuration).SetEase(Ease.InQuad))
            .OnComplete(ClearButtons);

        RestoreSelectionAfterPopup();
    }

    public bool IsPopupShown()
    {
        return _isPopupShown;
    }

    private void SelectFirstPopupButton()
    {
        if (EventSystem.current == null || !UIDefaultSelection.IsNavigationMode || _buttonParent.childCount == 0)
        {
            return;
        }
        EventSystem.current.SetSelectedGameObject(_buttonParent.GetChild(_buttonParent.childCount - 1).gameObject);
    }

    private void RestoreSelectionAfterPopup()
    {
        if (EventSystem.current == null)
        {
            return;
        }

        bool canRestore = UIDefaultSelection.IsNavigationMode
            && _selectionBeforePopup != null
            && _selectionBeforePopup.activeInHierarchy;
        EventSystem.current.SetSelectedGameObject(canRestore ? _selectionBeforePopup : null);
        _selectionBeforePopup = null;
    }
    #endregion

    #region FPS Monitor Methods
    public void UpdateFPSMonitor(int currentFps, float avgFps, int minFps, int maxFps, int lowFps, float frameTime)
    {
        _currentFpsText.text = $"Current FPS : {currentFps}";
        _avgFpsText.text = $"Avg FPS : {avgFps:F1}";
        _minFpsText.text = $"Min FPS : {minFps}";
        _maxFpsText.text = $"Max FPS : {maxFps}";
        _lowFpsText.text = $"1% Low FPS : {lowFps}";
        _frameTimeText.text = $"Frame Time : {frameTime:F2} ms";

        UpdateFPSColors(currentFps, (int)avgFps, minFps, maxFps, lowFps);
    }

    private void UpdateFPSColors(int currentFps, int avgFps, int minFps, int maxFps, int lowFps)
    {
        _currentFpsText.color = GetFPSColor(currentFps);
        _avgFpsText.color = GetFPSColor(avgFps);
        _minFpsText.color = GetFPSColor(minFps);
        _maxFpsText.color = GetFPSColor(maxFps);
        _lowFpsText.color = GetFPSColor(lowFps);
    }

    private Color GetFPSColor(int fps)
    {
        if (fps >= 240)
            return new Color(0f, 1f, 0f, 1f); // 초록색 (Lime Green)
        else if (fps >= 180)
            return new Color(0.5f, 1f, 0f, 1f); // 연초록 (Light Green)
        else if (fps >= 120)
            return new Color(0f, 1f, 1f, 1f); // 청록색 (Cyan)
        else if (fps >= 90)
            return new Color(1f, 1f, 0f, 1f); // 노란색 (Yellow)
        else if (fps >= 60)
            return new Color(1f, 0.65f, 0f, 1f); // 주황색 (Orange)
        else
            return new Color(1f, 0f, 0f, 1f); // 빨간색 (Red)
    }

    public void ShowFPSMonitor()
    {
        // ⚠️ IMPORTANT: Canvas Group의 alpha로만 제어 (gameObject 절대 건드리지 말 것!)
        if (_fpsMonitorCanvasGroup != null)
        {
            _fpsMonitorCanvasGroup.blocksRaycasts = false;
            _fpsMonitorCanvasGroup.alpha = 1f;
        }
    }

    public void HideFPSMonitor()
    {
        // ⚠️ IMPORTANT: Canvas Group의 alpha로만 제어 (gameObject 절대 건드리지 말 것!)
        if (_fpsMonitorCanvasGroup != null)
        {
            _fpsMonitorCanvasGroup.alpha = 0f;
            _fpsMonitorCanvasGroup.blocksRaycasts = false;
        }
    }
    #endregion
}
