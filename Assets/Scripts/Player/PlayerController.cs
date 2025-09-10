using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("플레이어 이동")]
    [SerializeField]
    private float moveSpeed = 5.0f;

    public Transform cameraTransform;

    void Update()
    {
        PlayerMove();
    }

    //플레이어 이동
    public void PlayerMove()
    {
        float horizontalInput = Input.GetAxis("Horizontal");

        if(Mathf.Abs(horizontalInput) > 0.1f)
        {
            // 카메라의 회전 각도 반영하여 횡이동
            float roundedY = Mathf.Round(cameraTransform.eulerAngles.y / 90.0f) * 90.0f;

            Quaternion cameraRotation = Quaternion.Euler(0, roundedY, 0);

            Vector3 moveDirection = cameraRotation * Vector3.right * horizontalInput;

            //플레이어 가속
            transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
        }
    }
}
