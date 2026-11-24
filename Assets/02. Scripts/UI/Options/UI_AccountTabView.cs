using TMPro;
using UnityEngine;
using UnityEngine.UI;

public struct AccountTabState
{
    public bool Online;
    public bool Linking;
    public bool Working;
    public bool Linked;
    public string Name;
    public string Id;
    public bool GoogleAvailable;
    public bool AdPrivacy;
}

public class UI_AccountTabView : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private TMP_Text _googleStatusText;
    [SerializeField] private Button _googleButton;
    [SerializeField] private TMP_Text _googleButtonLabel;
    [SerializeField] private GameObject _accountIdRow;
    [SerializeField] private TMP_Text _accountIdText;
    [SerializeField] private Button _copyButton;
    [SerializeField] private GameObject _deleteRow;
    [SerializeField] private Button _deleteButton;
    [SerializeField] private Button _supportButton;
    [SerializeField] private Button _privacyButton;
    [SerializeField] private Button _termsButton;
    [SerializeField] private GameObject _adPrivacyRow;
    [SerializeField] private Button _adPrivacyButton;

    [Header("문구")]
    [SerializeField, TextArea] private string _offline = "오프라인 플레이 중";
    [SerializeField, TextArea] private string _guest = "게스트";
    [SerializeField, TextArea] private string _connecting = "연결 중";
    [SerializeField, TextArea] private string _linked = "연동됨";
    [SerializeField, TextArea] private string _goOnline = "온라인 전환";
    [SerializeField, TextArea] private string _link = "연동하기";
    [SerializeField, TextArea] private string _copyTitle = "복사했습니다";
    [SerializeField, TextArea] private string _copyBody = "계정 번호를 클립보드에 복사했습니다.";
    [SerializeField, TextArea] private string _deleteTitle = "계정 삭제";
    [SerializeField, TextArea] private string _deleteBody = "클라우드와 이 기기에 저장된 모든 진행 기록이 삭제됩니다. 삭제한 기록은 복구할 수 없습니다.";
    [SerializeField, TextArea] private string _deleteConfirmTitle = "정말 삭제하시겠습니까?";
    [SerializeField, TextArea] private string _deleteConfirmBody = "이 작업은 되돌릴 수 없습니다.";
    [SerializeField, TextArea] private string _deleteDoneTitle = "계정을 삭제했습니다";
    [SerializeField, TextArea] private string _deleteDoneBody = "오프라인 상태로 처음부터 시작합니다.";
    [SerializeField, TextArea] private string _deleteFailedTitle = "계정을 삭제하지 못했습니다";
    [SerializeField, TextArea] private string _deleteFailedBody = "네트워크 연결을 확인한 뒤 다시 시도해 주십시오.";
    [SerializeField, TextArea] private string _linkFailedTitle = "연동하지 못했습니다";
    [SerializeField, TextArea] private string _linkFailedBody = "잠시 후 다시 시도해 주십시오.";
    [SerializeField, TextArea] private string _cancel = "취소";
    [SerializeField, TextArea] private string _continue = "계속";
    [SerializeField, TextArea] private string _permanentDelete = "영구 삭제";
    [SerializeField, TextArea] private string _confirm = "확인";
    private UI_AccountTabPresenter _presenter;

    private void Awake()
    {
        _presenter = new UI_AccountTabPresenter(this);
        _googleButton.onClick.AddListener(_presenter.Google);
        _copyButton.onClick.AddListener(_presenter.Copy);
        _deleteButton.onClick.AddListener(_presenter.Delete);
        _supportButton.onClick.AddListener(_presenter.Support);
        _privacyButton.onClick.AddListener(LandLinkerServices.OpenPrivacyPolicy);
        _termsButton.onClick.AddListener(LandLinkerServices.OpenTermsOfService);
        _adPrivacyButton.onClick.AddListener(_presenter.AdPrivacy);
    }

    private void OnEnable()
    {
        if (_presenter != null)
            _presenter.Start();
    }

    private void OnDisable()
    {
        if (_presenter != null)
            _presenter.Cleanup();
    }

    public void Refresh()
    {
        if (_presenter != null)
            _presenter.Refresh();
    }

    public void SetState(AccountTabState state)
    {
        bool online = state.Online;
        bool busy = state.Linking || state.Working;
        bool linked = state.Linked;
        string name = state.Name;
        string id = state.Id;
        bool googleAvailable = state.GoogleAvailable;
        bool adPrivacy = state.AdPrivacy;
        _googleStatusText.text = !online ? _offline : state.Linking ? _connecting : linked ? (string.IsNullOrEmpty(name) ? _linked : name) : _guest;
        _googleButton.gameObject.SetActive(!linked);
        _googleButtonLabel.text = !online ? _goOnline : _link;
        _googleButton.interactable = !busy && (!online || googleAvailable);
        _accountIdRow.SetActive(online);
        _deleteRow.SetActive(online);
        _accountIdText.text = id;
        _copyButton.interactable = !busy && !string.IsNullOrEmpty(id);
        _deleteButton.interactable = !busy;
        _supportButton.interactable = !busy;
        _privacyButton.interactable = !busy;
        _termsButton.interactable = !busy;
        _adPrivacyRow.SetActive(adPrivacy);
        _adPrivacyButton.interactable = !busy;
    }

    private GlobalUICanvasPopupPresenter Popup => GlobalUICanvasView.HasInstance ? GlobalUICanvasView.Instance.PopupPresenter : null;

    public void Message(string title, string body)
    {
        var popup = Popup;
        if (popup != null)
            popup.ShowPopup(title, body, (_confirm, () => popup.HidePopup()));
    }

    public void CopyMessage() => Message(_copyTitle, _copyBody);

    public void LinkFailed() => Message(_linkFailedTitle, _linkFailedBody);

    public void DeleteResult(bool ok) => Message(ok ? _deleteDoneTitle : _deleteFailedTitle, ok ? _deleteDoneBody : _deleteFailedBody);

    public void ConfirmDelete(System.Action action)
    {
        var popup = Popup;
        if (popup == null)
            return;
        popup.ShowPopup(_deleteTitle, _deleteBody, (_cancel, () => popup.HidePopup()), (_continue, () => popup.ShowPopup(_deleteConfirmTitle, _deleteConfirmBody, (_cancel, () => popup.HidePopup()), (_permanentDelete, () =>
        {
            popup.HidePopup();
            action();
        }))));
    }
}
