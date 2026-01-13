using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;

public class FallingRockSpawner : MonoBehaviour
{
    [Header("화산 설정")]
    [SerializeField]
    private GameObject _rockPrefab;
    [SerializeField]
    private Transform _firePoint;

    [Header("발사 옵션")]
    [SerializeField]
    private float _spawnInterval = 3.0f; // 발사 주기
    [SerializeField]
    private float _launchSpeed = 15f;    // 발사 속도
    [SerializeField]
    private float _spread = 0.3f;        // 탄퍼짐 정도

    [Header("기즈모 설정 (예측)")]
    [Tooltip("화산의 높이보다 얼마나 아래로 떨어질지 예측 (보통 화산 높이만큼 입력)")]
    [SerializeField]
    private float _fallHeight = 10f;

    private void Awake()
    {
        Assert.IsNotNull(_rockPrefab, $"[FallingRockSpawner] '{name}'에 Rock Prefab이 할당되지 않았습니다.");

        if (_firePoint == null)
        {
            _firePoint = transform;
            CustomDebug.LogWarning($"[FallingRockSpawner] '{name}'에 Fire Point가 없어 자신을 발사 지점으로 설정합니다.");
        }
    }

    private void Start()
    {
        StartCoroutine(SpawnLoop());
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        DrawTrajectoryGizmos();
    }
#endif

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            SpawnAndLaunchRock();
            yield return new WaitForSeconds(_spawnInterval);
        }
    }

    //--- Private Methods ---//
    private void SpawnAndLaunchRock()
    {
        if (_rockPrefab == null)
        {
            return;
        }

        // 1. 생성
        GameObject rockInstance = Instantiate(_rockPrefab, _firePoint.position, Random.rotation);

        // 2. 발사 로직
        Rigidbody rockRigidbody = rockInstance.GetComponent<Rigidbody>();
        if (rockRigidbody != null)
        {
            Vector3 randomSpreadDirection = Random.insideUnitSphere * _spread;
            randomSpreadDirection.y = 0;

            Vector3 launchDirection = (transform.up + randomSpreadDirection).normalized;

            rockRigidbody.AddForce(launchDirection * _launchSpeed, ForceMode.VelocityChange);
        }
        else
        {
            CustomDebug.LogError($"[FallingRockSpawner] 프리팹 '{_rockPrefab.name}'에 Rigidbody가 없습니다!");
        }
    }

#if UNITY_EDITOR
    private void DrawTrajectoryGizmos()
    {
        Transform startPoint = (_firePoint != null) ? _firePoint : transform;

        Vector3 initialVelocity = startPoint.up * _launchSpeed;
        float gravity = Mathf.Abs(Physics.gravity.y);

        float timeToImpact = (initialVelocity.y + Mathf.Sqrt(initialVelocity.y * initialVelocity.y + 2 * gravity * _fallHeight)) / gravity;

        // 착탄 지점 계산
        Vector3 landingPosition = startPoint.position + (initialVelocity * timeToImpact);
        landingPosition.y = startPoint.position.y - _fallHeight; // 높이는 바닥으로 고정

        // 1. 예상 착탄 지점 그리기 (구)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(landingPosition, 1f);

        // 2. 탄퍼짐 범위(원) 그리기
        float distance = Vector3.Distance(startPoint.position, landingPosition);
        float impactRadius = distance * _spread;

        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);

        UnityEditor.Handles.color = Color.red;
        UnityEditor.Handles.DrawWireDisc(landingPosition, Vector3.up, impactRadius);

        // 3. 시각적 가이드 라인 (직선 거리)
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(startPoint.position, landingPosition);
    }
#endif
}