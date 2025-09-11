using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("플레이어 이동")]
    [SerializeField]
    private float moveSpeed = 5.0f;

    void Update()
    {
        PlayerMove();
    }

    //플레이어 이동
    public void PlayerMove()
    {
        float horizontalInput = Input.GetAxis("Horizontal"); // A: -1, D: 1

        if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            Vector3 moveDirection = Vector3.zero;

            //카메라의 바라보는 각도에 따라 플레이어 횡이동 변경
            switch (CameraController.directionState)
            {
                case 0:
                    moveDirection = Vector3.right;
                    break;
                case 1:
                    moveDirection = Vector3.back;
                    break;
                case 2:
                    moveDirection = Vector3.left;
                    break;
                case 3:
                    moveDirection = Vector3.forward;
                    break;
            }

            transform.Translate(moveDirection * horizontalInput * moveSpeed * Time.deltaTime, Space.World);
        }
    }
}