using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

[Serializable]
public class DOFSettings
{
    [Header("DOF Parameters")]
    [Tooltip("조리개 값 (f-number). 낮을수록 배경 흐림이 심해짐")]
    public float Aperture;

    [Tooltip("초점 거리 (Focus Distance). 카메라로부터 초점이 맞는 지점까지의 거리 (단위: m)")]
    public float FocusDistance;

    [Tooltip("초점 길이 (Focal Length). 렌즈의 망원/광각 정도 (단위: mm)")]
    public float FocalLength;

    public string Name; // 설정 이름 (예: "Stage", "Title")

    // 기본값 생성자
    public DOFSettings()
    {
        Aperture = 5.6f;
        FocusDistance = 10f;
        FocalLength = 50f;
        Name = "Default";
    }

    public DOFSettings(float aperture, float focusDistance, float focalLength, string name)
    {
        Aperture = aperture;
        FocusDistance = focusDistance;
        FocalLength = focalLength;
        Name = name;
    }

    public void ApplyTo(DepthOfField dof)
    {
        if (dof != null)
        {
            dof.aperture.Override(Aperture);
            dof.focusDistance.Override(FocusDistance);
            dof.focalLength.Override(FocalLength);
        }
    }
}

/// <summary>
/// 게임의 그래픽 설정 및 후처리를 관리하며, 모든 설정값의 저장 및 불러오기를 담당하는 싱글톤 클래스입니다.
/// </summary>
[RequireComponent(typeof(Volume))]
public class GraphicManager : Singleton<GraphicManager>
{
    private Volume _volume;
    public Volume Volume => _volume;

    // 포스트 프로세싱 볼륨 컴포넌트들
    private Bloom _bloom;
    private ChromaticAberration _ca;
    private DepthOfField _dof;
    private LiftGammaGain _gamma;

    [Header("URP Asset Reference")]
    public UniversalRenderPipelineAsset urpAsset;

