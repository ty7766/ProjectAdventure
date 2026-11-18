using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 옵션 > 사운드 탭 아래의 "계정" 섹션: 구글 계정 로그인(클라우드 세이브 연동) 버튼.
/// 코리게임즈 플랫폼이 켜진 Android 빌드에서 Play Games를 쓸 수 있을 때만 보인다. (PC 빌드에서는 항상 숨김)
/// </summary>
public class UI_AccountSectionView : MonoBehaviour
{
    [SerializeField, Tooltip("섹션 헤더 + 행. 플랫폼 기능이 없으면 숨긴다")] private GameObject[] _section;
    [SerializeField] private Button _googleButton;
    [SerializeField] private TextMeshProUGUI _googleButtonText;
    [SerializeField] private TextMeshProUGUI _statusText;

    private bool _busy;

    private void Awake()
    {
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

    private void Refresh()
    {
        bool visible = LandLinkerServices.GoogleAvailable;
        if (_section != null)
        {
            foreach (var go in _section)
            {
                if (go != null) go.SetActive(visible);
            }
        }
        if (!visible) return;

        bool signedIn = LandLinkerServices.GoogleSignedIn;
        if (_googleButtonText != null)
        {
            _googleButtonText.text = _busy ? "연결 중…" : signedIn ? "연동됨" : "로그인";
        }
        if (_googleButton != null)
        {
            _googleButton.interactable = !_busy && !signedIn;
        }
        if (_statusText != null)
        {
            _statusText.text = signedIn
                ? (string.IsNullOrEmpty(LandLinkerServices.GoogleDisplayName) ? "구글 계정" : LandLinkerServices.GoogleDisplayName)
                : "구글 계정";
        }
    }

    private void OnGoogleClicked()
    {
        if (_busy) return;
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
}
