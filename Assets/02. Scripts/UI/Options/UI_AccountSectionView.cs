using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 옵션 > 사운드 탭 아래의 "계정" 및 "지원 및 약관" 섹션.
/// 코리게임즈 플랫폼이 켜진 Android 빌드에서는 항상 표시되며 (Google Play 게임즈 연동, 계정 ID 복사, 계정 삭제,
/// 고객 문의, 개인정보 처리방침, 이용약관, 광고 개인정보 설정), PC 빌드에서는 항상 숨긴다.
/// </summary>
public class UI_AccountSectionView : MonoBehaviour
{
    [SerializeField, Tooltip("섹션 헤더 + 행. 플랫폼 기능이 없으면 숨긴다")] private GameObject[] _section;
    [SerializeField] private Button _googleButton;
    [SerializeField] private TextMeshProUGUI _googleButtonText;
    [SerializeField] private TextMeshProUGUI _statusText;

    private struct ClonedRow
    {
        public GameObject Root;
        public TextMeshProUGUI Label;
        public Button ActionButton;
        public TextMeshProUGUI ActionLabel;
    }

    private readonly List<GameObject> _extraObjects = new List<GameObject>();
    private ClonedRow _accountIdRow;
    private ClonedRow _deleteAccountRow;
    private ClonedRow _supportRow;
    private ClonedRow _privacyRow;
    private ClonedRow _termsRow;
    private ClonedRow _adConsentRow;

    private bool _built;
    private bool _busy;
    private string _lastPlayerId = "";

    private void Awake()
    {
        if (LandLinkerServices.IsEnabled)
        {
            BuildExtraRowsIfNeeded();
        }
        if (_googleButton != null)
        {
            _googleButton.onClick.AddListener(OnGoogleClicked);
        }
    }

    private void OnEnable()
    {
        LandLinkerServices.StateChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        LandLinkerServices.StateChanged -= Refresh;
    }

    private void Update()
    {
        if (!LandLinkerServices.IsEnabled) return;
        string currentId = LandLinkerServices.PlayerId ?? "";
        if (currentId != _lastPlayerId)
        {
            Refresh();
        }
    }

    private void BuildExtraRowsIfNeeded()
    {
        if (_built || _section == null || _section.Length < 2 || _section[0] == null || _section[1] == null)
        {
            return;
        }
        _built = true;

        Transform parent = _section[1].transform.parent;
        int siblingIndex = _section[1].transform.GetSiblingIndex() + 1;

        _accountIdRow = CloneRow(parent, ref siblingIndex, "UI_AccountId", OnCopyPlayerIdClicked);
        _deleteAccountRow = CloneRow(parent, ref siblingIndex, "UI_DeleteAccount", OnDeleteAccountClicked);

        GameObject supportHeader = Instantiate(_section[0], parent);
        supportHeader.name = "UI_TextDisplay_Support";
        supportHeader.transform.SetSiblingIndex(siblingIndex++);
        var headerText = supportHeader.GetComponent<TextMeshProUGUI>() ?? supportHeader.GetComponentInChildren<TextMeshProUGUI>(true);
        if (headerText != null) headerText.text = "   지원 및 약관";
        _extraObjects.Add(supportHeader);

        _supportRow = CloneRow(parent, ref siblingIndex, "UI_CustomerSupport", OnSupportClicked);
        _privacyRow = CloneRow(parent, ref siblingIndex, "UI_PrivacyPolicy", () => LandLinkerServices.OpenPrivacyPolicy());
        _termsRow = CloneRow(parent, ref siblingIndex, "UI_TermsOfService", () => LandLinkerServices.OpenTermsOfService());
        _adConsentRow = CloneRow(parent, ref siblingIndex, "UI_AdPrivacyOptions", OnAdPrivacyClicked);
    }

