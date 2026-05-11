using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GlobalUICanvasView : Singleton<GlobalUICanvasView>
{
    #region Common Elements
    [Header("Background Blocker")]
    [SerializeField] private CanvasGroup _bgBlockerCanvasGroup;
    [SerializeField, Range(0.1f, 1f)] private float _fadeDuration = 0.3f;

    private Coroutine _bgBlockerFadeCoroutine;
    #endregion

    #region Popup Components
    [Header("Popup Components")]
    [SerializeField] private CanvasGroup _popupCanvasGroup;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private Transform _buttonParent;
    [SerializeField] private Button _buttonPrefab;

    private Coroutine _popupFadeCoroutine;
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
        if (_bgBlockerFadeCoroutine != null)
        {
            StopCoroutine(_bgBlockerFadeCoroutine);
        }
        _bgBlockerFadeCoroutine = StartCoroutine(FadeInBgBlocker());
    }

    public void HideBgBlocker()
    {
        if (_bgBlockerFadeCoroutine != null)
        {
            StopCoroutine(_bgBlockerFadeCoroutine);
        }
        _bgBlockerFadeCoroutine = StartCoroutine(FadeOutBgBlocker());
    }

    private IEnumerator FadeInBgBlocker()
    {
        _bgBlockerCanvasGroup.alpha = 0f;
        _bgBlockerCanvasGroup.blocksRaycasts = true;
        float timer = 0f;
        while (timer < _fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            _bgBlockerCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / _fadeDuration);
            yield return null;
        }
        _bgBlockerCanvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOutBgBlocker()
    {
        float timer = 0f;
        while (timer < _fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            _bgBlockerCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / _fadeDuration);
            yield return null;
        }
        _bgBlockerCanvasGroup.alpha = 0f;
        _bgBlockerCanvasGroup.blocksRaycasts = false;
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

        if (_popupFadeCoroutine != null)
        {
            StopCoroutine(_popupFadeCoroutine);
        }

        ShowBgBlocker();
        _popupFadeCoroutine = StartCoroutine(FadeInPopup());
    }

    public void HidePopup()
    {
        if (_popupFadeCoroutine != null)
        {
            StopCoroutine(_popupFadeCoroutine);
        }

        HideBgBlocker();
        _popupFadeCoroutine = StartCoroutine(FadeOutPopup());
        ClearButtons();
    }

    public bool IsPopupShown()
    {
        return _popupCanvasGroup.alpha > 0f;
    }

    private IEnumerator FadeInPopup()
    {
        _popupCanvasGroup.blocksRaycasts = true;
        float timer = 0f;
        while (timer < _fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            _popupCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / _fadeDuration);
            yield return null;
        }
        _popupCanvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOutPopup()
    {
        float timer = 0f;
        while (timer < _fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            _popupCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / _fadeDuration);
            yield return null;
        }
        _popupCanvasGroup.alpha = 0f;
        _popupCanvasGroup.blocksRaycasts = false;
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
