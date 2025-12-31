
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    //--- Fields ---//
    private Rigidbody rb;
    private Animator animator;
    private Collider col;
    bool b_isJumping = false;
    private Vector3 direction;
    private float speed;
    private float turnSpeed;


    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer; 
    [SerializeField] private float groundCheckDist = 0.1f;

    //--- Unity Methods ---//
    private void Start()
    {
        if(!TryGetComponent<Rigidbody>(out rb))
        {
            Debug.LogWarning("Rigidbody Component not found on player");
        }
        if(!TryGetComponent<Animator>(out animator))
        {
            Debug.LogWarning("Animator Component not found on player");
        }
        if(!TryGetComponent<Collider>(out col))
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
        if (col == null || animator == null || rb == null)
        {
            return;
        }

        //ground check
        Vector3 rayOrigin = col.bounds.center;
        float rayLength = col.bounds.extents.y + groundCheckDist;
        bool isGrounded = Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayLength, groundLayer);
        animator?.SetBool("isGround", isGrounded);

        //debug raycast (scene view)
        Debug.DrawRay(rayOrigin, Vector3.down * rayLength, isGrounded ? Color.green : Color.red);


        if (isGrounded)
        {
            b_isJumping = false;
            //apply movement only when player is on ground
            Vector3 targetVelocity = Vector3.ClampMagnitude(direction, 1f) * speed;
            targetVelocity.y = rb.linearVelocity.y;
            rb.linearVelocity = targetVelocity;


            float animSpeed = rb.linearVelocity.magnitude;
            animator.SetFloat("Speed", Mathf.Clamp(animSpeed, 1f, speed));
        }
        else
        {
            if (!b_isJumping)
            {
                //play animation only once when player falling
                animator.SetTrigger("Jump");
                b_isJumping = true;
            }
        }

    }

    //--- Public Methods ---//
    //player rigidbody status reset
    public void ResetSpeed()
    {
        rb.linearVelocity = Vector3.zero;
        animator.SetFloat("Speed", 1);
        b_isJumping = false;
    }

    //tp player to desired position
    public void TeleportTo(Vector3 position)
    {
        rb.position = position; //move position based on rigidbody
        ResetSpeed(); //reset rigidbody properties to pretend unintentionall movements
    }

    public void Move(Vector3 direction, float speed, float turnSpeed)
    {
        this.direction = direction;
        this.speed = speed;
        this.turnSpeed = turnSpeed;
    }

    //--- Private Helper ---//
    private void ChangeViewDirection()
    {
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }
    }
}