    private ClonedRow CloneRow(Transform parent, ref int siblingIndex, string rowName, UnityEngine.Events.UnityAction onClick)
    {
        GameObject rowObj = Instantiate(_section[1], parent);
        rowObj.name = rowName;
        rowObj.transform.SetSiblingIndex(siblingIndex++);
        _extraObjects.Add(rowObj);

        Button btn = rowObj.GetComponentInChildren<Button>(true);
        TextMeshProUGUI btnLabel = btn != null ? btn.GetComponentInChildren<TextMeshProUGUI>(true) : null;
        TextMeshProUGUI rowLabel = null;
        foreach (var tmp in rowObj.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (tmp != btnLabel)
            {
                rowLabel = tmp;
                break;
            }
        }

        if (btn != null)
        {
            btn.onClick = new Button.ButtonClickedEvent();
            btn.onClick.AddListener(onClick);
            UIButtonFeedback.Ensure(btn);
        }

        return new ClonedRow
        {
            Root = rowObj,
            Label = rowLabel,
            ActionButton = btn,
            ActionLabel = btnLabel,
        };
    }

    private void Refresh()
    {
        bool onlineEnabled = LandLinkerServices.IsEnabled;
        bool optInAvailable = !onlineEnabled && LandLinkerServices.CanOptInOnThisPlatform;
        bool visible = onlineEnabled || optInAvailable;
        if (visible) BuildExtraRowsIfNeeded();

        if (_section != null)
        {
            foreach (var go in _section)
            {
                if (go != null) go.SetActive(visible);
            }
        }
        if (!visible)
        {
            foreach (var go in _extraObjects)
            {
                if (go != null) go.SetActive(false);
            }
            return;
        }

        if (optInAvailable)
        {
            if (_statusText != null) _statusText.text = "온라인 · 클라우드 저장 (비활성)";
            if (_googleButtonText != null) _googleButtonText.text = "사용 동의";
            if (_googleButton != null) _googleButton.interactable = true;

            if (_accountIdRow.Root != null) _accountIdRow.Root.SetActive(false);
            if (_deleteAccountRow.Root != null) _deleteAccountRow.Root.SetActive(false);
            if (_supportRow.Root != null) _supportRow.Root.SetActive(false);
            if (_adConsentRow.Root != null) _adConsentRow.Root.SetActive(false);
            if (_privacyRow.Root != null)
            {
                _privacyRow.Root.SetActive(true);
                UpdateRow(_privacyRow, "개인정보 처리방침", "보기", true);
            }
            if (_termsRow.Root != null)
            {
                _termsRow.Root.SetActive(true);
                UpdateRow(_termsRow, "이용약관", "보기", true);
            }
            return;
        }

        foreach (var go in _extraObjects)
        {
            if (go != null) go.SetActive(true);
        }

        bool googleAvailable = LandLinkerServices.GoogleAvailable;
        bool linked = LandLinkerServices.GoogleLinked;

        if (_googleButtonText != null)
        {
            _googleButtonText.text = _busy ? "연결 중…"
                : linked ? "연동됨"
                : googleAvailable ? "연동"
                : "미지원";
        }
        if (_googleButton != null)
        {
            _googleButton.interactable = !_busy && googleAvailable && !linked;
        }
        if (_statusText != null)
        {
            if (linked)
            {
                string displayName = LandLinkerServices.GoogleDisplayName;
                _statusText.text = string.IsNullOrEmpty(displayName)
                    ? "Google 계정 (연동됨)"
                    : $"Google 계정 ({displayName})";
            }
            else
            {
                _statusText.text = googleAvailable
                    ? "Google 계정 (미연동 · 게스트)"
                    : "Google 계정 (게스트)";
            }
        }

        string pid = LandLinkerServices.PlayerId ?? "";
        _lastPlayerId = pid;
        string shortId = FormatPlayerId(pid);
        UpdateRow(_accountIdRow, $"계정 ID: {shortId}", "복사", !_busy && !string.IsNullOrEmpty(pid));
        UpdateRow(_deleteAccountRow, "계정 및 클라우드 데이터", "계정 삭제", !_busy);
        UpdateRow(_supportRow, "고객 문의 (버그 · 계정 · 기타)", "문의하기", !_busy);
        UpdateRow(_privacyRow, "개인정보 처리방침", "보기", true);
        UpdateRow(_termsRow, "이용약관", "보기", true);

        if (_adConsentRow.Root != null)
        {
            bool showAdConsent = LandLinkerServices.PrivacyOptionsRequired;
            _adConsentRow.Root.SetActive(showAdConsent);
            if (showAdConsent)
            {
                UpdateRow(_adConsentRow, "광고 개인정보 설정", "설정", !_busy);
            }
        }
    }

