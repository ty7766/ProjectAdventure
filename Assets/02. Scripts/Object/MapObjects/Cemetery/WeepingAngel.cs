using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class WeepingAngel : MonoBehaviour
{
    [Header("Angel 속성")]
    [SerializeField]
    private float _angelSpeed = 3.0f;
    [SerializeField]
    private float _angelRotationSpeed = 10f;
    [SerializeField]
    private int _damageAmount = 1;

    [Header("플레이어 감지 속성")]
    [SerializeField]
    private float _activeRange = 15.0f;
    [SerializeField]
    private float _playerViewAngle = 100f;

    [Header("Angel 활동 범위 제한")]
    [SerializeField]
    private Vector2 _mapSize = new Vector2(12f, 10f);
    [SerializeField]
    private Transform _mapCenterTransform;

    private Vector3 _initialPosition;
    private Quaternion _initialRotation;
    private Transform _playerTransform;

    private void Awake()
    {
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;
    }

    private void OnEnable()
    {
        transform.position = _initialPosition;
        transform.rotation = _initialRotation;

        if (_playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null )
            {
                _playerTransform = player.transform;
            }
        }
    }

    private void Update()
    {
        if (_playerTransform == null || _mapCenterTransform == null)
        {
            return;
        }

        if (!IsPlayerInMap())
        {
            return;
        }

        if (Vector3.Distance(transform.position, _playerTransform.position) > _activeRange)
        {
            return;
        }

        if (IsVisibleToPlayer())
        {
            return;
        }

        ChasePlayer();
    }

    private bool IsPlayerInMap()
    {
        float dx = Mathf.Abs(_playerTransform.position.x - _mapCenterTransform.position.x);
        float dz = Mathf.Abs(_playerTransform.position.z - _mapCenterTransform.position.z);
        return (dx <= _mapSize.x / 2f) && (dz <= _mapSize.y / 2f);
    }

    //플레이어가 보이는지 체크
    private bool IsVisibleToPlayer()
    {
        Vector3 directionToAngel = (transform.position - _playerTransform.position).normalized;

        if (Vector3.Angle(_playerTransform.forward, directionToAngel) < _playerViewAngle / 2f)
        {
            return true;
        }
        return false;
    }

    private void ChasePlayer()
    {
        //바라보기
        Vector3 direction = (_playerTransform.position - transform.position).normalized;
        direction.y = 0;

        if(direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _angelRotationSpeed * Time.deltaTime);
        }
        //이동
        transform.position = Vector3.MoveTowards(transform.position, _playerTransform.position, _angelSpeed * Time.deltaTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            HandlePlayerHit(collision);
        }
    }

    private void HandlePlayerHit(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<PlayerController>(out var playerController))
        {
            playerController.TakeDamage(_damageAmount);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_mapCenterTransform != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(_mapCenterTransform.position, new Vector3(_mapSize.x, 1, _mapSize.y));
        }

        if (_playerTransform != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 leftRay = Quaternion.Euler(0, -_playerViewAngle / 2f, 0) * _playerTransform.forward;
            Vector3 rightRay = Quaternion.Euler(0, _playerViewAngle / 2f, 0) * _playerTransform.forward;
            Gizmos.DrawRay(_playerTransform.position, leftRay * 5f);
            Gizmos.DrawRay(_playerTransform.position, rightRay * 5f);
        }
    }
#endif
}
