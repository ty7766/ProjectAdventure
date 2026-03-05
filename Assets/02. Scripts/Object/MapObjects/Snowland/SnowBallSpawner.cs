using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;

public class SnowBallSpawner : SpawnedObjectManager<SnowBall>
{
    [Header("오브젝트 풀링 설정")]
    [SerializeField]
    private PoolObjectType _snowBallType = PoolObjectType.SnowBall;

    [Header("구역 설정")]
    [SerializeField, Tooltip("눈덩이가 생성될 랜덤 범위")]
    private BoxCollider _spawnArea;
    [SerializeField, Tooltip("눈덩이 생성 간격 (초)")]
    private float _spawnInterval = 3.0f;

    [Header("물리 설정")]
    [SerializeField, Tooltip("-x 방향으로 굴러가도록 설정")]
    private Vector3 _initialForce = new Vector3(-2f, 0f, 0f);

    private WaitForSeconds _snowBallSpawnInterval;

    private void Awake()
    {
        Assert.IsNotNull(_spawnArea, $"[{gameObject.name}] 스포너에 _spawnArea가 할당되지 않았습니다! 인스펙터를 확인하세요.");
        _snowBallSpawnInterval = new WaitForSeconds(_spawnInterval);
    }

    protected override IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return _snowBallSpawnInterval;
            SpawnSnowBallRandomArea();
        }
    }
    protected override void ReturnObjectToPool(SnowBall snowBallObject)
    {
        if(snowBallObject == null)
        {
            return;
        }
        UnregisterObject(snowBallObject);
        snowBallObject.ReturnToPool();
    }

    private void SpawnSnowBallRandomArea()
    {
        Vector3 randomPos = CalculateRandomSpawnPoint();
        GameObject snowBall = ObjectPoolManager.Instance.SpawnObject(_snowBallType, randomPos, Quaternion.identity);

        if (snowBall == null)
        {
            return;
        }

        if (snowBall.TryGetComponent<SnowBall>(out var ballScript))
        {
            RegisterObject(ballScript);
            ApplyForceForSnowBall(ballScript);
        }
    }

    private Vector3 CalculateRandomSpawnPoint()
    {
        Vector3 center = _spawnArea.center;
        Vector3 size = _spawnArea.size;

        Vector3 randomLocalPos = new Vector3(
            Random.Range(center.x - size.x / 2, center.x + size.x / 2),
            Random.Range(center.y - size.y / 2, center.y + size.y / 2),
            Random.Range(center.z - size.z / 2, center.z + size.z / 2)
        );
        return _spawnArea.transform.TransformPoint(randomLocalPos);
    }

    private void ApplyForceForSnowBall(SnowBall snowBall)
    {
        Rigidbody rigidbody = snowBall.GetComponent<Rigidbody>();
        rigidbody.AddForce(_initialForce, ForceMode.Impulse);
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