    [Header("DOF Default Settings")]
    [SerializeField] private List<DOFSettings> _dofSettings;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        _volume = GetComponent<Volume>();
        InitPostProcessing();
        LoadSettings(); // 게임 시작 시 저장된 모든 설정 적용
    }

    private void OnEnable()
    {
        // 씬 로드 이벤트 구독
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // 씬 로드 이벤트 구독 해제
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬이 바뀔 때마다 파괴되고 새로 생성된 메인 카메라에 카메라 종속적인 설정(AA 등)을 다시 적용해줌
        if (PlayerPrefs.HasKey("AntiAliasing"))
        {
            SetAntiAliasing(PlayerPrefs.GetInt("AntiAliasing"));
        }
    }

    private void InitPostProcessing()
    {
        if (_volume.profile.TryGet<Bloom>(out var bloom)) _bloom = bloom;
        if (_volume.profile.TryGet<ChromaticAberration>(out var ca)) _ca = ca;
        if (_volume.profile.TryGet<DepthOfField>(out var dof)) _dof = dof;
        if (_volume.profile.TryGet<LiftGammaGain>(out var gamma)) _gamma = gamma;
    }

    #region 디스플레이 설정
    /// <summary> 해상도 및 화면 모드 설정 </summary>
    public void SetResolution(int width, int height, FullScreenMode mode)
    {
        Screen.SetResolution(width, height, mode);
        PlayerPrefs.SetInt("ResW", width);
        PlayerPrefs.SetInt("ResH", height);
        PlayerPrefs.SetInt("ScreenMode", (int)mode);
    }

    /// <summary> 수직 동기화 설정 (0: 끔, 1: 켬) </summary>
    public void SetVSync(bool isOn)
    {
        QualitySettings.vSyncCount = isOn ? 1 : 0;
        PlayerPrefs.SetInt("VSync", isOn ? 1 : 0);
    }

    /// <summary> 안티 에일리어싱 설정 (0: 끔, 1: SMAA, 2: TAA) </summary>
    public void SetAntiAliasing(int mode)
    {
        if (Camera.main != null && Camera.main.TryGetComponent<UniversalAdditionalCameraData>(out var cameraData))
        {
            switch (mode)
            {
                case 0:
                    cameraData.antialiasing = AntialiasingMode.None;
                    break;
                case 1:
                    cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                    cameraData.antialiasingQuality = AntialiasingQuality.High;
                    break;
                case 2:
                    cameraData.antialiasing = AntialiasingMode.TemporalAntiAliasing;
                    break;
            }
        }
        PlayerPrefs.SetInt("AntiAliasing", mode);
    }
    #endregion

    #region 그래픽 품질 설정
    /// <summary> 렌더링 스케일 설정 (60 ~ 200%) </summary>
    public void SetRenderScale(float percent)
    {
        if (urpAsset != null)
        {
            // 60% ~ 200% -> 0.6 ~ 2.0으로 변환
            float scale = Mathf.Clamp(percent / 100f, 0.6f, 2.0f);
            urpAsset.renderScale = scale;
        }
        PlayerPrefs.SetFloat("RenderScale", percent);
    }

    /// <summary> 텍스처 품질 설정 (0: 저, 1: 중, 2: 고) </summary>
    public void SetTextureQuality(int level)
    {
        // globalTextureMipmapLimit -> 0: Full, 1: Half, 2: Quarter
        // UI에서 0(저), 1(중), 2(고)로 들어온다고 가정하고 역순 적용
        int mipmapLimit = Mathf.Clamp(2 - level, 0, 2);
        QualitySettings.globalTextureMipmapLimit = mipmapLimit;
        PlayerPrefs.SetInt("TextureQuality", level);
    }

    /// <summary> 비등방성 필터링 수준 설정 (0: 끔, 2~16: 배율) </summary>
    public void SetAnisotropicFiltering(int level)
    {
        if (level > 0)
        {
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            Texture.SetGlobalAnisotropicFilteringLimits(level, 16);
        }
        else
        {
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
        }
        PlayerPrefs.SetInt("AnisoLevel", level);
    }

    /// <summary> 그림자 품질 설정 (Obsolete 대응 완료) </summary>
    public void SetShadowQuality(int level)
    {
        if (urpAsset != null)
        {
            switch (level)
            {
                case 0: // 저 (캐스케이드 1단계)
                    urpAsset.shadowCascadeCount = 1;
                    urpAsset.mainLightShadowmapResolution = 512;
                    urpAsset.shadowDistance = 20f;
                    break;
                case 1: // 중 (캐스케이드 2단계)
                    urpAsset.shadowCascadeCount = 2;
                    urpAsset.cascade2Split = 0.25f; // 2단계일 때 비율 설정
                    urpAsset.mainLightShadowmapResolution = 2048;
                    urpAsset.shadowDistance = 50f;
                    break;
                case 2: // 고 (캐스케이드 4단계)
                    urpAsset.shadowCascadeCount = 4;
                    urpAsset.cascade4Split = new Vector3(0.067f, 0.2f, 0.467f); // 4단계일 때 비율 설정
                    urpAsset.mainLightShadowmapResolution = 4096;
                    urpAsset.shadowDistance = 100f;
                    break;
            }
        }
        PlayerPrefs.SetInt("ShadowQuality", level);
    }
    #endregion

    #region 후처리 설정
    /// <summary> 밝기(감마) 설정 (-1.0 ~ 1.0 범위) </summary>
    public void SetBrightness(float value)
    {
        if (_gamma != null)
        {
            // Lift, Gamma, Gain 중 Gamma의 w값을 조절하여 밝기 제어
            value = Mathf.Clamp(value, -1f, 1f);
            _gamma.gamma.Override(new Vector4(1f, 1f, 1f, value));
        }
        PlayerPrefs.SetFloat("BrightnessGamma", value);
    }

    /// <summary> 블룸 효과 설정 </summary>
    public void SetBloom(bool isOn)
    {
        if (_bloom != null)
            _bloom.active = isOn;

        PlayerPrefs.SetInt("BloomOn", isOn ? 1 : 0);
    }

    /// <summary> 색수차(Chromatic Aberration) 설정 </summary>
    public void SetChromaticAberration(bool isOn)
    {
        if (_ca != null)
            _ca.active = isOn;

        PlayerPrefs.SetInt("CAOn", isOn ? 1 : 0);
    }

    public void SetDepthOfField(bool isOn)
    {
        if (_dof != null)
        {
            // 효과 전체를 활성화/비활성화 (가장 확실함)
            _dof.active = isOn;
        }
        PlayerPrefs.SetInt("DOFOn", isOn ? 1 : 0);
    }
    #endregion

    public void SetDoFMode(string modeName)
    {
        _dofSettings?.Find(_dof => _dof.Name == modeName)?.ApplyTo(_dof);
    }

    /// <summary>
    /// 저장된 모든 그래픽 설정값을 불러와 게임에 즉시 적용합니다.
    /// </summary>
    public void LoadSettings()
    {
        // 1. 디스플레이 & 해상도
        if (PlayerPrefs.HasKey("ResW") && PlayerPrefs.HasKey("ResH"))
        {
            int w = PlayerPrefs.GetInt("ResW");
            int h = PlayerPrefs.GetInt("ResH");
            FullScreenMode mode = (FullScreenMode)PlayerPrefs.GetInt("ScreenMode", (int)FullScreenMode.FullScreenWindow);
            SetResolution(w, h, mode);
        }

        // 2. 수직 동기화
        if (PlayerPrefs.HasKey("VSync"))
            SetVSync(PlayerPrefs.GetInt("VSync") == 1);

        // 3. 안티 에일리어싱
        if (PlayerPrefs.HasKey("AntiAliasing"))
            SetAntiAliasing(PlayerPrefs.GetInt("AntiAliasing"));

        // 4. 렌더링 스케일 (기본값 100%)
        if (PlayerPrefs.HasKey("RenderScale"))
            SetRenderScale(PlayerPrefs.GetFloat("RenderScale", 100f));

        // 5. 텍스처 품질 (기본값 고품질 2)
        if (PlayerPrefs.HasKey("TextureQuality"))
            SetTextureQuality(PlayerPrefs.GetInt("TextureQuality", 2));

        // 6. 비등방성 필터링 (기본값 끔 0)
        if (PlayerPrefs.HasKey("AnisoLevel"))
            SetAnisotropicFiltering(PlayerPrefs.GetInt("AnisoLevel", 0));

        // 7. 그림자 품질 (기본값 고품질 2)
        if (PlayerPrefs.HasKey("ShadowQuality"))
            SetShadowQuality(PlayerPrefs.GetInt("ShadowQuality", 2));

        // 8. 밝기 (감마) (기본값 0)
        if (PlayerPrefs.HasKey("BrightnessGamma"))
            SetBrightness(PlayerPrefs.GetFloat("BrightnessGamma", 0f));

        // 9. 블룸 (기본값 켬)
        if (PlayerPrefs.HasKey("BloomOn"))
            SetBloom(PlayerPrefs.GetInt("BloomOn", 1) == 1);

        // 10. CA (기본값 끔)
        if (PlayerPrefs.HasKey("CAOn"))
            SetChromaticAberration(PlayerPrefs.GetInt("CAOn", 0) == 1);

        // 11. DOF (기본값 켬)
        if (PlayerPrefs.HasKey("DOFOn"))
            SetDepthOfField(PlayerPrefs.GetInt("DOFOn", 1) == 1);

        // 설정 적용 후 데이터 강제 저장
        PlayerPrefs.Save();
    }
}