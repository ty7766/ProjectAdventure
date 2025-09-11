using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("�÷��̾� �̵�")]
    [SerializeField]
    private float moveSpeed = 5.0f;

    void Update()
    {
        PlayerMove();
    }

    //�÷��̾� �̵�
    public void PlayerMove()
    {
        float horizontalInput = Input.GetAxis("Horizontal"); // A: -1, D: 1

        if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            Vector3 moveDirection = Vector3.zero;

            //ī�޶��� �ٶ󺸴� ������ ���� �÷��̾� Ⱦ�̵� ����
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