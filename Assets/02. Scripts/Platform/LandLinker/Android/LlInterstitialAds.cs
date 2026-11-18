#if UNITY_ANDROID
using System;
using Kory.Platform;
using UnityEngine;
#if !UNITY_EDITOR
using GoogleMobileAds.Api;
#endif

/// <summary>
/// 판 사이 전면 광고. 판이 끝날 때 RBD 공식(InterstitialPolicy)으로 노출 여부를 정하고,
/// 플레이어가 결과 화면에서 다음 행동을 고를 때 광고를 보여준 뒤 그 행동을 이어서 실행한다.
/// 광고 동의(UMP)와 MobileAds 초기화는 SDK(KoryAdMobAds)가 하고, 여기서는 전면 광고만 불러온다.
/// </summary>
public static class LlInterstitialAds
{
    private const string CountedRunsKey = "ll.adCountedRuns";
    private const string RunsSinceAdKey = "ll.adRunsSinceAd";

    private static LandLinkerAdConfig s_config;
    private static bool s_pending;
    private static int s_pendingStage;
    private static LlInterstitialLoader s_loader;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        s_config = null;
        s_pending = false;
        s_loader = null;
    }

    public static void Attach(GameObject host)
    {
        s_config = LandLinkerAdConfig.Load();
        if (s_config == null)
        {
            Debug.LogWarning("[LandLinker] Resources/LandLinkerAdConfig 가 없어 전면 광고를 끕니다.");
            return;
        }
        s_loader = host.AddComponent<LlInterstitialLoader>();
        s_loader.Begin(s_config.InterstitialUnitId);
    }

    private static int EveryRuns => KoryRemoteConfig.Get("ads.interstitial.everyRuns", s_config.EveryRuns);
    private static float MinRunSeconds => KoryRemoteConfig.Get("ads.interstitial.minRunSeconds", s_config.MinRunSeconds);
    private static int GraceRuns => KoryRemoteConfig.Get("ads.interstitial.graceRuns", s_config.GraceRuns);

    /// <summary>
    /// RBD InterstitialPolicy.Roll과 같은 공식: 짧은 판은 세지 않고, 유예 판수가 지난 뒤
    /// 마지막 광고 이후 everyRuns판째에 광고. 광고를 못 띄우면 다음 판에 다시 시도한다.
    /// </summary>
    public static void OnRunEnded(int stage, float runSeconds)
    {
        if (s_config == null || TutorialManager.IsActive) return;
        if (runSeconds < MinRunSeconds) return;

        int counted = PlayerPrefs.GetInt(CountedRunsKey, 0) + 1;
        PlayerPrefs.SetInt(CountedRunsKey, counted);
        if (counted <= GraceRuns)
        {
            PlayerPrefs.Save();
            return;
        }

        int sinceAd = PlayerPrefs.GetInt(RunsSinceAdKey, 0) + 1;
        PlayerPrefs.SetInt(RunsSinceAdKey, sinceAd);
        PlayerPrefs.Save();
        if (sinceAd >= Math.Max(1, EveryRuns))
        {
            s_pending = true;
            s_pendingStage = stage;
        }
    }

    public static void ShowPendingThen(Action proceed)
    {
        if (!s_pending || s_loader == null || !s_loader.IsReady)
        {
            // 광고가 준비되지 않았으면 그대로 진행하고, 카운트는 문턱에 남아 다음 판에 다시 시도한다.
            s_pending = false;
            proceed?.Invoke();
            return;
        }

        s_pending = false;
        int stage = s_pendingStage;
        s_loader.Show(shown =>
        {
            if (shown)
            {
                PlayerPrefs.SetInt(RunsSinceAdKey, 0);
                PlayerPrefs.Save();
                LlPlatform.Track("ad_interstitial_shown", ("stage", stage));
            }
            proceed?.Invoke();
        });
    }
}

/// <summary>AdMob 전면 광고를 미리 불러 두고 보여준다. 에디터에서는 광고 없이 바로 넘어간다.</summary>
public sealed class LlInterstitialLoader : MonoBehaviour
{
#if UNITY_EDITOR
    public bool IsReady => false;
    public void Begin(string unitId) { }
    public void Show(Action<bool> finished) => finished?.Invoke(false);
#else
    private string _unitId;
    private InterstitialAd _ad;
    private float _retrySeconds = 5f;
    private bool _showing;
    private bool _initialized;

    public bool IsReady => _ad != null && _ad.CanShowAd() && !_showing;

    public void Begin(string unitId)
    {
        _unitId = unitId;
        KoryAdConsent.Changed -= TryInitialize;
        KoryAdConsent.Changed += TryInitialize; // SDK가 동의 절차를 끝내면 호출된다
        TryInitialize();
    }

    private void TryInitialize()
    {
        if (_initialized || this == null || !KoryAdConsent.CanRequestAds) return;
        _initialized = true;
        MobileAds.Initialize(_ => { if (this != null) Load(); });
    }

    private void Load()
    {
        bool completed = false;
        InterstitialAd.Load(_unitId, new AdRequest(), (ad, error) =>
        {
            if (completed) return;
            completed = true;
            if (this == null) { ad?.Destroy(); return; }
            if (error != null || ad == null)
            {
                ad?.Destroy();
                Invoke(nameof(Load), _retrySeconds);
                _retrySeconds = Mathf.Min(60f, _retrySeconds * 2f);
                return;
            }
            _retrySeconds = 5f;
            _ad = ad;
        });
    }

    public void Show(Action<bool> finished)
    {
        if (!IsReady) { finished?.Invoke(false); return; }
        _showing = true;
        bool done = false, opened = false;
        var ad = _ad;
        void Finish()
        {
            if (done) return;
            done = true;
            AudioListener.pause = false;
            _showing = false;
            ad.Destroy();
            _ad = null;
            Load();
            finished?.Invoke(opened);
        }
        ad.OnAdFullScreenContentOpened += () =>
        {
            if (done || opened) return;
            opened = true;
            AudioListener.pause = true;
        };
        ad.OnAdFullScreenContentClosed += Finish;
        ad.OnAdFullScreenContentFailed += _ => Finish();
        ad.Show();
    }

    private void OnDestroy()
    {
        KoryAdConsent.Changed -= TryInitialize;
        _ad?.Destroy();
        if (_showing) AudioListener.pause = false;
    }
#endif
}
#endif
