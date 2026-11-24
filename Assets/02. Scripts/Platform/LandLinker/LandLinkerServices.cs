using System;
using UnityEngine;

/// <summary>
/// 게임 코드가 부르는 코리게임즈 플랫폼 기능(클라우드 세이브, 구글 계정, 고객 문의, 계정 삭제, 법적 문서, 전면 광고, 분석)의 단일 입구.
/// Android 빌드에서만 KORYGAMES 플랫폼 SDK에 연결되고, PC 등 다른 타겟에서는 아무 일도 하지 않는다
/// (구현은 LandLinker/Android 폴더의 #if UNITY_ANDROID 코드). 게임 코드에는 플랫폼 분기를 두지 않는다.
/// </summary>
public static class LandLinkerServices
{
    public const string PrivacyPolicyUrl = "https://games.codingbot.kr/land-linker/privacy/";
    public const string TermsOfServiceUrl = "https://games.codingbot.kr/land-linker/terms/";
    public const string AccountDeletionWebUrl = "https://games.codingbot.kr/land-linker/privacy/#delete-account";

    /// <summary>온라인/구글 계정 상태가 바뀌었을 때 (옵션 UI 갱신용)</summary>
    public static event Action StateChanged;

    public static bool IsEnabled
    {
        get
        {
#if UNITY_ANDROID
            return LlPlatform.IsRunning;
#else
            return false;
#endif
        }
    }

    public static bool CanOptInOnThisPlatform
    {
        get
        {
#if UNITY_ANDROID
            return LlPlatform.CanOptIn;
#else
            return false;
#endif
        }
    }

    public static void RequestConsentAndEnable()
    {
#if UNITY_ANDROID
        LlPlatform.RequestConsentFromOptions();
#endif
    }

    public static bool IsOnline
    {
        get
        {
#if UNITY_ANDROID
            return LlPlatform.IsRunning && Kory.Platform.KorySession.Online;
#else
            return false;
#endif
        }
    }

    public static string PlayerId
    {
        get
        {
#if UNITY_ANDROID
            return LlPlatform.PlayerId ?? "";
#else
            return "";
#endif
        }
    }

    public static bool GoogleAvailable
    {
        get
        {
#if UNITY_ANDROID
            return LlPlatform.IsRunning && Kory.Platform.KoryGoogleAccount.Instance != null && Kory.Platform.KoryGoogleAccount.Instance.Available;
#else
            return false;
#endif
        }
    }

    public static bool GoogleSignedIn
    {
        get
        {
#if UNITY_ANDROID
            return GoogleAvailable && Kory.Platform.KoryGoogleAccount.Instance.SignedIn;
#else
            return false;
#endif
        }
    }

    public static bool GoogleLinked
    {
        get
        {
#if UNITY_ANDROID
            return LlPlatform.GoogleLinked;
#else
            return false;
#endif
        }
    }

    public static string GoogleDisplayName
    {
        get
        {
#if UNITY_ANDROID
            return GoogleSignedIn ? Kory.Platform.KoryGoogleAccount.Instance.DisplayName : "";
#else
            return "";
#endif
        }
    }

    public static bool PrivacyOptionsRequired
    {
        get
        {
#if UNITY_ANDROID
            return LlPlatform.PrivacyOptionsRequired;
#else
            return false;
#endif
        }
    }

    /// <summary>구글 계정 로그인(연동). 결과 문구는 게임이 보여준다. (null = 성공)</summary>
    public static void SignInGoogle(Action<string> done)
    {
#if UNITY_ANDROID
        LlPlatform.SignInGoogle(done);
#else
        done?.Invoke("unavailable");
#endif
    }

    /// <summary>서버 및 기기 계정 데이터 영구 삭제 (Google Play 계정 삭제 정책 준수)</summary>
    public static void DeleteAccount(Action<bool, string> done)
    {
#if UNITY_ANDROID
        LlPlatform.DeleteAccount(done);
#else
        done?.Invoke(false, "unavailable");
#endif
    }

    /// <summary>인게임 고객 문의 제출 (POST /v1/support/tickets)</summary>
    public static void SubmitSupportTicket(string category, string body, string email, Action<bool, string, string, string> done)
    {
#if UNITY_ANDROID
        LlPlatform.SubmitSupportTicket(category, body, email, done);
#else
        done?.Invoke(false, null, null, "unavailable");
#endif
    }

    public static void ShowPrivacyOptions(Action done)
    {
#if UNITY_ANDROID
        LlPlatform.ShowPrivacyOptions(done);
#else
        done?.Invoke();
#endif
    }

    public static void OpenPrivacyPolicy() => Application.OpenURL(PrivacyPolicyUrl);

    public static void OpenTermsOfService() => Application.OpenURL(TermsOfServiceUrl);

    /// <summary>스테이지 시작 (분석)</summary>
    public static void StageStarted(int stage)
    {
#if UNITY_ANDROID
        LlPlatform.Track("stage_start", ("stage", stage));
#endif
    }

    /// <summary>한 판이 끝났을 때 (클리어/실패). 분석 기록 + 전면 광고 판정.</summary>
    public static void RunEnded(int stage, bool cleared, int stars, float seconds)
    {
#if UNITY_ANDROID
        if (cleared)
        {
            LlPlatform.Track("stage_clear", ("stage", stage), ("stars", stars), ("seconds", (int)seconds));
        }
        else
        {
            LlPlatform.Track("stage_fail", ("stage", stage), ("seconds", (int)seconds));
        }
        LlInterstitialAds.OnRunEnded(stage, seconds);
#endif
    }

    /// <summary>
    /// 결과 화면 등에서 다음 행동(다시하기/다음 스테이지/타이틀)으로 넘어가기 직전에 부른다.
    /// 판정된 전면 광고가 있으면 광고를 보여준 뒤, 없으면 즉시 proceed를 호출한다.
    /// </summary>
    public static void BeforeLeavingRun(Action proceed)
    {
#if UNITY_ANDROID
        LlInterstitialAds.ShowPendingThen(proceed);
#else
        proceed?.Invoke();
#endif
    }

    internal static void RaiseStateChanged() => StateChanged?.Invoke();
}
