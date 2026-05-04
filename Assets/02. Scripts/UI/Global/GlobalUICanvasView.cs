using UnityEngine;
using TMPro;
using UnityEngine.UI;
using GameManager.Singleton;
using System.Collections;

public class GlobalUICanvasView : Singleton<GlobalUICanvasView>
{
    [Header("Popup Components")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private Transform _buttonParent;
    [SerializeField] private Button _buttonPrefab;
    [SerializeField, Range(0.1f, 1f)] private float _fadeDuration = 0.3f;

    private GlobalUICanvasPopupPresenter _presenter;
    private Coroutine _fadeCoroutine;

    public GlobalUICanvasPopupPresenter Presenter => _presenter;

    protected override void Awake()
    {
        base.Awake();
        _presenter = new GlobalUICanvasPopupPresenter(this);
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
    }

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
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = true;

        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }
        _fadeCoroutine = StartCoroutine(FadeIn());
    }

    public void HidePopup()
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }
        _fadeCoroutine = StartCoroutine(FadeOut());
    }

    private IEnumerator FadeIn()
    {
        float timer = 0f;
        while (timer < _fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / _fadeDuration);
            yield return null;
        }
        _canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOut()
    {
        float timer = 0f;
        while (timer < _fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / _fadeDuration);
            yield return null;
        }
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
        ClearButtons();
    }
}
