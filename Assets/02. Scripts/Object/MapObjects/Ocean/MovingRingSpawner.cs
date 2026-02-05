using System.Collections;
using UnityEngine;

public class TileSpawner : MonoBehaviour
{
    public enum TileDirection { Left_MinusX, Right_PlusX }

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
    [SerializeField, Tooltip("이동 방향 선택")]
    private TileDirection _direction = TileDirection.Left_MinusX;

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
            Vector3 direction = (_direction == TileDirection.Left_MinusX) ? Vector3.left : Vector3.right;
            tileScript.InitializeForRingAttributs(_tileSpeed, _moveDistance, direction);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        BoxCollider box = _movingRingPrefab.GetComponent<BoxCollider>();
        if (box == null) return;

        Vector3 tileScale = _movingRingPrefab.transform.localScale;
        Vector3 size = Vector3.Scale(box.size, tileScale);

        // [MODIFIED] 기즈모도 방향에 맞춰서 그리기
        Vector3 dirVector = (_direction == TileDirection.Left_MinusX) ? Vector3.left : Vector3.right;

        // 1. 경로 그리기 (하늘색 터널)
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Vector3 pathCenter = transform.position + (dirVector * _moveDistance * 0.5f);
        Vector3 pathSize = new Vector3(_moveDistance, size.y, size.z);
        Gizmos.DrawCube(pathCenter, pathSize);
        Gizmos.DrawWireCube(pathCenter, pathSize);

        // 2. 시작점 (노랑)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, size);

        // 3. 끝점 (빨강)
        Gizmos.color = Color.red;
        Vector3 endPos = transform.position + (dirVector * _moveDistance);
        Gizmos.DrawWireCube(endPos, size);
    }
#endif
}