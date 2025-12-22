using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    //--- Fields ---//
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Move(Vector3 direction, float speed)
    {
        //Move the player based on input direction and speed
        Vector3 movement = direction.normalized * speed * Time.deltaTime;
        rb.MovePosition(transform.position + movement);
    }
}
