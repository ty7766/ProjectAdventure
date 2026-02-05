using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class SnowBallSpawner : MonoBehaviour
{
    [Header("생성 설정")]
    [SerializeField]
    private PoolObjectType _snowBallType = PoolObjectType.SnowBall;
    [SerializeField] 
    private Transform _spawnPoint;

    [Header("물리 설정")]
    [SerializeField] 
    private Vector3 _initialForce = new Vector3(0, -2f, 10f);

    [Header("트리거 설정")]
    [SerializeField]
    private bool _isOneTimeUse = true;

    private void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SpawnSnowBall();
        }
    }

    private void SpawnSnowBall()
    {
        if (ObjectPoolManager.Instance == null || _spawnPoint == null)
        {
            return;
        }

        GameObject snowBall = ObjectPoolManager.Instance.SpawnObject(_snowBallType, _spawnPoint.position, _spawnPoint.rotation);

        if (snowBall != null && snowBall.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(_initialForce, ForceMode.Impulse);
        }

        if (_isOneTimeUse)
        {
            GetComponent<Collider>().enabled = false;
            this.enabled = false;
        }
    }
}