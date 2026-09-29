using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class TitleFlowController : MonoBehaviour
{
    enum TitleState
    {
        Title,
        StageSelect,
        Options
    }

    [Header("Components")]
    [SerializeField]
    private TitleCameraController _titleCameraController;
    [SerializeField]
    private CanvasGroup _titleCanvas;
    [SerializeField]
    private CanvasGroup _stageSelectCanvas;
    [SerializeField]
    private CanvasGroup _optionsCanvas;

    [Header("Settings")]
    [SerializeField, Range(0.1f, 3f)]
    private float _fadeDuration = 0.5f;
    [SerializeField, Range(0.1f, 3f)]
    private float _introDuration = 0.5f;

    private const int STABLE_FRAME_COUNT = 3;
    private const float STABLE_FRAME_DELTA = 0.05f;
    private const float MAX_INTRO_WAIT = 2f;

    private Coroutine _fadeCoroutine;
    private CanvasGroup _currentActiveCanvas;
    private TitleState _currentState = TitleState.Title;
    private bool _isExitingTitle;

    private void Start()
    {
       InitializeTitleView();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            HandleEscapeInput();
        }
    }

    public void GoToTitle()
    {
        if (_isExitingTitle)
        {
            return;
        }
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }
        _titleCameraController.DoTransition("Title");
        _fadeCoroutine = StartCoroutine(SequentialFade(_currentActiveCanvas, _titleCanvas));
        _currentActiveCanvas = _titleCanvas;
        _currentState = TitleState.Title;
    }

    public void GoToStageSelect()
    {
        if (_isExitingTitle)
        {
            return;
        }

        // 타이틀에서 넘어갈 때는 타이틀 UI 퇴장 연출(역순 슬라이드 아웃)을 먼저 끝낸다
        if (_currentState == TitleState.Title && _titleCanvas.TryGetComponent(out IUIScreenTransition titleTransition))
        {
            _isExitingTitle = true;
            _titleCanvas.blocksRaycasts = false;
            _titleCanvas.interactable = false;
            titleTransition.PlayExit(() =>
            {
                _isExitingTitle = false;
                SwitchToStageSelect();
            });
            return;
        }

        SwitchToStageSelect();
    }

    private void SwitchToStageSelect()
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }
        _titleCameraController.DoTransition("StageSelect");
        _fadeCoroutine = StartCoroutine(SequentialFade(_currentActiveCanvas, _stageSelectCanvas));
        _currentActiveCanvas = _stageSelectCanvas;
        _currentState = TitleState.StageSelect;
    }

    public void GoToOptions()
    {
        if (_isExitingTitle)
        {
            return;
        }
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }
        _titleCameraController.DoTransition("Options");
        _optionsCanvas.gameObject.SetActive(true);
        _optionsCanvas.alpha = 0f;
        _currentActiveCanvas = _optionsCanvas;
        _fadeCoroutine = StartCoroutine(SequentialFade(_titleCanvas, _optionsCanvas));
        _currentState = TitleState.Options;
    }

    private void InitializeTitleView()
    {
        _currentActiveCanvas = _titleCanvas;
        _titleCameraController.SetCameraInstantly("Title");
        _stageSelectCanvas.blocksRaycasts = false;
        _stageSelectCanvas.interactable = false;
        _titleCanvas.blocksRaycasts = false;
        _titleCanvas.interactable = false;
        _titleCanvas.alpha = 0.0f;
        _stageSelectCanvas.alpha = 0.0f;
        _optionsCanvas.gameObject.SetActive(false);
        _titleCanvas.gameObject.SetActive(true);
        StartCoroutine(IntroRoutine());
    }

    private IEnumerator SequentialFade(CanvasGroup fadeOut, CanvasGroup fadeIn)
    {
        // 숨겨지는 화면의 버튼이 게임패드/키보드 네비게이션에 잡히지 않도록 interactable도 끈다
        fadeOut.blocksRaycasts = false;
        fadeOut.interactable = false;
        fadeIn.interactable = false;

        float halfDuration = _fadeDuration / 2f;
        float timer = 0f;

        while (timer < halfDuration)
        {
            timer += Time.unscaledDeltaTime;
            fadeOut.alpha = Mathf.Lerp(1f, 0f, timer / halfDuration);
            yield return null;
        }
        fadeOut.alpha = 0f;

        if (fadeOut == _optionsCanvas || fadeOut == _stageSelectCanvas)
        {
            fadeOut.gameObject.SetActive(false);
        }

        if(!fadeIn.gameObject.activeSelf)
        {
            fadeIn.gameObject.SetActive(true);
        }
        yield return EnterScreen(fadeIn, halfDuration);
    }

    private IEnumerator IntroRoutine()
    {
        // 씬 로드 직후의 프레임 스파이크(수백 ms)에 등장 연출이 통째로 건너뛰어지지 않도록 프레임이 안정될 때까지 대기
        int stableFrames = 0;
        float waited = 0f;
        while (stableFrames < STABLE_FRAME_COUNT && waited < MAX_INTRO_WAIT)
        {
            yield return null;
            waited += Time.unscaledDeltaTime;
            stableFrames = Time.unscaledDeltaTime < STABLE_FRAME_DELTA ? stableFrames + 1 : 0;
        }

        yield return EnterScreen(_titleCanvas, _introDuration);
    }

    /// <summary>
    /// 화면을 표시한다. 등장 연출(IUIScreenTransition)이 있으면 캔버스는 즉시 보이게 하고 연출에 맡기며,
    /// 없으면 기존처럼 알파 페이드 인. 끝나면 입력을 열고 포커스를 잡는다.
    /// </summary>
    private IEnumerator EnterScreen(CanvasGroup screen, float fadeDuration)
    {
        if (screen.TryGetComponent(out IUIScreenTransition transition))
        {
            bool enterDone = false;
            // PlayEnter가 같은 프레임에 요소들을 숨김 상태로 초기화하므로 알파를 바로 1로 올려도 튀지 않는다
            transition.PlayEnter(() => enterDone = true);
            screen.alpha = 1f;
            while (!enterDone)
            {
                yield return null;
            }
        }
        else
        {
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.unscaledDeltaTime;
                screen.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                yield return null;
            }
            screen.alpha = 1f;
        }

        screen.blocksRaycasts = true;
        screen.interactable = true;
        NotifyScreenShown(screen);
    }

    private static void NotifyScreenShown(CanvasGroup screen)
    {
        if (screen.TryGetComponent(out UIDefaultSelection selection))
        {
            selection.OnScreenShown();
        }
    }

    private void HandleEscapeInput()
    {
        if (_isExitingTitle)
        {
            return;
        }

        if (GlobalUICanvasView.Instance != null)
        {
            if (GlobalUICanvasView.Instance.IsPopupShown())
            {
                GlobalUICanvasView.Instance.HidePopup();
                return;
            }
        }
        else
        {
            Debug.LogWarning("[TitleFlowController] GlobalUICanvasView.Instance is not initialized.");
        }

        switch (_currentState)
        {
            case TitleState.Title:
                TryQuitGame();
                break;
            case TitleState.StageSelect:
            case TitleState.Options:
                GoToTitle();
                break;
        }
    }

    private void TryQuitGame()
    {
        if (GlobalUICanvasView.Instance != null && GlobalUICanvasView.Instance.PopupPresenter != null)
        {
            GlobalUICanvasView.Instance.PopupPresenter.ShowPopup(
                "경고",
                "정말 게임을 종료하시겠습니까?",
                ("확인", () =>
                {
                    GlobalUICanvasView.Instance.PopupPresenter.HidePopup();
                    PerformQuit();
                }
            ),
                ("취소", () => GlobalUICanvasView.Instance.PopupPresenter.HidePopup())
            );
        }
        else
        {
            PerformQuit();
        }
    }

    private void PerformQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}