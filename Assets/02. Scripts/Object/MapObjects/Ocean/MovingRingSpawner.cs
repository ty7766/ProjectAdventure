using System.Collections;
using UnityEngine;

public class TileSpawner : MonoBehaviour
{
    [Header("링 오브젝트")]
    [SerializeField]
    private GameObject _movingRingPrefab;

    [Header("생성 설정")]
    [SerializeField]
    private PoolObjectType _tileType = PoolObjectType.MovingRing;

    [SerializeField, Tooltip("생성 간격 (최소 ~ 최대 랜덤)")]
    private float _minInterval = 2.0f;
    [SerializeField]
    private float _maxInterval = 4.0f;

    [Header("이동 설정 (-X 방향)")]
    [SerializeField, Tooltip("링이 이동할 거리")]
    private float _moveDistance = 30.0f;

    [SerializeField, Tooltip("링 이동 속도")]
    private float _tileSpeed = 5.0f;

    private void Start()
    {
        StartCoroutine(MovingRingSpawnRoutine());
    }

    private IEnumerator MovingRingSpawnRoutine()
    {
        while (true)
        {
            float waitTime = Random.Range(_minInterval, _maxInterval);
            yield return new WaitForSeconds(waitTime);
            SpawnTile();
        }
    }

    private void SpawnTile()
    {
        if (ObjectPoolManager.Instance == null)
        {
            return;
        }

        // 위치와 회전값은 스포너 기준
        GameObject ringObject = ObjectPoolManager.Instance.SpawnObject(_tileType, transform.position, transform.rotation);

        if (ringObject != null && ringObject.TryGetComponent<MovingRing>(out var tileScript))
        {
            tileScript.InitializeForRingAttributs(_tileSpeed, _moveDistance);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // 타일의 박스 콜라이더 크기 가져오기
        BoxCollider box = _movingRingPrefab.GetComponent<BoxCollider>();
        if (box == null) return;

        // 1. 타일 자체의 크기
        Vector3 tileScale = _movingRingPrefab.transform.localScale;
        Vector3 size = Vector3.Scale(box.size, tileScale);

        // 2. 이동 경로 그리기
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);

        Vector3 pathCenter = transform.position + (Vector3.left * _moveDistance * 0.5f);
        Vector3 pathSize = new Vector3(_moveDistance, size.y, size.z);

        Gizmos.DrawCube(pathCenter, pathSize);
        Gizmos.DrawWireCube(pathCenter, pathSize);

        // 3. 시작점과 끝점 명확히 표시
        Gizmos.color = Color.yellow; // 시작점
        Gizmos.DrawWireCube(transform.position, size);

        Gizmos.color = Color.red; // 끝점
        Vector3 endPos = transform.position + (Vector3.left * _moveDistance);
        Gizmos.DrawWireCube(endPos, size);
    }
#endif
}