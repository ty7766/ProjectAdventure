using UnityEngine;

/// <summary>
/// 기기 화면 재생률(Refresh Rate)에 맞춰 목표 프레임레이트를 설정하는 싱글톤입니다.
///
/// - 일반 기기: 60프레임 고정
/// - 고주사율 기기(90/120Hz 등): 기기가 지원하는 최대 재생률에 맞춤
///
/// Android에서는 PlayerSettings.Android.optimizedFramePacing(ProjectAdventureBuild.cs에서 활성화)과
/// 함께 동작하며, Application.targetFrameRate 설정 시 Unity가 내부적으로 Choreographer 및
/// Surface.setFrameRate를 통해 고주사율 디스플레이 모드 전환을 시스템에 요청합니다.
///
/// 씬에 배치하지 않아도 런타임에 자동 생성됩니다.
/// </summary>
public class FrameRateManager : Singleton<FrameRateManager>
{
    private const int DefaultTargetFPS = 60;

    public int CurrentTargetFPS { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void Bootstrap()
    {
        if (HasInstance) return;

        GameObject go = new GameObject(nameof(FrameRateManager));
        go.AddComponent<FrameRateManager>();
    }

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        ApplyTargetFrameRate();
    }

    /// <summary>
    /// 화면 모드가 바뀌거나 앱이 포그라운드로 돌아올 때 재생률이 바뀔 수 있으므로 재적용합니다.
    /// </summary>
    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            ApplyTargetFrameRate();
        }
    }

    /// <summary>
    /// 기기 재생률을 조사해 목표 프레임레이트를 결정하고 적용합니다.
    /// </summary>
    [ContextMenu("Apply Target Frame Rate")]
    public void ApplyTargetFrameRate()
    {
        // 에디터에서는 EditorFPSLimitter가 프레임레이트를 관리하므로 간섭하지 않음
#if UNITY_EDITOR
        return;
#else
        int targetFps = DetermineTargetFPS();
        CurrentTargetFPS = targetFps;

        // 페이싱은 전적으로 targetFrameRate가 담당 (모바일에서 vSyncCount는 무시됨)
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFps;

        CustomDebug.Log($"[FrameRateManager] 목표 프레임레이트 적용: {targetFps} FPS (기기 재생률: {GetDeviceRefreshRate():0.#}Hz)");
#endif
    }

    /// <summary>
    /// 지원 해상도 목록에서 확인 가능한 최대 재생률을 반환합니다.
    /// 해상도 목록에 재생률 정보가 없으면 현재 재생률을 사용합니다.
    /// </summary>
    private static float GetDeviceRefreshRate()
    {
        float maxHz = 0f;

        foreach (Resolution resolution in Screen.resolutions)
        {
            float hz = (float)resolution.refreshRateRatio.value;
            if (!float.IsNaN(hz) && !float.IsInfinity(hz) && hz > maxHz)
            {
                maxHz = hz;
            }
        }

        if (maxHz < 1f)
        {
            float currentHz = (float)Screen.currentResolution.refreshRateRatio.value;
            if (!float.IsNaN(currentHz) && !float.IsInfinity(currentHz) && currentHz > 1f)
            {
                maxHz = currentHz;
            }
        }

        return maxHz;
    }

    private static int DetermineTargetFPS()
    {
        float displayHz = GetDeviceRefreshRate();

        // 재생률을 알 수 없는 기기(0 또는 비정상 값): 60프레임 기본값
        if (displayHz < 1f)
            return DefaultTargetFPS;

        // 60Hz 이하 기기: 60 프레임 고정 (기기 재생률 이하로는 의미가 없음)
        if (displayHz <= DefaultTargetFPS + 0.1f)
            return DefaultTargetFPS;

        // 고주사율 기기: 기기 최대 재생률에 맞춤 (59.94Hz 같은 비정수 재생률은 내림 처리)
        return Mathf.FloorToInt(displayHz);
    }
}
