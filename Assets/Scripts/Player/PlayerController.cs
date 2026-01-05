using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.Processors;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerProperties))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    //--- Componenents ---//
    [SerializeField]
    private PlayerMovement _movement;
    [SerializeField]
    private PlayerProperties _properties;
    private Animator _animator;

    //--- Fields ---//
    [SerializeField] [Header("이동 카메라 각도 보정치")]
    private float _cameraAngleOffset = -75f;
    [SerializeField] [Header("플레이어 낙사 임계치 Y좌표")]
    private float _fallThresholdY = -10f;
    [SerializeField] [Header("리스폰후 경직시간")]
    private float _respawnDelay = 2.0f;
    [SerializeField] [Header("피격 후 무적시간")]
    private float _invincibleTime = 3.0f;
    private float _invTimer;
    [SerializeField] [Header("피격 후 경직시간")]
    private float _stunTime = 0.39f;
    private WaitForSeconds _stunDelay;

    private Vector3 _respawnPoint;
    private bool _isAlive = true;
    private bool _isMovable = true;


    //--- Unity Methods ---//
    private void Awake()
    {
        if(!TryGetComponent<Animator>(out _animator))
        {
            Debug.LogWarning("Animator component not found on Player.");
        }
    }

    private void Start()
    {
        SetRespawnPoint();
        _stunDelay = new WaitForSeconds(_stunTime);
        _invTimer = _invincibleTime;
    }

    private void Update()
    {
        if(!_isMovable)
        {
            _movement.Move(Vector3.zero, _properties.Speed, _properties.TurnSpeed);
        }

        if(!_isAlive)
        {
            return;
        }

        if (this.transform.position.y < _fallThresholdY)
        {
            Respawn();
        }

        if(_movement != null)
        {
            HandleInputs();
        }

        UpdateTimer();

    }

    //--- Public Methods ---//
    /// <summary>
    /// 플레이어에게 데미지를 적용합니다. 일반적으로 데미지는 1이어야 하지만 기획에 따라 다를 수 있습니다.
    /// </summary>
    /// <param name="damage">적용 데미지 양</param>
    public void TakeDamage(int damage)
    {
        if(!_isAlive || _invTimer < _invincibleTime)
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
    /// 플레이어의 조작을 비활성화합니다.
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
    private void Dead()
    {
        // Handle player death logic
        _isAlive = false;
        _isMovable = false;
        _movement.ResetMovements();
        _animator.SetTrigger("Dead");
    }

    //reset player position to respawnpoint with damage penalty
    private void Respawn()
    {
        _isMovable = false;
        _movement.TeleportTo(_respawnPoint);
        TakeDamage(1);
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
        Quaternion camRotation = Quaternion.Euler(0,_cameraAngleOffset, 0);
        _movement.Move(camRotation * inputDirection, _properties.Speed, _properties.TurnSpeed);


#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Space)){
            Debug.Log("Damage Taken Simulated");
            TakeDamage(1);
        }
#endif
    }

    private void UpdateTimer(){
        _invTimer += Time.deltaTime;
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

    //지속 장판 관련
    private void OnCollisionStay(Collision collision)
    {
        //데드존에 지속적으로 있을 때
        if(collision.gameObject.CompareTag("Dead"))
        {
            //Stay는 매 프레임 실행
            //but 무적시간으로 영향 X
            TakeDamage(1);
        }
    }
}
