using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
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
    private Rigidbody _rigidbody;

    private float _sqrActiveRange;
    private float _cosViewAngle;

    private bool _canChase;

    private void Awake()
    {
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;
        _rigidbody = GetComponent<Rigidbody>();

        _sqrActiveRange = _activeRange * _activeRange;
        _cosViewAngle = Mathf.Cos(_playerViewAngle * 0.5f * Mathf.Deg2Rad);
    }

    private void OnEnable()
    {
        transform.position = _initialPosition;
        transform.rotation = _initialRotation;

        FindPlayer();
    }

    private void Update()
    {
        _canChase = false;

        if (_playerTransform == null)
        {
            FindPlayer();
            if (_playerTransform == null)
            {
                return;
            }
        }
        if (_mapCenterTransform == null)
        {
            return;
        }
        if (!IsPlayerInMap())
        {
            return;
        }

        Vector3 directionToPlayer = _playerTransform.position - transform.position;
        if (directionToPlayer.sqrMagnitude > _sqrActiveRange)
        {
            return;
        }
        if (IsVisibleToPlayer(directionToPlayer))
        {
            return;
        }

        _canChase = true;
    }

    private void FixedUpdate()
    {
        if (_canChase)
        {
            ChasePlayer();
        }
    }

    private void FindPlayer()
    {
        if (_playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
            }
        }
    }
    private bool IsPlayerInMap()
    {
        float dx = Mathf.Abs(_playerTransform.position.x - _mapCenterTransform.position.x);
        float dz = Mathf.Abs(_playerTransform.position.z - _mapCenterTransform.position.z);
        return (dx <= _mapSize.x * 0.5f) && (dz <= _mapSize.y * 0.5f);
    }

    //플레이어가 보이는지 체크
    private bool IsVisibleToPlayer(Vector3 directionToPlayer)
    {
        //플레이어가 위/아래를 봐도 정확히 체크하기 위해 Y축을 제거하고 평면 벡터로 만듦
        Vector3 playerLookDirFlattened = new Vector3(_playerTransform.forward.x, 0, _playerTransform.forward.z).normalized;
        Vector3 dirToAngelFlattened = new Vector3(-directionToPlayer.x, 0, -directionToPlayer.z).normalized;

        // 내적 계산
        float dot = Vector3.Dot(playerLookDirFlattened, dirToAngelFlattened);

        // 내적 값이 기준 코사인값보다 크면 시야각 안에 있는 것
        return dot >= _cosViewAngle;
    }

    private void ChasePlayer()
    {
        Vector3 targetPosition = new Vector3(_playerTransform.position.x, transform.position.y, _playerTransform.position.z);
        Vector3 direction = (targetPosition - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _angelRotationSpeed * Time.fixedDeltaTime);
        }

        // 이동
        Vector3 newPosition = Vector3.MoveTowards(transform.position, targetPosition, _angelSpeed * Time.fixedDeltaTime);
        _rigidbody.MovePosition(newPosition);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (collision.gameObject.TryGetComponent<PlayerController>(out var playerController))
            {
                playerController.TakeDamage(_damageAmount);
            }
        }
    }
}
