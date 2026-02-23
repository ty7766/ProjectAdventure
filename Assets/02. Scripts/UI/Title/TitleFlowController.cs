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

    private Coroutine _fadeCoroutine;

    public void GoToTitle()
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _titleCameraController.DoTransition("Title");
        _fadeCoroutine = StartCoroutine(Crossfade(_stageSelectCanvas, _titleCanvas));

    }

    public void GoToStageSelect()
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _titleCameraController.DoTransition("StageSelect");
        _fadeCoroutine = StartCoroutine(Crossfade(_titleCanvas, _stageSelectCanvas));
    }

    private IEnumerator Crossfade(CanvasGroup fadeOut, CanvasGroup fadeIn)
    {
        // 페이드 시작할 때 터치 컷
        fadeOut.interactable = false;
        fadeOut.blocksRaycasts = false;

        // 켜질 캔버스도 완전히 켜지기 전까진 터치 막아두기
        fadeIn.interactable = false;
        fadeIn.blocksRaycasts = false;

        float timer = 0f;
        while (timer < _fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / _fadeDuration;

            fadeOut.alpha = Mathf.Lerp(1f, 0f, progress);
            fadeIn.alpha = Mathf.Lerp(0f, 1f, progress);

            yield return null;
        }

        // 오차 없이 확실하게 마무리
        fadeOut.alpha = 0f;
        fadeIn.alpha = 1f;

        // 다 켜진 캔버스만 터치 온
        fadeIn.interactable = true;
        fadeIn.blocksRaycasts = true;
    }
}