    private static void UpdateRow(ClonedRow row, string label, string buttonText, bool interactable)
    {
        if (row.Label != null) row.Label.text = label;
        if (row.ActionLabel != null) row.ActionLabel.text = buttonText;
        if (row.ActionButton != null) row.ActionButton.interactable = interactable;
    }

    private static string FormatPlayerId(string pid)
    {
        if (string.IsNullOrEmpty(pid)) return "연결 중…";
        if (pid.Length <= 8) return "#" + pid.ToUpperInvariant();
        return "#" + pid.Substring(pid.Length - 4).ToUpperInvariant() + " (" + pid.Substring(0, 6) + "…)";
    }

    private void OnGoogleClicked()
    {
        if (_busy) return;
        if (!LandLinkerServices.IsEnabled && LandLinkerServices.CanOptInOnThisPlatform)
        {
            LandLinkerServices.RequestConsentAndEnable();
            return;
        }
        _busy = true;
        Refresh();
        LandLinkerServices.SignInGoogle(error =>
        {
            _busy = false;
            if (this == null) return;
            Refresh();
            if (error != null && error != "google_cancelled" && GlobalUICanvasView.HasInstance)
            {
                var popup = GlobalUICanvasView.Instance.PopupPresenter;
                popup.ShowPopup("알림", "구글 계정에 연결하지 못했습니다.\n잠시 후 다시 시도해 주세요.", ("확인", () => popup.HidePopup()));
            }
        });
    }

    private void OnCopyPlayerIdClicked()
    {
        string pid = LandLinkerServices.PlayerId;
        if (string.IsNullOrEmpty(pid)) return;
        GUIUtility.systemCopyBuffer = pid;
        if (GlobalUICanvasView.HasInstance)
        {
            var popup = GlobalUICanvasView.Instance.PopupPresenter;
            popup.ShowPopup("계정 ID 복사", $"계정 ID를 클립보드에 복사했습니다.\n<size=80%>{pid}</size>", ("확인", () => popup.HidePopup()));
        }
    }

    private void OnDeleteAccountClicked()
    {
        if (_busy || !GlobalUICanvasView.HasInstance) return;
        var popup = GlobalUICanvasView.Instance.PopupPresenter;
        popup.ShowPopup(
            "계정 삭제",
            "계정을 삭제하면 서버와 이 기기에 저장된 모든 진행 데이터가 영구적으로 삭제되며 복구할 수 없습니다.\n계속하시겠습니까?",
            ("계속", () =>
            {
                popup.ShowPopup(
                    "최종 확인",
                    "정말로 계정을 영구 삭제하시겠습니까?\n이 작업은 되돌릴 수 없습니다.",
                    ("영구 삭제", () =>
                    {
                        popup.HidePopup();
                        ExecuteDeleteAccount();
                    }),
                    ("취소", () => popup.HidePopup())
                );
            }),
            ("취소", () => popup.HidePopup())
        );
    }

    private void ExecuteDeleteAccount()
    {
        _busy = true;
        Refresh();
        LandLinkerServices.DeleteAccount((ok, error) =>
        {
            _busy = false;
            if (this == null) return;
            Refresh();
            if (!GlobalUICanvasView.HasInstance) return;
            var popup = GlobalUICanvasView.Instance.PopupPresenter;
            if (ok)
            {
                popup.ShowPopup("계정 삭제 완료", "계정과 저장된 데이터가 삭제되었습니다.", ("확인", () => popup.HidePopup()));
            }
            else
            {
                popup.ShowPopup("삭제 실패", "계정을 삭제하지 못했습니다.\n네트워크 연결을 확인한 뒤 다시 시도해 주세요.", ("확인", () => popup.HidePopup()));
            }
        });
    }

    private void OnSupportClicked()
    {
        if (_busy) return;
        TMP_FontAsset font = _statusText != null ? _statusText.font : TMP_Settings.defaultFontAsset;
        UI_SupportModal.Show(font);
    }

    private void OnAdPrivacyClicked()
    {
        if (_busy) return;
        _busy = true;
        Refresh();
        LandLinkerServices.ShowPrivacyOptions(() =>
        {
            _busy = false;
            if (this != null) Refresh();
        });
    }
}
