using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    //--- Fields ---//
    private Rigidbody rb;
    private Animator animator;
    private Collider col;
    bool b_isJumping = false;


    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer; // 바닥으로 인식할 레이어 (Inspector에서 설정!)
    [SerializeField] private float groundCheckDist = 0.1f; // 발바닥에서 얼마나 더 체크할지

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        col = GetComponent<Collider>();
    }

    public void ResetSpeed()
    {
        rb.linearVelocity = Vector3.zero;
        animator.SetFloat("Speed", 1);
        b_isJumping = false;
    }

    public void Move(Vector3 direction, float speed, float turnSpeed)
    {
        // 1. 바닥 체크 (Raycast 쏨)
        // 1. 레이 시작점: 내 몸통 정중앙 (배꼽)
        Vector3 rayOrigin = col.bounds.center;

        // 2. 레이 길이: (몸통 절반 높이) + (여유분)
        float rayLength = col.bounds.extents.y + groundCheckDist;

        // 3. 레이 쏘기
        bool isGrounded = Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayLength, groundLayer);

        // 디버그: 이제 배꼽부터 발바닥 아래까지 시원하게 뻗는지 봐봐!
        Debug.DrawRay(rayOrigin, Vector3.down * rayLength, isGrounded ? Color.green : Color.red);

        // 2. 이동 로직 (땅에 있을 때만 컨트롤 가능하게)
        if (isGrounded)
        {
            // 속도 계산
            Vector3 targetVelocity = Vector3.ClampMagnitude(direction, 1f) * speed;

            //허공답보 방지
            targetVelocity.y = rb.linearVelocity.y;
            rb.linearVelocity = targetVelocity;

            // 애니메이션 (땅에 있을 때만 걷는 모션)
            // 너무 0에 가까우면 1로 튀는거 방지용으로 삼항연산자 살짝 씀
            float animSpeed = rb.linearVelocity.magnitude;
            animator.SetFloat("Speed", Mathf.Clamp(animSpeed, 1f, speed)); //걷는 애니메이션 정지되는 것 방지 (애니메이션 실행 속도가 0이 되는 것 방지)

            // 3. 회전 로직 (이동 입력이 있을 때만)
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }
        }
        else
        {
            // 공중에 뜸 - 제어 불가 - 낙하 애니메이션 실행
            if (!b_isJumping)
            {
                animator.SetTrigger("Jump");
                b_isJumping = true;
            }
        }
    }
}