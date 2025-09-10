using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("�÷��̾� �̵�")]
    [SerializeField]
    private float moveSpeed = 5.0f;

    public Transform cameraTransform;

    void Update()
    {
        PlayerMove();
    }

    //�÷��̾� �̵�
    public void PlayerMove()
    {
        float horizontalInput = Input.GetAxis("Horizontal");

        if(Mathf.Abs(horizontalInput) > 0.1f)
        {
            // ī�޶��� ȸ�� ���� �ݿ��Ͽ� Ⱦ�̵�
            float roundedY = Mathf.Round(cameraTransform.eulerAngles.y / 90.0f) * 90.0f;

            Quaternion cameraRotation = Quaternion.Euler(0, roundedY, 0);

            Vector3 moveDirection = cameraRotation * Vector3.right * horizontalInput;

            //�÷��̾� ����
            transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
        }
    }
}
