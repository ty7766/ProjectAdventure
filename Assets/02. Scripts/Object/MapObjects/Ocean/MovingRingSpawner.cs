using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingRingSpawner : MonoBehaviour
{
    public enum RingDirection { Left_MinusX, Right_PlusX }

    [Header("링 오브젝트")]
    [SerializeField]
    private GameObject _movingRingPrefab;

    [Header("생성 설정")]
    [SerializeField]
    private PoolObjectType _objectType = PoolObjectType.MovingRing;

    [SerializeField, Tooltip("생성 간격 (최소 ~ 최대 랜덤)")]
    private float _minInterval = 2.0f;
    [SerializeField]
    private float _maxInterval = 4.0f;

    [Header("이동 설정 (-X 방향)")]
    [SerializeField, Tooltip("링이 이동할 거리")]
    private float _moveDistance = 30.0f;
    [SerializeField, Tooltip("이동 방향 선택")]
    private RingDirection _direction = RingDirection.Left_MinusX;

    [SerializeField, Tooltip("링 이동 속도")]
    private float _tileSpeed = 5.0f;

    //생성된 링들 추적용 리스트
    private List<MovingRing> _spawnedRings = new List<MovingRing>();

    private void Start()
    {
        StartCoroutine(MovingRingSpawnRoutine());
    }


    //맵이 바뀌게 되던 기존에 생성된 링들 제거
    private void OnDisable()
    {
        StopAllCoroutines();

        foreach(var ring in _spawnedRings)
        {
            if (ring != null && ring.gameObject.activeInHierarchy)
            {
                ring.ReturnToPool();
            }
        }

        _spawnedRings.Clear();
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
        GameObject ringObject = ObjectPoolManager.Instance.SpawnObject(_objectType, transform.position, transform.rotation);

        if (ringObject != null && ringObject.TryGetComponent<MovingRing>(out var ringScript))
        {
            for (int i = _spawnedRings.Count - 1; i >= 0; i--)
            {
                if (_spawnedRings[i] == null || !_spawnedRings[i].gameObject.activeInHierarchy)
                {
                    _spawnedRings.RemoveAt(i);
                }
            }

            _spawnedRings.Add(ringScript);

            Vector3 direction = (_direction == RingDirection.Left_MinusX) ? Vector3.left : Vector3.right;
            ringScript.InitializeForRingAttributs(_tileSpeed, _moveDistance, direction);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if(_movingRingPrefab == null)
        {
            return;
        }

        BoxCollider box = _movingRingPrefab.GetComponent<BoxCollider>();
        if (box == null) return;

        Vector3 tileScale = _movingRingPrefab.transform.localScale;
        Vector3 size = Vector3.Scale(box.size, tileScale);

        Vector3 dirVector = (_direction == RingDirection.Left_MinusX) ? Vector3.left : Vector3.right;

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