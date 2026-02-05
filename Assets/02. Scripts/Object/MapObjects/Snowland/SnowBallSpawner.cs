using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnowBallSpawner : MonoBehaviour
{
    [Header("구역 설정")]
    [SerializeField, Tooltip("눈덩이가 생성될 랜덤 범위")]
    private BoxCollider _spawnArea;

    [Header("생성 설정")]
    [SerializeField]
    private PoolObjectType _snowBallType = PoolObjectType.SnowBall;

    [SerializeField, Tooltip("눈덩이 생성 간격 (초)")]
    private float _spawnInterval = 3.0f;

    [Header("물리 설정")]
    [SerializeField, Tooltip("-x 방향으로 굴러가도록 설정")]
    private Vector3 _initialForce = new Vector3(-2f, 0f, 0f);

    //생성된 스노우볼 추적용 리스트
    private List<SnowBall> _spawnedSnowBalls = new List<SnowBall>();

    private void Start()
    {
        StartCoroutine(ActivateSnowBall());
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        foreach (var ring in _spawnedSnowBalls)
        {
            if (ring != null && ring.gameObject.activeInHierarchy)
            {
                ring.ReturnToPool();
            }
        }

        _spawnedSnowBalls.Clear();
    }

    private IEnumerator ActivateSnowBall()
    {
        WaitForSeconds wait = new WaitForSeconds(_spawnInterval);

        while (true)
        {
            yield return wait;
            SpawnSnowBallRandomArea();
        }
    }
    private void SpawnSnowBallRandomArea()
    {
        if (ObjectPoolManager.Instance == null || _spawnArea == null)
        {
            return;
        }

        Vector3 randomPos = CalculateRandomSpawnPoint();
        GameObject snowBall = ObjectPoolManager.Instance.SpawnObject(_snowBallType, randomPos, Quaternion.identity);

        if (snowBall != null)
        {
            ApplyForceForSnowBall(snowBall);

            if (snowBall.TryGetComponent<SnowBall>(out var ballScript))
            {
                for (int i = _spawnedSnowBalls.Count - 1; i >= 0; i--)
                {
                    if (_spawnedSnowBalls[i] == null || !_spawnedSnowBalls[i].gameObject.activeInHierarchy)
                    {
                        _spawnedSnowBalls.RemoveAt(i);
                    }
                }

                _spawnedSnowBalls.Add(ballScript);
            }
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