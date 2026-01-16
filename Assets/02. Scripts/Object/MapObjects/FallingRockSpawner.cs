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

    [Header("이펙트 설정")]
    [Tooltip("설정한 값만큼 밑에서 이펙트 출력")]
    [SerializeField]
    private float _effectOffsetY = 0.5f;

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
            ProcessRockSpawning();
            yield return new WaitForSeconds(_spawnInterval);
        }
    }

    //--- Private Methods ---//
    private void ProcessRockSpawning()
    {
        if (_rockPrefab == null)
        {
            return;
        }

        GameObject rock = SpawnRock();
        ApplyLaunchForce(rock);
        PlayLaunchEffect();
    }

    private GameObject SpawnRock()
    {
        return Instantiate(_rockPrefab, _firePoint.position, Random.rotation);
    }

    private void ApplyLaunchForce(GameObject rock)
    {
        Rigidbody rockRigidbody = rock.GetComponent<Rigidbody>();
        if (rockRigidbody == null)
        {
            CustomDebug.LogError($"[FallingRockSpawner] 프리팹 '{rock.name}'에 Rigidbody가 없습니다!");
            return;
        }

        Vector3 launchDirection = CalculateLaunchDirection();
        rockRigidbody.AddForce(launchDirection * _launchSpeed, ForceMode.VelocityChange);
    }

    private Vector3 CalculateLaunchDirection()
    {
        Vector3 randomSpread = Random.insideUnitSphere * _spread;
        randomSpread.y = 0;
        return (transform.up + randomSpread).normalized;
    }

    private void PlayLaunchEffect()
    {
        Vector3 effectPos = _firePoint.position;
        effectPos.y -= _effectOffsetY; // 오프셋 적용

        VFXManager.Instance.PlayVFX(VFXType.VolcanoFire, effectPos, _firePoint.rotation);
    }

#if UNITY_EDITOR
    //--- Gizmos Logic ---//
    private void DrawTrajectoryGizmos()
    {
        Transform startPoint = (_firePoint != null) ? _firePoint : transform;

        Vector3 landingPosition = CalculateLandingPosition(startPoint);

        // 1. 착탄 지점
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(landingPosition, 1f);

        // 2. 탄퍼짐 범위
        DrawSpreadDisc(startPoint.position, landingPosition);

        // 3. 가이드 라인
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(startPoint.position, landingPosition);
    }

    // 물리 공식을 이용한 낙하 위치 예측 계산
    private Vector3 CalculateLandingPosition(Transform startPoint)
    {
        Vector3 initialVelocity = startPoint.up * _launchSpeed;
        float gravity = Mathf.Abs(Physics.gravity.y);

        float timeToImpact = (initialVelocity.y + Mathf.Sqrt(initialVelocity.y * initialVelocity.y + 2 * gravity * _fallHeight)) / gravity;

        Vector3 landingPosition = startPoint.position + (initialVelocity * timeToImpact);
        landingPosition.y = startPoint.position.y - _fallHeight;

        return landingPosition;
    }

    private void DrawSpreadDisc(Vector3 startPos, Vector3 landingPos)
    {
        float distance = Vector3.Distance(startPos, landingPos);
        float impactRadius = distance * _spread;

        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        UnityEditor.Handles.color = Color.red;
        UnityEditor.Handles.DrawWireDisc(landingPos, Vector3.up, impactRadius);
    }
#endif
}