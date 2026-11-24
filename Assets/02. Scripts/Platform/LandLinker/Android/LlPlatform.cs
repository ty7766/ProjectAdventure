#if UNITY_ANDROID
using System;
using System.Collections;
using GameManager.Singleton;
using Kory.Platform;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// Android 빌드에서 KORYGAMES 플랫폼 SDK를 띄우고 게임과 연결한다. (게임 키 "ll")
/// PC 빌드에는 컴파일되지 않는다. Resources/KoryConfig의 autoInitialize는 꺼 두고 여기서만 초기화하므로
/// 다른 타겟에서는 SDK가 절대 시작되지 않는다.
/// 또한 최초 실행 시 명시적 고지·동의(Prominent Disclosure)를 받은 뒤에만 SDK·분석·세션을 초기화한다.
/// </summary>
public static class LlPlatform
{
    public const string GameKey = "ll";
    private const string ConsentPrefKey = "kory.ll.consent.v1";

    private static bool s_unlinkedAfterDelete;

    public static bool IsRunning => KoryPlatform.IsInitialized && HasConsent;

    public static bool HasConsent => PlayerPrefs.GetInt(ConsentPrefKey, 0) == 1;

    public static bool CanOptIn => (!KoryPlatform.IsInitialized || !HasConsent) && !KoryPlatform.OfflineRequested;

    public static bool GoogleLinked =>
        !s_unlinkedAfterDelete
        && IsRunning
        && KorySession.HasAccount
        && KoryGoogleAccount.Instance != null
        && (KoryGoogleAccount.Instance.Linked || KoryGoogleAccount.Instance.SignedIn);

    public static string PlayerId => IsRunning ? KorySession.PlayerId : "";

    public static bool PrivacyOptionsRequired => IsRunning && KoryAds.PrivacyOptionsRequired;

