using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    //--- Fields ---//
    private Rigidbody rb;
    private Animator animator;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
    }

    public void Move(Vector3 direction, float speed, float turnSpeed)
    {
        //Move the player with velocity control
        rb.linearVelocity = Vector3.ClampMagnitude(direction, 1f) * speed;
  
        animator.SetFloat("Speed", Mathf.Clamp(rb.linearVelocity.magnitude, 1f, speed));

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // 현재 회전값에서 목표 회전값으로 turnSpeed 만큼 부드럽게 보간(Slerp)
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }
    }
}
