using System.Collections;
using UnityEngine;

//TODO : 화산 기믹 버그 해결
public class FallingRockSpawner : MonoBehaviour
{
    [Header("화산 설정")]
    [SerializeField] 
    private GameObject _rockPrefab;
    [SerializeField] 
    private Transform _firePoint;

    [Header("발사 옵션")]
    [SerializeField] 
    private float _spawnInterval = 3.0f; //발사 주기
    [SerializeField] 
    private float _power = 30f;          //힘
    [SerializeField] 
    private float _spread = 0.3f;        //탄퍼짐 정도

    [Header("기즈모 설정 (예측)")]
    [Tooltip("화산의 높이보다 얼마나 아래로 떨어질지 예측. (보통 화산 높이만큼 입력)")]
    [SerializeField] 
    private float _fallHeight = 10f;

    private void Start()
    {
        if (_firePoint == null)
        {
            _firePoint = transform;
        }
        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            ShootRock();
            yield return new WaitForSeconds(_spawnInterval);
        }
    }

    private void ShootRock()
    {
        if (_rockPrefab == null)
        {
            return;
        }

        // 1. 생성
        GameObject rock = Instantiate(_rockPrefab, _firePoint.position, Random.rotation);

        // 2. 발사
        Rigidbody rb = rock.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 랜덤한 방향 계산
            Vector3 randomDir = Random.insideUnitSphere * _spread;
            randomDir.y = 0; // 높이에는 영향 안 주고 수평으로만 퍼지게 보정

            // 최종 발사 방향
            Vector3 dir = (transform.up + randomDir).normalized;
            rb.AddForce(dir * _power, ForceMode.Impulse);
        }
    }

    //탄 범위 그리기
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Transform startPoint = (_firePoint != null) ? _firePoint : transform;

        Vector3 v0 = startPoint.up * _power;
        float g = Mathf.Abs(Physics.gravity.y);

        float t = (v0.y + Mathf.Sqrt(v0.y * v0.y + 2 * g * _fallHeight)) / g;

        Vector3 landingPos = startPoint.position + (v0 * t);
        landingPos.y = startPoint.position.y - _fallHeight; // 높이는 바닥으로 고정

        // 1. 예상 착탄 지점 그리기
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(landingPos, 1f);

        // 2. 탄퍼짐 범위(원) 그리기
        float distance = Vector3.Distance(startPoint.position, landingPos);
        float impactRadius = distance * _spread;

        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        UnityEditor.Handles.color = Color.red;
        UnityEditor.Handles.DrawWireDisc(landingPos, Vector3.up, impactRadius);

        // 시각적 가이드 라인 (직선 거리)
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(startPoint.position, landingPos);
    }
#endif
}