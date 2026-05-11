#if UNITY_EDITOR
using UnityEngine;

public class DevStageLoader : MonoBehaviour
{
    [Header("필수 레퍼런스")]
    [SerializeField] private GameObject _stagePrefab;
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private PlayerProperties _playerProperties;
    [SerializeField] private CameraFollow _cameraFollow;

    [Header("이펙트 (선택)")]
    [SerializeField] private MapChangeEffect _mapChangeEffect;

    [Header("HUD (선택)")]
    [SerializeField] private HUDStatusPresenter _hudStatusPresenter;
    [SerializeField] private HUDFlowPresenter _hudFlowPresenter;
    [SerializeField] private HUDSystemPresenter _hudSystemPresenter;

    private void Start()
    {
        if (_stagePrefab == null)
        {
            Debug.LogError("[DevStageLoader] Stage Prefab이 할당되지 않았습니다.");
            return;
        }
        if (_playerController == null)
        {
            Debug.LogError("[DevStageLoader] PlayerController가 할당되지 않았습니다.");
            return;
        }

        GameObject instance = Instantiate(_stagePrefab);
        StageManager stageManager = instance.GetComponentInChildren<StageManager>(true);

        if (stageManager == null)
        {
            Debug.LogError("[DevStageLoader] 스테이지 프리팹 안에 StageManager가 없습니다.");
            return;
        }

        stageManager.SetPlayerController(_playerController);

        var mapManager = instance.GetComponentInChildren<MapManager>(true);
        if(mapManager != null)
        {
            stageManager.SetMapManager(mapManager);
            mapManager.SetMapChangeEffect(_mapChangeEffect);
            mapManager.ResetState();
        }

        // HUD 먼저 구독 (InitializeStage의 이벤트를 놓치지 않기 위해)
        _hudStatusPresenter?.Setup(stageManager);

        stageManager.InitializeStage();

        _cameraFollow?.SnapToTarget();

        _hudFlowPresenter?.Setup(stageManager, _playerProperties);
        _hudSystemPresenter?.Setup(stageManager);
    }
}
#endif
