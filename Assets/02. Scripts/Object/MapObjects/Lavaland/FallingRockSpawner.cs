using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;

public class FallingRockSpawner : SpawnedObjectManager<FallingRock>
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

    private WaitForSeconds _waitSpawnInterval;

    private void Awake()
    {
        Assert.IsNotNull(_rockPrefab, $"[FallingRockSpawner] '{name}'에 Rock Prefab이 할당되지 않았습니다.");

        if (_firePoint == null)
        {
            _firePoint = transform;
        }

        _waitSpawnInterval = new WaitForSeconds(_spawnInterval);
    }

    //SpawnedObjectManager 상속
    protected override IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return _waitSpawnInterval;
            ProcessRockSpawning();
        }
    }
    //SpawnedObjectManager 상속
    protected override void ReturnObjectToPool(FallingRock fallingRock)
    {
        if (fallingRock != null)
        {
            Destroy(fallingRock.gameObject);
        }
    }

    private void ProcessRockSpawning()
    {
        if (_rockPrefab == null)
        {
            return;
        }

        PlayLaunchEffect();

        GameObject rock = Instantiate(_rockPrefab, _firePoint.position, Random.rotation);
        if (rock.TryGetComponent<FallingRock>(out var rockScript))
        {
            RegisterObject(rockScript);
            ApplyLaunchForce(rock);
        }
        else
        {
            Destroy(rock);
        }
    }

    private void ApplyLaunchForce(GameObject rock)
    {
        if (rock.TryGetComponent(out Rigidbody rigidbody))
        {
            Vector3 direction = FallingRockTrajectoryCalculator.GetRandomLaunchDirection(transform.up, _spread);
            rigidbody.AddForce(direction * _launchSpeed, ForceMode.VelocityChange);
        }
    }

    private void PlayLaunchEffect()
    {
        Vector3 effectPos = _firePoint.position;
        effectPos.y -= _effectOffsetY; // 오프셋 적용
        VFXManager.Instance.PlayVFX(VFXType.VolcanoFire, effectPos, _firePoint.rotation);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_firePoint == null) return;

        // 기즈모 그릴 때도 계산기(Calculator) 사용
        Vector3 startPos = _firePoint.position;
        Vector3 velocity = _firePoint.up * _launchSpeed;

        // 예상 착탄 지점 받아오기
        Vector3 landingPos = FallingRockTrajectoryCalculator.PredictLandingPosition(startPos, velocity, _fallHeight);

        // 1. 착탄 지점 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(landingPos, 1f);

        // 2. 탄퍼짐 범위 표시 (Spread Disc)
        float distance = Vector3.Distance(startPos, landingPos);
        float impactRadius = distance * _spread;

        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        UnityEditor.Handles.color = Color.red;
        UnityEditor.Handles.DrawWireDisc(landingPos, Vector3.up, impactRadius);

        // 3. 궤적 선
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(startPos, landingPos);
    }
#endif
}