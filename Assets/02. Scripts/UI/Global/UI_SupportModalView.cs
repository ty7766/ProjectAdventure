using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_SupportModalView : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private CanvasGroup _root;
    [SerializeField] private ToggleGroup _categoryGroup;
    [SerializeField] private Toggle _accountToggle;
    [SerializeField] private Toggle _bugToggle;
    [SerializeField] private Toggle _sanctionToggle;
    [SerializeField] private Toggle _otherToggle;
    [SerializeField] private TMP_InputField _emailInput;
    [SerializeField] private TMP_InputField _bodyInput;
    [SerializeField] private TMP_Text _counterText;
    [SerializeField] private TMP_Text _errorText;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private Button _submitButton;
    [SerializeField] private TMP_Text _submitLabel;
    [SerializeField] private GameObject _formGroup;
    [SerializeField] private GameObject _doneGroup;
    [SerializeField] private TMP_Text _doneBodyText;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _openThreadButton;

    [Header("문구")]
    [SerializeField, TextArea] private string _submitText = "보내기";
    [SerializeField, TextArea] private string _sendingText = "보내는 중";
    [SerializeField, TextArea] private string _counterFormat = "{0} / 2000";
    [SerializeField, TextArea] private string _doneFormat = "문의 번호 #{0}\n답변은 문의 내역에서 확인할 수 있습니다.";
    [SerializeField, TextArea] private string _bodyError = "문의 내용을 5자 이상 입력해 주십시오.";
    [SerializeField, TextArea] private string _emailError = "이메일 주소 형식이 올바르지 않습니다.";
    [SerializeField, TextArea] private string _rateError = "잠시 후 다시 시도해 주십시오.";
    [SerializeField, TextArea] private string _failedError = "문의를 보내지 못했습니다. 네트워크 연결을 확인해 주십시오.";
    private UI_SupportModalPresenter _presenter;
    private Tween _fade;

    public bool IsShown { get; private set; }

    private void Awake()
    {
        _presenter = new UI_SupportModalPresenter(this);
        _presenter.Start();
        _cancelButton.onClick.AddListener(Hide);
        _closeButton.onClick.AddListener(Hide);
        _submitButton.onClick.AddListener(_presenter.Submit);
        _openThreadButton.onClick.AddListener(_presenter.OpenThread);
        _bodyInput.onValueChanged.AddListener(OnBodyChanged);
    }

    private void OnDestroy()
    {
        _fade?.Kill();
        if (_presenter != null)
            _presenter.Cleanup();
    }

    private void OnBodyChanged(string value)
    {
        _counterText.text = string.Format(_counterFormat, value.Length);
    }

    public string Body => _bodyInput.text.Trim();

    public string Email => _emailInput.text.Trim();

    public string Category => _accountToggle.isOn ? "account" : _bugToggle.isOn ? "bug" : _sanctionToggle.isOn ? "sanction" : "other";

    public void Show()
    {
        gameObject.SetActive(true);
        _emailInput.text = "";
        _bodyInput.text = "";
        _otherToggle.isOn = true;
        IsShown = true;
        _root.interactable = true;
        _root.blocksRaycasts = true;
        _fade?.Kill();
        _fade = _root.DOFade(1f, 0.196f).SetEase(Ease.OutQuad).SetUpdate(true);
        _presenter.Reset();
        OnBodyChanged(_bodyInput.text);
    }

    public void Hide()
    {
        IsShown = false;
        _root.blocksRaycasts = false;
        _root.interactable = false;
        _fade?.Kill();
        _fade = _root.DOFade(0f, 0.18f).SetEase(Ease.InQuad).SetUpdate(true).OnComplete(() => gameObject.SetActive(false));
    }

    public void SetSending(bool sending)
    {
        _submitButton.interactable = !sending;
        _cancelButton.interactable = !sending;
        _submitLabel.text = sending ? _sendingText : _submitText;
    }

    public void ShowForm()
    {
        _formGroup.SetActive(true);
        _doneGroup.SetActive(false);
        _errorText.gameObject.SetActive(false);
        SetSending(false);
    }

    public void ShowError(SupportError error)
    {
        _errorText.text = error == SupportError.TooShort ? _bodyError : error == SupportError.InvalidEmail ? _emailError : error == SupportError.RateLimited ? _rateError : _failedError;
        _errorText.gameObject.SetActive(true);
    }

    public void ShowDone(string reference, bool hasThread)
    {
        _formGroup.SetActive(false);
        _doneGroup.SetActive(true);
        _doneBodyText.text = string.Format(_doneFormat, reference);
        _openThreadButton.gameObject.SetActive(hasThread);
    }
}
