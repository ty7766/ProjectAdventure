
using System.Diagnostics;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Collider))]
public class PlayerMovement : MonoBehaviour
{
    //--- Components ---//
    private Rigidbody _rb;
    private Animator _animator;
    private Collider _col;

    //--- Fields ---//
    private bool _isJumping = false;
    private Vector3 _direction;
    private float _speed;
    private float _turnSpeed;

    //--- Settings ---//
    [Header("Ground Detection")]
    [SerializeField] private LayerMask _groundLayer; 
    [SerializeField] private float _groundCheckDist = 0.1f;

    //--- Unity Methods ---//
    private void Awake()
    {
        GetPlayerComponents();
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
    private void GetPlayerComponents()
    {
        _rb = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
        _col = GetComponent<Collider>();
    }

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
        Vector3 rayOrigin;
        float rayLength;
        bool isGrounded;
        DoGroundCheck(out rayOrigin, out rayLength, out isGrounded);

        if (isGrounded)
        {
            ApplyMoveWithAnimation();
        }
        else
        {
            RunJumpAnimationOnlyOnce();
        }

        DebugGizmo(rayOrigin, rayLength, isGrounded);
    }

    private void DoGroundCheck(out Vector3 rayOrigin, out float rayLength, out bool isGrounded)
    {
        rayOrigin = _col.bounds.center;
        rayLength = _col.bounds.extents.y + _groundCheckDist;
        isGrounded = Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayLength, _groundLayer);
        _animator?.SetBool("isGround", isGrounded);
    }

    [Conditional("UNITY_EDITOR")]
    private static void DebugGizmo(Vector3 rayOrigin, float rayLength, bool isGrounded)
    {
        UnityEngine.Debug.DrawRay(rayOrigin, Vector3.down * rayLength, isGrounded ? Color.green : Color.red);
    }

    private void RunJumpAnimationOnlyOnce()
    {
        if (!_isJumping)
        {
            _animator.SetTrigger("Jump");
            _isJumping = true;
        }
    }

    private void ApplyMoveWithAnimation()
    {
        _isJumping = false;
        ApplyTargetVelocity();
        SetAnimationSpeed();
    }

    private void SetAnimationSpeed()
    {
        float animSpeed = _rb.linearVelocity.magnitude;
        _animator.SetFloat("Speed", Mathf.Clamp(animSpeed, 1f, _speed));
    }

    private void ApplyTargetVelocity()
    {
        Vector3 targetVelocity = Vector3.ClampMagnitude(_direction, 1f) * _speed;
        targetVelocity.y = _rb.linearVelocity.y;
        _rb.linearVelocity = targetVelocity;
    }
}