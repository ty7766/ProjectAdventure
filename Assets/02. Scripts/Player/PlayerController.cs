using System.Collections;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerProperties))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerHitEffect))]
public class PlayerController : MonoBehaviour
{
    //Animator parameter hashes
    private static readonly int DamageHash = Animator.StringToHash("Damage");
    private static readonly int DeadHash = Animator.StringToHash("Dead");
    private static readonly int GetUpHash = Animator.StringToHash("GetUp");

    //--- Components ---//
    private PlayerMovement _movement;
    private PlayerProperties _properties;
    private PlayerHitEffect _hitEffect;
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
    private WaitForSeconds _respawnDelayWait;
    private Quaternion _cameraRotation;
    private Vector3 _respawnPoint;
    private GameControls _controls;
    private bool _isAlive = true;
    private bool _isMovable = true;

    //--- properties ---//
    public int Health => _properties.Health;

    //--- Events ---//
    public event System.Action OnPlayerDamageTaken;
    public event System.Action OnPlayerFallenDown;

    //--- Unity Methods ---//
    private void Awake()
    {
        GetPlayerComponents();
        _controls = InputManager.Instance.GameControls;
        _cameraRotation = Quaternion.Euler(0, _cameraAngleOffset, 0);
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

        HandleInputs();
        UpdateTimer();
    }

    private void OnEnable()
    {
        _controls?.Player.Enable();
    }

    private void OnDisable()
    {
        _controls?.Player.Disable();
    }

    //지속 장판 관련
    private void OnCollisionStay(Collision collision)
    {
        //데드존에 지속적으로 있을 때
        if (collision.gameObject.CompareTag("Dead"))
        {
            TakeDamage(1);
        }
    }

    //--- Public Methods ---//
    /// <summary>
    /// 플레이어에게 데미지를 적용합니다. 일반적으로 데미지는 1이어야 하지만 기획에 따라 다를 수 있습니다.
    /// </summary>
    /// <param name="damage">적용 데미지 양 (기본값 = 1)</param>
    /// <param name="applyStun">경직 적용 여부</param>
    public void TakeDamage(int damage = 1, bool applyStun = true)
    {
        if (!_isAlive || _invTimer < _invincibleTime)
        {
            return;
        }

        if (_hitEffect != null)
        {
            _hitEffect.PlayHitEffect();
        }
        _properties.Health -= damage;
        OnPlayerDamageTaken?.Invoke();

        _invTimer = 0f;

        if (_properties.Health <= 0)
        {
            Dead();
        }
        else
        {
            SoundManager.Instance.PlaySFX(SoundType.SFX_PlayerDamaged);
            _animator.SetTrigger(DamageHash);
            if(applyStun)
            {
                _isMovable = false;
                StartCoroutine(EnableMovementAfterDelay(_stunDelay));
            }
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
    /// 플레이어를 지정한 위치로 즉시 이동시키고 리스폰 지점도 해당 위치로 설정합니다.
    /// </summary>
    public void PlaceAtStartPosition(Vector3 position)
    {
        _isAlive = true;
        _isMovable = true;
        _invTimer = _invincibleTime;
        _properties.ResetHealth();
        _animator.Rebind();
        _animator.Update(0f);
        _movement.TeleportTo(position);
        _respawnPoint = position;
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
        _hitEffect = GetComponent<PlayerHitEffect>();
    }

    private void InitializeTimer()
    {
        _stunDelay = new WaitForSeconds(_stunTime);
        _respawnDelayWait = new WaitForSeconds(_respawnDelay);
        _invTimer = _invincibleTime;
    }

    private void Dead()
    {
        SoundManager.Instance.PlaySFX(SoundType.SFX_PlayerDead);

        // Handle player death logic
        _isAlive = false;
        _isMovable = false;
        _movement.ResetMovements();
        _animator.SetTrigger(DeadHash);
    }

    private void StopPlayer()
    {
        _movement.Move(Vector3.zero, _properties.Speed, _properties.TurnSpeed);
    }

    private void RespawnWithDamagePenalty(int damage = 1)
    {
        OnPlayerFallenDown?.Invoke();
        _isMovable = false;
        _movement.TeleportTo(_respawnPoint);
        TakeDamage(damage, false); //리스폰 경직 적용을 위해 데미지 경직은 적용하지 않음.
        if (_isAlive)
        {
            _animator.SetTrigger(GetUpHash);
        }

        //Disable Player Movement for a short duration
        StartCoroutine(EnableMovementAfterDelay(_respawnDelayWait));
    }

    private void HandleInputs()
    {
        if (!_isMovable)
        {
            return;
        }

        Vector2 moveInput = _controls.Player.Move.ReadValue<Vector2>();
        Vector3 inputDirection = new Vector3(-moveInput.x, 0, -moveInput.y);
        _movement.Move(_cameraRotation * inputDirection, _properties.Speed, _properties.TurnSpeed);
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
    private IEnumerator EnableMovementAfterDelay(WaitForSeconds wfs)
    {
        yield return wfs;
        _isMovable = true;
    }
}