using UnityEngine;

public class CameraController : MonoBehaviour
{
    // ī�޶� ȸ���� �߽� ������Ʈ
    public Transform target;

    // ī�޶�� Ÿ�� ������ �Ÿ�
    public float distance = 5.0f;

    private float currentX = 0.0f;
    private float currentY = 0.0f;

    // Ű���� ȸ�� ���� ����
    private float targetRotationY = 0.0f;
    public float keyboardRotationSpeed = 3.0f; // Ű���� ȸ�� �ӵ�

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        currentX = angles.y;
        currentY = angles.x;
        targetRotationY = currentX;
    }

    void Update()
    {

        // Q, E Ű�� ������ ������ 90���� ȸ�� ��ǥ ����
        if (Input.GetKeyDown(KeyCode.Q))
        {
            targetRotationY -= 90.0f; // �������� 90�� ȸ��
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            targetRotationY += 90.0f; // ���������� 90�� ȸ��
        }
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        // ���콺 ȸ���� Ű���� ȸ�� ���� �ε巴�� ����
        currentX = Mathf.Lerp(currentX, targetRotationY, Time.deltaTime * keyboardRotationSpeed);

        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);

        Vector3 position = rotation * new Vector3(0.0f, 0.0f, -distance) + target.position;

        transform.rotation = rotation;
        transform.position = position;
    }
}