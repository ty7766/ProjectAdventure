using UnityEngine;

public class SnowBallSpawner : MonoBehaviour
{
    [Header("구역 설정")]
    [SerializeField, Tooltip("눈덩이가 생성될 랜덤 범위")]
    private BoxCollider _spawnArea;

    [SerializeField, Tooltip("플레이어가 밟으면 함정이 발동되는 트리거")]
    private BoxCollider _triggerArea;

    [Header("생성 설정")]
    [SerializeField]
    private PoolObjectType _snowBallType = PoolObjectType.SnowBall;

    [Header("물리 설정")]
    [SerializeField, Tooltip("-x 방향으로 굴러가도록 설정")]
    private Vector3 _initialForce = new Vector3(-2f, 0f, 0f);

    [Header("옵션")]
    [SerializeField]
    private bool _isOneTimeUse = true;

    private bool _isActivated = false;

    private void Awake()
    {
        if (_triggerArea == null)
        {
            _triggerArea = GetComponent<BoxCollider>();
        }

        if (_triggerArea != null)
        {
            _triggerArea.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isActivated) return;

        if (other.CompareTag("Player"))
        {
            SpawnRandomSnowBall();

            if (_isOneTimeUse)
            {
                _isActivated = true;
                if (_triggerArea != null) _triggerArea.enabled = false;
            }
        }
    }

    //--- Private Methods ---//
    private void SpawnRandomSnowBall()
    {
        if (ObjectPoolManager.Instance == null || _spawnArea == null)
        {
            CustomDebug.LogError($"[SnowBallSpawner] 설정 오류: ObjectPoolManager가 없거나 SpawnArea가 연결되지 않음.", this);
            return;
        }

        Bounds bound = _spawnArea.bounds;
        Vector3 randomPos = CalculateRandomSpawnPoint(bound);

        GameObject snowBall = ObjectPoolManager.Instance.SpawnObject(_snowBallType, randomPos, Quaternion.identity);
        ApplyForceForSnowBall(snowBall);

        CustomDebug.Log($"[SnowBallSpawner] 눈덩이 생성! 위치: {randomPos}, 힘: {_initialForce}");
    }

    private Vector3 CalculateRandomSpawnPoint(Bounds bound)
    {
        Vector3 randomPos = new Vector3(
            Random.Range(bound.min.x, bound.max.x),
            Random.Range(bound.min.y, bound.max.y),
            Random.Range(bound.min.z, bound.max.z)
        );

        return randomPos;
    }

    private void ApplyForceForSnowBall(GameObject snowBall)
    {
        if (snowBall != null && snowBall.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(_initialForce, ForceMode.Impulse);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_spawnArea != null)
        {
            // 반투명 초록색으로 스폰 구역 표시
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);

            Gizmos.matrix = _spawnArea.transform.localToWorldMatrix;
            Gizmos.DrawCube(_spawnArea.center, _spawnArea.size);
            Gizmos.DrawWireCube(_spawnArea.center, _spawnArea.size);
            Gizmos.matrix = Matrix4x4.identity; // 매트릭스 복구
        }
    }
#endif
}