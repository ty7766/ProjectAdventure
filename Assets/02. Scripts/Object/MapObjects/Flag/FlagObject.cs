using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class FlagObject : SpecialObject
{
    [Header("Components")]
    [SerializeField] private StageManager _stageManager;

    private void Start()
    {
        FindStageManagerWhenIsNull();
    }

    // 다시하기는 미리 생성된 스테이지 인스턴스를 껐다 켜서 재사용하므로, 깃발을 파괴하지 않고 콜라이더만 끈다.
    // (기본 SpecialObject 동작인 Destroy를 쓰면 다시하기 후 깃발이 사라진다)
    private void OnEnable()
    {
        if (_col != null)
        {
            _col.enabled = true;
        }
    }

    protected override void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }
        _col.enabled = false;
        ApplyEffect(other.gameObject);
    }

    protected override void ApplyEffect(GameObject player)
    {
        StageClear();
    }

    //--- Private Methods ---//
    private void StageClear()
    {
        if (_stageManager == null)
        {
            FindStageManagerWhenIsNull();
        }

        if (_stageManager != null)
        {
            _stageManager.StageClear();
        }
        else
        {
            CustomDebug.LogWarning("StageManager reference is missing in FlagObject.");
        }
    }

    //인스펙터 할당이 불가능 할 경우 (맵 매니저에 의한 동적 생성 등) 씬에서 찾아 할당
    private void FindStageManagerWhenIsNull()
    {
        if (_stageManager != null)
        {
            return;
        }

        _stageManager = FindAnyObjectByType<StageManager>();
        if (_stageManager == null)
        {
            CustomDebug.LogError("StageManager not found in the scene for FlagObject.", this);
        }
    }
}
