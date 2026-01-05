
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Collider))]
public class PlayerMovement : MonoBehaviour
{
    //--- Fields ---//
    private Rigidbody _rb;
    private Animator _animator;
    private Collider _col;
    private bool _isJumping = false;
    private Vector3 _direction;
    private float _speed;
    private float _turnSpeed;


    [Header("Ground Detection")]
    [SerializeField] private LayerMask _groundLayer; 
    [SerializeField] private float _groundCheckDist = 0.1f;

    //--- Unity Methods ---//
    private void Awake()
    {
        if(!TryGetComponent<Rigidbody>(out _rb))
        {
            Debug.LogWarning("Rigidbody Component not found on player");
        }
        if(!TryGetComponent<Animator>(out _animator))
        {
            Debug.LogWarning("Animator Component not found on player");
        }
        if(!TryGetComponent<Collider>(out _col))
        {
            Debug.LogWarning("Collider Component not found on player");
        }
    }

    private void Update()
    {
        ChangeViewDirection();
    }

    private void FixedUpdate()
    {
        if (_col == null || _animator == null || _rb == null)
        {
            return;
        }

        ApplyMovement();
    }

    //--- Public Methods ---//
    /// <summary>
    /// 플레이어의 리지드바디 속도와 상태 플래그를 초기화 변경합니다.
    /// </summary>
    public void ResetMovements()
    {
        _rb.linearVelocity = Vector3.zero;
        _animator.SetFloat("Speed", 1);
        _isJumping = false;
    }

    /// <summary>
    /// 플레이어를 지정된 위치로 이동시킵니다
    /// </summary>
    /// <param name="position"></param>
    public void TeleportTo(Vector3 position)
    {
        _rb.position = position;
        ResetMovements();
    }

    /// <summary>
    /// 플레이어의 이동 방향과 속도를 설정합니다.
    /// </summary>
    /// <param name="direction">이동 방향</param>
    /// <param name="speed">이동 속도</param>
    /// <param name="turnSpeed">플레이어 회전 속도</param>
    public void Move(Vector3 direction, float speed, float turnSpeed)
    {
        this._direction = direction;
        this._speed = speed;
        this._turnSpeed = turnSpeed;
    }

    //--- Private Helper ---//
    private void ChangeViewDirection()
    {
        if (_direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _turnSpeed * Time.deltaTime);
        }
    }

    private void ApplyMovement()
    {
        //ground check
        Vector3 rayOrigin = _col.bounds.center;
        float rayLength = _col.bounds.extents.y + _groundCheckDist;
        bool isGrounded = Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayLength, _groundLayer);
        _animator?.SetBool("isGround", isGrounded);

#if UNITY_EDITOR
        Debug.DrawRay(rayOrigin, Vector3.down * rayLength, isGrounded ? Color.green : Color.red);
#endif


        if (isGrounded)
        {
            _isJumping = false;
            //apply movement only when player is on ground
            Vector3 targetVelocity = Vector3.ClampMagnitude(_direction, 1f) * _speed;
            targetVelocity.y = _rb.linearVelocity.y;
            _rb.linearVelocity = targetVelocity;


            float animSpeed = _rb.linearVelocity.magnitude;
            _animator.SetFloat("Speed", Mathf.Clamp(animSpeed, 1f, _speed));
        }
        else
        {
            if (!_isJumping)
            {
                //play animation only once when player falling
                _animator.SetTrigger("Jump");
                _isJumping = true;
            }
        }
    }
}