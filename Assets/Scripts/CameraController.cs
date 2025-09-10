using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("ī�޶� ����")]
    public Transform pivotPoint; // ī�޶� ȸ���� ������ �߽���
    public Transform target;     // ī�޶� �׻� �ٶ� Ÿ�� (�÷��̾�)

    public float distance = 5.0f;      // �߽������ �Ÿ�
    public float rotationSpeed = 5.0f; // ȸ�� �ӵ�

    // 0: ��(East), 1: ��(North), 2: ��(West), 3: ��(South)
    public static int directionState = 0;
    private float targetRotationY = 45.0f;

    void Start()
    {
        // ���� ���� �� �ʱ� ī�޶� ���� ����
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, targetRotationY, 0);
    }

    void Update()
    {
        bool hasRotated = false;
        if (Input.GetKeyDown(KeyCode.Q))
        {
            directionState--; // �������� ���� ����
            hasRotated = true;
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            directionState++; // ���������� ���� ����
            hasRotated = true;
        }

        if (hasRotated)
        {
            // directionState ���� 0~3 ���̷� ����
            directionState = (directionState % 4 + 4) % 4;
            // ���¿� ���� ��ǥ ȸ�� ������ ���
            targetRotationY = 45.0f + directionState * 90.0f;
        }
    }

    void LateUpdate()
    {
        if (pivotPoint == null || target == null) return;

        Quaternion currentRotation = transform.rotation;
        Quaternion targetPositioningRotation = Quaternion.Euler(transform.eulerAngles.x, targetRotationY, 0);
        Quaternion newPositioningRotation = Quaternion.Lerp(currentRotation, targetPositioningRotation, Time.deltaTime * rotationSpeed);
        transform.position = pivotPoint.position + newPositioningRotation * new Vector3(0.0f, 0.0f, -distance);

        transform.LookAt(target);
    }
}