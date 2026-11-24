using UnityEngine;

public class UI_PlatformPromptsView : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private UI_ConsentModalView _consentModal;

    [Header("문구")]
    [SerializeField, TextArea] private string _restoredTitle = "알림";
    [SerializeField, TextArea] private string _restoredBody = "구글 계정의 진행 상황을 불러왔습니다.";
    [SerializeField, TextArea] private string _linkedTitle = "알림";
    [SerializeField, TextArea] private string _linkedBody = "구글 계정과 연동되었습니다.\n진행 상황이 클라우드에 저장됩니다.";
    [SerializeField, TextArea] private string _confirm = "확인";
    [SerializeField, TextArea] private string _conflictTitle = "이어서 할 기록 선택";
    [SerializeField, TextArea] private string _conflictBody = "이 기기와 구글 계정에 서로 다른 진행 기록이 있습니다.{0}{1}";
    [SerializeField, TextArea] private string _deviceLine = "\n이 기기: {0}스테이지 클리어, 별 {1}개";
    [SerializeField, TextArea] private string _googleLine = "\n구글 계정: {0}스테이지 클리어, 별 {1}개";
    [SerializeField, TextArea] private string _chooseGoogle = "구글 계정 기록";
    [SerializeField, TextArea] private string _chooseDevice = "이 기기 기록";
    private void OnEnable()
    {
        LandLinkerServices.Notice += OnNotice;
        LandLinkerServices.ConflictChoiceRequested += OnConflict;
    }

    private void OnDisable()
    {
        LandLinkerServices.Notice -= OnNotice;
        LandLinkerServices.ConflictChoiceRequested -= OnConflict;
    }

    private void Update()
    {
        if (_consentModal != null && LandLinkerServices.ConsentPromptPending && !_consentModal.IsShown && GlobalUICanvasView.HasInstance && !GlobalUICanvasView.Instance.IsPopupShown() && (GlobalUICanvasView.Instance.SupportModal == null || !GlobalUICanvasView.Instance.SupportModal.IsShown) && !SceneTransitionManager.IsBusy)
            _consentModal.Show(LandLinkerServices.GrantConsent, LandLinkerServices.DeclineConsent);
    }

    private void OnNotice(PlatformNotice notice)
    {
        if (!GlobalUICanvasView.HasInstance)
            return;
        var popup = GlobalUICanvasView.Instance.PopupPresenter;
        popup.ShowPopup(notice == PlatformNotice.GoogleLinked ? _linkedTitle : _restoredTitle, notice == PlatformNotice.GoogleLinked ? _linkedBody : _restoredBody, (_confirm, () => popup.HidePopup()));
    }

    private void OnConflict(ConflictInfo info)
    {
        if (!GlobalUICanvasView.HasInstance)
            return;
        var popup = GlobalUICanvasView.Instance.PopupPresenter;
        string device = info.DeviceClearedStages < 0 || info.DeviceStars < 0 ? "" : string.Format(_deviceLine, info.DeviceClearedStages, info.DeviceStars);
        string google = info.GoogleClearedStages < 0 || info.GoogleStars < 0 ? "" : string.Format(_googleLine, info.GoogleClearedStages, info.GoogleStars);
        popup.ShowPopup(_conflictTitle, string.Format(_conflictBody, device, google), (_chooseGoogle, () =>
        {
            popup.HidePopup();
            LandLinkerServices.ResolveConflict(true);
        }), (_chooseDevice, () =>
        {
            popup.HidePopup();
            LandLinkerServices.ResolveConflict(false);
        }));
    }
}
