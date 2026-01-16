using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class GemObject : SpecialObject
{
    [Header("Components")]
    [SerializeField] private StageManager _stageManager;

    private void Start()
    {
        FindStageManagerWhenIsNull();
    }

    protected override void ApplyEffect(GameObject player)
    {
        CollectGem();
    }

    //--- Private Methods ---//
    private void CollectGem()
    {
        if(_stageManager == null)
        {
            FindStageManagerWhenIsNull();
        }

        if (_stageManager != null)
        {
            _stageManager.CollectGem();
        }
        else
        {
            Debug.LogWarning("StageManager reference is missing in GemObject.");
        }
    }

    //인스펙터 할당이 불가능 할 경우 (맵 매니저에 의한 동적 생성 등) 씬에서 찾아 할당
    private void FindStageManagerWhenIsNull()
    {
        if(_stageManager != null)
        {
            return;
        }

        _stageManager = FindAnyObjectByType<StageManager>();
        if(_stageManager == null)
        {
            CustomDebug.LogError("StageManager not found in the scene for GemObject.", this);
        }
    }
}
