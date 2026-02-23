using System.Collections;
using UnityEngine;

public class TitleFlowController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField]
    private TitleCameraController _titleCameraController;
    [SerializeField]
    private CanvasGroup _titleCanvas;
    [SerializeField]
    private CanvasGroup _stageSelectCanvas;

    [Header("Settings")]
    [SerializeField, Range(0.1f, 3f)]
    private float _fadeDuration = 0.5f;
    [SerializeField, Range(0.1f, 3f)]
    private float _introDuration = 0.5f;

    private Coroutine _fadeCoroutine;

    private void Start()
    {
       InitializeTitleView();
    }

    public void GoToTitle()
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _titleCameraController.DoTransition("Title");
        _fadeCoroutine = StartCoroutine(SequentialFade(_stageSelectCanvas, _titleCanvas));
    }

    public void GoToStageSelect()
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _titleCameraController.DoTransition("StageSelect");
        _fadeCoroutine = StartCoroutine(SequentialFade(_titleCanvas, _stageSelectCanvas));
    }

    private void InitializeTitleView()
    {
        _stageSelectCanvas.blocksRaycasts = false;
        _stageSelectCanvas.interactable = false;
        _titleCanvas.blocksRaycasts = true;
        _titleCanvas.interactable = true;
        _titleCanvas.alpha = 0.0f;
        StartCoroutine(FadeIn(_titleCanvas, _introDuration));
    }

    private IEnumerator SequentialFade(CanvasGroup fadeOut, CanvasGroup fadeIn)
    {
        // 터치 컷
        fadeOut.interactable = false;
        fadeOut.blocksRaycasts = false;
        // 다 켜진 캔버스만 터치 온
        fadeIn.interactable = true;
        fadeIn.blocksRaycasts = true;

        // 전체 시간을 반으로 나눠서 페이드아웃, 페이드인에 각각 사용!
        float halfDuration = _fadeDuration / 2f;
        float timer = 0f;

        // 1. 먼저 깔끔하게 페이드아웃 싹싹
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            fadeOut.alpha = Mathf.Lerp(1f, 0f, timer / halfDuration);
            yield return null;
        }
        fadeOut.alpha = 0f;

        // 2. 끝나면 바로 페이드인 시작
        timer = 0f;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            fadeIn.alpha = Mathf.Lerp(0f, 1f, timer / halfDuration);
            yield return null;
        }
        fadeIn.alpha = 1f;
    }

    private IEnumerator FadeIn(CanvasGroup target, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            target.alpha = Mathf.Lerp(0f, 1f, timer / duration);
            yield return null;
        }
        target.alpha = 1f;
    }
}