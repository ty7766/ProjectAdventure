using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GemObject : SpecialObject
{
    [Header("식별자")]
    [SerializeField, Tooltip("\"스테이지 이름\"_Gem 형식으로 작성")]
    private string _gemID;

    [Header("Components")]
    [SerializeField] private StageManager _stageManager;

    private void Start()
    {
        FindStageManagerWhenIsNull();
        CheckCollectedGemInMap();
    }

    protected override void ApplyEffect(GameObject player)
    {
        CollectGem();
    }

    //--- Private Methods ---//
    private void CollectGem()
    {
        _stageManager.CollectGem(_gemID);
        gameObject.SetActive(false);
        VFXManager.Instance.PlayVFX(VFXType.CollectGem, transform.position, Quaternion.identity);
        SoundManager.Instance.PlaySFX(SoundType.SFX_GemCollect);
    }

    private void CheckCollectedGemInMap()
    {
        if (_stageManager != null && _stageManager.IsGemCollected(_gemID))
        {
            gameObject.SetActive(false);
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