    // SDK 부팅(BeforeSceneLoad)보다 먼저: 원격 설정·분석 경로를 이 게임 경로로 돌린다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Routes()
    {
        s_unlinkedAfterDelete = false;
        KoryRoutes.Config = KoryRoutes.GamePrefix + GameKey + "/config";
        KoryRoutes.Events = KoryRoutes.GamePrefix + GameKey + "/events";
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot()
    {
        if (KoryPlatform.IsInitialized || KoryPlatform.OfflineRequested)
        {
            return;
        }

        if (HasConsent)
        {
            InitializePlatform();
            return;
        }

        // 아직 최초 동의를 받지 않은 경우(최초 실행) 백그라운드 전송·세션 생성 없이 UI 준비 후 고지 팝업만 띄운다.
        if (!PlayerPrefs.HasKey(ConsentPrefKey))
        {
            var gate = new GameObject("LlConsentGate");
            UnityEngine.Object.DontDestroyOnLoad(gate);
            gate.AddComponent<LlConsentGate>();
        }
    }

    public static void RequestConsentFromOptions()
    {
        if (IsRunning || KoryPlatform.OfflineRequested) return;
        LlConsentGate.ShowDisclosurePopup(() =>
        {
            GrantConsentAndBoot();
        }, DeclineConsent);
    }

    public static void GrantConsentAndBoot()
    {
        PlayerPrefs.SetInt(ConsentPrefKey, 1);
        PlayerPrefs.Save();
        InitializePlatform();
        if (KoryPlatform.IsInitialized && !KorySession.HasAccount)
        {
            _ = KorySession.StartGuest();
        }
        LandLinkerServices.RaiseStateChanged();
    }

    public static void DeclineConsent()
    {
        PlayerPrefs.SetInt(ConsentPrefKey, 0);
        PlayerPrefs.Save();
        LandLinkerServices.RaiseStateChanged();
    }

    private static void InitializePlatform()
    {
        if (KoryPlatform.IsInitialized || KoryPlatform.OfflineRequested)
        {
            return;
        }

        var config = KoryConfig.Load();
        if (config == null)
        {
            Debug.LogWarning("[LandLinker] Resources/KoryConfig 가 없어 플랫폼 기능을 끕니다.");
            return;
        }

        KorySession.AfterLogin = LlCloudSave.PullAfterLogin;
        KoryAnalyticsPump.PerfSampling = () => StageLoader.HasInstance && Time.timeScale > 0f;
        KoryPlatform.Notice -= OnNotice;
        KoryPlatform.Notice += OnNotice;
        KoryGoogleAccount.Conflict -= OnGoogleConflict;
        KoryGoogleAccount.Conflict += OnGoogleConflict;
        KoryGoogleAccount.StateChanged -= LandLinkerServices.RaiseStateChanged;
        KoryGoogleAccount.StateChanged += LandLinkerServices.RaiseStateChanged;
        KorySession.Changed -= LandLinkerServices.RaiseStateChanged;
        KorySession.Changed += LandLinkerServices.RaiseStateChanged;

        KoryPlatform.Initialize(config);

        LlCloudSave.Attach();
        LlInterstitialAds.Attach(KoryPlatform.Host);
        KoryPlatform.Host.AddComponent<LlPlatformRunner>();
    }

    public static void Track(string name, params (string key, object value)[] props)
    {
        if (!IsRunning) return;
        KoryAnalytics.Track(name, props);
    }

    public static async void SignInGoogle(Action<string> done)
    {
        var google = KoryGoogleAccount.Instance;
        if (!IsRunning || google == null || !google.Available)
        {
            done?.Invoke("unavailable");
            return;
        }
        try
        {
            s_unlinkedAfterDelete = false;
            var result = await google.SignInInteractive();
            done?.Invoke(string.IsNullOrEmpty(result.errorCode) ? null : result.errorCode);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            done?.Invoke("error");
        }
    }

    public static void ShowPrivacyOptions(Action done)
    {
        if (!IsRunning)
        {
            done?.Invoke();
            return;
        }
        KoryAds.ShowPrivacyOptions(() =>
        {
            LandLinkerServices.RaiseStateChanged();
            done?.Invoke();
        });
    }

    public static async void DeleteAccount(Action<bool, string> done)
    {
        if (!IsRunning)
        {
            done?.Invoke(false, "unavailable");
            return;
        }
        if (!KorySession.HasAccount)
        {
            LlCloudSave.Reset();
            if (GameSaveManager.HasInstance) GameSaveManager.Instance.ResetAllProgress();
            done?.Invoke(true, null);
            return;
        }
        try
        {
            var result = await KoryApi.Delete<JObject>(KoryRoutes.Account);
            if (!result.ok)
            {
                done?.Invoke(false, string.IsNullOrEmpty(result.errorCode) ? "error" : result.errorCode);
                return;
            }

            s_unlinkedAfterDelete = true;
            LlCloudSave.Reset();
            if (GameSaveManager.HasInstance)
            {
                GameSaveManager.Instance.ResetAllProgress();
            }
            KorySession.SignOut();
            DeclineConsent();
            done?.Invoke(true, null);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            done?.Invoke(false, "error");
        }
    }

    public static async void SubmitSupportTicket(string category, string body, string email, Action<bool, string, string, string> done)
    {
        if (!IsRunning)
        {
            done?.Invoke(false, null, null, "unavailable");
            return;
        }
        try
        {
            if (!KorySession.HasAccount)
            {
                var auth = await KorySession.StartGuest();
                if (auth != KoryAuthOutcome.SignedIn && string.IsNullOrEmpty(KorySession.Token))
                {
                    done?.Invoke(false, null, null, "offline");
                    return;
                }
            }

            var payload = new JObject
            {
                ["category"] = category,
                ["body"] = body,
                ["device"] = new JObject
                {
                    ["appVersion"] = Application.version,
                    ["platform"] = Application.platform.ToString(),
                    ["os"] = SystemInfo.operatingSystem,
                    ["model"] = SystemInfo.deviceModel,
                },
            };
            if (!string.IsNullOrWhiteSpace(email))
            {
                payload["email"] = email.Trim();
            }

            var result = await KoryApi.Post<JObject>(KoryRoutes.SupportTickets, payload);
            if (result.ok)
            {
                string ticketRef = result.data?["ref"]?.ToString() ?? "";
                string threadUrl = result.data?["threadUrl"]?.ToString() ?? "";
                done?.Invoke(true, ticketRef, threadUrl, null);
            }
            else
            {
                string code = !string.IsNullOrEmpty(result.errorCode)
                    ? result.errorCode
                    : (result.networkError ? "network_error" : "error");
                done?.Invoke(false, null, null, code);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            done?.Invoke(false, null, null, "error");
        }
    }

    private static void OnNotice(KoryNotice notice, string serverMessage)
    {
        switch (notice)
        {
            case KoryNotice.GoogleProgressRestored:
                ShowPopup("알림", "구글 계정의 진행 상황을 불러왔습니다.");
                break;
            case KoryNotice.GoogleLinked:
                ShowPopup("알림", "구글 계정과 연동되었습니다.\n진행 상황이 클라우드에 저장됩니다.");
                break;
            // 오프라인이어도 게임은 로컬 세이브로 그대로 진행되므로 따로 알리지 않는다.
        }
    }

    // 이 기기 계정과 구글 계정에 각각 진행도가 있을 때. 어느 쪽을 고르든 이 기기의 로컬 진행도는
    // 접속 후 클라우드 세이브와 합쳐지므로(클리어 OR, 별 최댓값) 진행도가 줄지 않는다.
    private static void OnGoogleConflict(JObject device, JObject google)
    {
        LlPlatformRunner.RunWhenUiReady(() =>
        {
            var popup = GlobalUICanvasView.Instance.PopupPresenter;
            string message = "이 기기와 구글 계정에 서로 다른 진행 상황이 있습니다.\n"
                + $"이 기기: {Summary(device)}\n구글 계정: {Summary(google)}\n"
                + "<size=80%>이 기기의 진행 상황은 선택한 쪽에 합쳐집니다.</size>";
            popup.ShowPopup("이어서 할 계정 선택", message,
                ("구글 계정", () => { popup.HidePopup(); KoryGoogleAccount.Instance?.Choose(KoryGoogleAccount.ChooseGoogle); }),
                ("이 기기", () => { popup.HidePopup(); KoryGoogleAccount.Instance?.Choose(KoryGoogleAccount.ChooseDevice); }));
        });
    }

    private static string Summary(JObject summary)
    {
        if (summary == null) return "진행 없음";
        int cleared = summary.Value<int?>("stagesCleared") ?? 0;
        int stars = summary.Value<int?>("stars") ?? 0;
        return $"{cleared}스테이지 클리어 · 별 {stars}개";
    }

    private static void ShowPopup(string title, string message)
    {
        LlPlatformRunner.RunWhenUiReady(() =>
        {
            var popup = GlobalUICanvasView.Instance.PopupPresenter;
            popup.ShowPopup(title, message, ("확인", () => popup.HidePopup()));
        });
    }
}

/// <summary>
/// 최초 실행 시 UI가 준비되면 데이터 수집·이용 안내(Prominent Disclosure)를 띄워 동의를 받는다.
/// 동의 전까지 KoryPlatform / KorySession / KoryAnalyticsPump는 시작되지 않는다.
/// </summary>
public sealed class LlConsentGate : MonoBehaviour
{
    private bool _shown;

    private void Update()
    {
        if (_shown) return;
        if (!GlobalUICanvasView.HasInstance || GlobalUICanvasView.Instance.PopupPresenter == null) return;
        if (GlobalUICanvasView.Instance.IsPopupShown() || SceneTransitionManager.IsBusy) return;

        _shown = true;
        ShowDisclosurePopup(
            () =>
            {
                LlPlatform.GrantConsentAndBoot();
                if (this != null) Destroy(gameObject);
            },
            () =>
            {
                LlPlatform.DeclineConsent();
                if (this != null) Destroy(gameObject);
            });
    }

    public static void ShowDisclosurePopup(Action onAccept, Action onDecline)
    {
        if (!GlobalUICanvasView.HasInstance || GlobalUICanvasView.Instance.PopupPresenter == null) return;
        var popup = GlobalUICanvasView.Instance.PopupPresenter;
        const string message =
            "LAND LINKER(Android)는 클라우드 저장, 고객 지원, 광고 제공 및 성능·플레이 분석을 위해 아래 정보를 수집·전송합니다.\n\n"
            + "• 계정·진행: 게스트 ID, Play 게임즈 ID, 스테이지 클리어 및 별 기록\n"
            + "• 진단·플레이: 앱 버전, 기기 모델·OS·화면 주사율·메모리, 프레임 속도(FPS), 스테이지·광고 기록\n"
            + "• 광고: Google AdMob 광고 ID\n\n"
            + "<size=85%>동의하지 않으면 오프라인 모드로 실행되며 서버 전송과 광고 기능이 꺼집니다.</size>";
        popup.ShowPopup(
            "데이터 수집 및 이용 안내",
            message,
            ("동의하고 시작", () =>
            {
                popup.HidePopup();
                onAccept?.Invoke();
            }),
            ("개인정보 처리방침", () => LandLinkerServices.OpenPrivacyPolicy()),
            ("오프라인 (동의 안 함)", () =>
            {
                popup.HidePopup();
                onDecline?.Invoke();
            })
        );
    }
}

/// <summary>
/// 플랫폼 호스트 오브젝트에 붙어 시작 시 게스트 계정을 만들고, UI가 준비되면 대기 중인 팝업을 띄운다.
/// </summary>
public sealed class LlPlatformRunner : MonoBehaviour
{
    private static LlPlatformRunner s_instance;
    private static readonly System.Collections.Generic.Queue<Action> s_uiQueue = new System.Collections.Generic.Queue<Action>();

    public static void RunWhenUiReady(Action action)
    {
        s_uiQueue.Enqueue(action);
    }

    private void Awake()
    {
        s_instance = this;
    }

    private IEnumerator Start()
    {
        // SDK는 스스로 계정을 만들지 않는다. 구글 복구/기존 계정 접속이 끝난 뒤에도 계정이 없으면 게스트로 시작한다.
        while (!KorySession.StartupDone)
        {
            yield return null;
        }
        if (!KorySession.HasAccount)
        {
            _ = KorySession.StartGuest();
        }
    }

    private void Update()
    {
        if (s_uiQueue.Count == 0) return;
        if (!GlobalUICanvasView.HasInstance || GlobalUICanvasView.Instance.PopupPresenter == null) return;
        if (GlobalUICanvasView.Instance.IsPopupShown() || SceneTransitionManager.IsBusy) return;
        s_uiQueue.Dequeue()?.Invoke();
    }

    private void OnDestroy()
    {
        if (s_instance == this) s_instance = null;
    }
}
#endif
