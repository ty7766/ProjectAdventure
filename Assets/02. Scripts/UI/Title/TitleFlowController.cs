using System.Collections;
using UnityEngine;

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

    private Coroutine _fadeCoroutine;
    private CanvasGroup _currentActiveCanvas;
    private TitleState _currentState = TitleState.Title;

    private void Start()
    {
       InitializeTitleView();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleEscapeInput();
        }
    }

    public void GoToTitle()
    {
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
        _stageSelectCanvas.interactable = true;
        _titleCanvas.blocksRaycasts = false;
        _titleCanvas.interactable = true;
        _titleCanvas.alpha = 0.0f;
        _stageSelectCanvas.alpha = 0.0f;
        _optionsCanvas.gameObject.SetActive(false);
        _titleCanvas.gameObject.SetActive(true);
        StartCoroutine(FadeIn(_titleCanvas, _introDuration));
    }

    private IEnumerator SequentialFade(CanvasGroup fadeOut, CanvasGroup fadeIn)
    {
        fadeOut.blocksRaycasts = false;

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
        fadeIn.alpha = 0f;

        timer = 0f;
        while (timer < halfDuration)
        {
            timer += Time.unscaledDeltaTime;
            fadeIn.alpha = Mathf.Lerp(0f, 1f, timer / halfDuration);
            yield return null;
        }
        fadeIn.alpha = 1f;
        fadeIn.blocksRaycasts = true;
    }

    private IEnumerator FadeIn(CanvasGroup target, float duration)
    {
        target.interactable = true;
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            target.alpha = Mathf.Lerp(0f, 1f, timer / duration);
            yield return null;
        }
        target.alpha = 1f;
        target.blocksRaycasts = true;
    }

    private void HandleEscapeInput()
    {
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

    private static void TryQuitGame()
    {
        GlobalUICanvasView.Instance.Presenter.ShowPopup(
            "경고",
            "정말 게임을 종료하시겠습니까?",
            ("확인", () =>
            {
                GlobalUICanvasView.Instance.Presenter.HidePopup();
                Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            }
        ),
            ("취소", () => GlobalUICanvasView.Instance.Presenter.HidePopup())
        );
    }
}