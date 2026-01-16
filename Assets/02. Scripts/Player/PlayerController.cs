using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.Processors;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerProperties))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    //--- Components ---//
    private PlayerMovement _movement;
    private PlayerProperties _properties;
    private Animator _animator;

    //--- Settings ---//
    [Header("Camera Settings")]
    [SerializeField, Tooltip("이동 카메라 각도 보정치")]
    private float _cameraAngleOffset = -75f;

    [Header("Map Restrictions")]
    [SerializeField, Tooltip("플레이어 낙사 임계치 Y좌표")]
    private float _fallThresholdY = -10f;

    [Header("Combat Settings")]
    [SerializeField, Tooltip("리스폰 후 경직시간")]
    private float _respawnDelay = 2.0f;

    [SerializeField, Tooltip("피격 후 무적시간")]
    private float _invincibleTime = 3.0f;

    [SerializeField, Tooltip("피격 후 경직시간")]
    private float _stunTime = 0.39f;

    //--- Private Fields ---//
    private float _invTimer;
    private WaitForSeconds _stunDelay;
    private Vector3 _respawnPoint;
    private bool _isAlive = true;
    private bool _isMovable = true;


    //--- Unity Methods ---//
    private void Awake()
    {
        GetPlayerComponents();
    }

    private void Start()
    {
        SetRespawnPoint();
        InitializeTimer();
    }

    private void Update()
    {
        if (!_isAlive)
        {
            StopPlayer();
            return;
        }

        if (!_isMovable)
        {
            StopPlayer();
        }

        if (IsPlayerFallOut())
        {
            RespawnWithDamagePenalty();
        }

        if (_movement != null)
        {
            HandleInputs();
        }

        UpdateTimer();

    }

    //지속 장판 관련
    private void OnCollisionStay(Collision collision)
    {
        //데드존에 지속적으로 있을 때
        if (collision.gameObject.CompareTag("Dead"))
        {
            //Stay는 매 프레임 실행
            //but 무적시간으로 영향 X
            TakeDamage(1);
        }
    }

    //--- Public Methods ---//
    /// <summary>
    /// 플레이어에게 데미지를 적용합니다. 일반적으로 데미지는 1이어야 하지만 기획에 따라 다를 수 있습니다.
    /// </summary>
    /// <param name="damage">적용 데미지 양 (기본값 = 1)</param>
    public void TakeDamage(int damage = 1)
    {
        if (!_isAlive || _invTimer < _invincibleTime)
        {
            return;
        }

        _properties.Health -= damage;

        _invTimer = 0f;

        if (_properties.Health <= 0)
        {
            Dead();
        }
        else
        {
            _animator.SetTrigger("Damage");
            _isMovable = false;
            StartCoroutine(EnableMovementAfterDelay(_stunDelay));
        }
    }

    /// <summary>
    /// 플레이어의 복구 위치를 지정된 위치로 설정합니다.
    /// </summary>
    /// <param name="newRespawnPoint">새로운 스폰 지점</param>
    public void SetRespawnPoint(Vector3 newRespawnPoint)
    {
        _respawnPoint = newRespawnPoint;
    }

    /// <summary>
    /// 플레이어의 복구 위치를 현재 위치로 설정합니다.
    /// </summary>
    public void SetRespawnPoint()
    {
        _respawnPoint = this.transform.position;
    }

    /// <summary>
    /// 플레이어의 조작을 비활성화합니다. (컷씬, 대화, 경직 등)
    /// </summary>
    public void DisablePlayerControl()
    {
        _isMovable = false;
    }

    /// <summary>
    /// 플레이어의 조작을 활성화합니다.
    /// </summary>
    public void EnablePlayerControl()
    {
        _isMovable = true;
    }

    //--- Private Methods ---//
    private void GetPlayerComponents()
    {
        _animator = GetComponent<Animator>();
        _movement = GetComponent<PlayerMovement>();
        _properties = GetComponent<PlayerProperties>();
    }

    private void InitializeTimer()
    {
        _stunDelay = new WaitForSeconds(_stunTime);
        _invTimer = _invincibleTime;
    }

    private void Dead()
    {
        // Handle player death logic
        _isAlive = false;
        _isMovable = false;
        _movement.ResetMovements();
        _animator.SetTrigger("Dead");
    }

    private void StopPlayer()
    {
        _movement.Move(Vector3.zero, _properties.Speed, _properties.TurnSpeed);
    }

    private void RespawnWithDamagePenalty(int damage = 1)
    {
        _isMovable = false;
        _movement.TeleportTo(_respawnPoint);
        TakeDamage(damage);
        if (_isAlive)
        {
            _animator.SetTrigger("GetUp");
        }

        //Disable Player Movement for a short duration
        StartCoroutine(EnableMovementAfterDelay(_respawnDelay));
    }

    private void HandleInputs()
    {
        if (!_isMovable)
        {
            return;
        }

        Vector3 inputDirection = new Vector3(-Input.GetAxis("Horizontal"), 0, -Input.GetAxis("Vertical"));
        Quaternion camRotation = Quaternion.Euler(0, _cameraAngleOffset, 0);
        _movement.Move(camRotation * inputDirection, _properties.Speed, _properties.TurnSpeed);


#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Damage Taken Simulated");
            TakeDamage(1);
        }
#endif
    }

    private void UpdateTimer()
    {
        _invTimer += Time.deltaTime;
    }

    private bool IsPlayerFallOut()
    {
        return this.transform.position.y < _fallThresholdY;
    }


    //--- Coroutines ---//
    private IEnumerator EnableMovementAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        _isMovable = true;
    }

    private IEnumerator EnableMovementAfterDelay(WaitForSeconds wfs)
    {
        yield return wfs;
        _isMovable = true;
    }
}
