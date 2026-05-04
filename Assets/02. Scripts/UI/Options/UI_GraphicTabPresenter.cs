using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UI_GraphicTabPresenter
{
    private UI_GraphicTabView _view;
    private GraphicManager _model;
    private Resolution[] _resolutions;

    public UI_GraphicTabPresenter(UI_GraphicTabView view)
    {
        _view = view;
        _model = GraphicManager.Instance;
    }

    public void Initialize()
    {
        // 1. 해상도 드롭다운 구성 및 싱크 (수치 기반)
        SetupResolutionDropdown();

        // 2. 나머지 UI 요소들 싱크 (인덱스/플로트 기반)
        SyncViewWithModel();

        // 3. 이벤트 바인딩 (사용자 조작 대기)
        BindEvents();
    }

    private void SetupResolutionDropdown()
    {
        // 1. 기기가 지원하는 해상도 목록 가져오기 (중복 제거)
        _resolutions = Screen.resolutions
            .Select(res => new Resolution { width = res.width, height = res.height })
            .Distinct()
            .ToArray();

        List<string> options = new List<string>();

        // 2. 저장된 해상도 값 불러오기 (없으면 현재 화면 해상도)
        int savedW = PlayerPrefs.GetInt("ResW", Screen.width);
        int savedH = PlayerPrefs.GetInt("ResH", Screen.height);
        int targetIndex = 0;

        for (int i = 0; i < _resolutions.Length; i++)
        {
            string option = $"{_resolutions[i].width} x {_resolutions[i].height}";
            options.Add(option);

            // 3. 저장된 값과 일치하는 항목의 인덱스 기억
            if (_resolutions[i].width == savedW && _resolutions[i].height == savedH)
            {
                targetIndex = i;
            }
        }

        // 4. View에 리스트 전달 및 인덱스 설정
        _view.UpdateResolutionOptions(options);
        _view.ResolutionIndex = targetIndex;
    }

    private void SyncViewWithModel()
    {
        // 1. 디스플레이 설정 (해상도 인덱스는 SetupResolutionDropdown에서 처리했으므로 제외)
        _view.DisplayModeIndex = PlayerPrefs.GetInt("ScreenMode", (int)FullScreenMode.FullScreenWindow);
        _view.AAIndex = PlayerPrefs.GetInt("AntiAliasing", 0);
        _view.VSyncIndex = PlayerPrefs.GetInt("VSync", 0);
        _view.GammaValue = PlayerPrefs.GetFloat("BrightnessGamma", 0f);

        // 2. 그래픽 품질 설정
        _view.RenderScale = PlayerPrefs.GetFloat("RenderScale", 100f);
        _view.TextureQualityIndex = PlayerPrefs.GetInt("TextureQuality", 2);

        // 비등방성 필터링 배율 -> 인덱스 변환 적용
        int savedAniso = PlayerPrefs.GetInt("AnisoLevel", 0);
        _view.TextureFilteringIndex = GetIndexFromAnisoLevel(savedAniso);

        _view.ShadowQualityIndex = PlayerPrefs.GetInt("ShadowQuality", 2);

        // 3. 후처리 설정 (bool을 int 인덱스로 매핑)
        _view.BloomIndex = PlayerPrefs.GetInt("BloomOn", 1);
        _view.ChromaticAberrationIndex = PlayerPrefs.GetInt("CAOn", 0);
        _view.DepthOfFieldIndex = PlayerPrefs.GetInt("DOFOn", 1);
    }

    private void BindEvents()
    {
        _view.BindDisplaySettings(
                    index => _model.SetResolution(Screen.width, Screen.height, (FullScreenMode)index), // 모드 변경
                    index => {
                        // 해상도 드롭다운 선택 시 실제 적용
                        var res = _resolutions[index];
                        _model.SetResolution(res.width, res.height, (FullScreenMode)_view.DisplayModeIndex);
                    },
                    index => _model.SetAntiAliasing(index),
                    index => _model.SetVSync(index == 1),
                    val => _model.SetBrightness(val)
                );

        _view.BindQualitySettings(
                val => { }, // 렌더링 스케일은 드래그 종료 시에만 적용
                index => _model.SetTextureQuality(index), // 0:저(1/4), 1:중(1/2), 2:고(Full) 로 GraphicManager 내부에서 처리됨
                index => {
                    // 드롭다운 index를 실제 배율로 변환 (0, 2, 4, 8, 16)
                    int anisoLevel = GetAnisoLevelFromIndex(index);
                    _model.SetAnisotropicFiltering(anisoLevel);
                },
                index => _model.SetShadowQuality(index)
            );

        _view.BindRenderScaleDragEnd(val => _model.SetRenderScale(val));

        _view.BindPostProcessSettings(
            index => _model.SetBloom(index == 1),
            index => _model.SetChromaticAberration(index == 1),
            index => _model.SetDepthOfField(index == 1)
        );
    }

    /// <summary>
    /// 드롭다운 인덱스(0~4)를 실제 비등방성 필터링 배율로 변환
    /// </summary>
    private int GetAnisoLevelFromIndex(int index)
    {
        return index switch
        {
            0 => 0,
            1 => 2,
            2 => 4,
            3 => 8,
            4 => 16,
            _ => 0
        };
    }

    /// <summary>
    /// 저장된 배율을 다시 드롭다운 인덱스로 변환 (초기화용)
    /// </summary>
    private int GetIndexFromAnisoLevel(int level)
    {
        return level switch
        {
            0 => 0,
            2 => 1,
            4 => 2,
            8 => 3,
            16 => 4,
            _ => 0
        };
    }
}