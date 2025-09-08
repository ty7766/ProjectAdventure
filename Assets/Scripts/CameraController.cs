using UnityEngine;

public class CameraController : MonoBehaviour
{
    // 카메라가 회전할 중심 오브젝트
    public Transform target;

    // 카메라와 타겟 사이의 거리
    public float distance = 5.0f;

    private float currentX = 0.0f;
    private float currentY = 0.0f;

    // 키보드 회전 관련 변수
    private float targetRotationY = 0.0f;
    public float keyboardRotationSpeed = 3.0f; // 키보드 회전 속도

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        currentX = angles.y;
        currentY = angles.x;
        targetRotationY = currentX;
    }

    void Update()
    {

        // Q, E 키를 누르는 순간에 90도씩 회전 목표 설정
        if (Input.GetKeyDown(KeyCode.Q))
        {
            targetRotationY -= 90.0f; // 왼쪽으로 90도 회전
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            targetRotationY += 90.0f; // 오른쪽으로 90도 회전
        }
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        // 마우스 회전과 키보드 회전 값을 부드럽게 보간
        currentX = Mathf.Lerp(currentX, targetRotationY, Time.deltaTime * keyboardRotationSpeed);

        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);

        Vector3 position = rotation * new Vector3(0.0f, 0.0f, -distance) + target.position;

        transform.rotation = rotation;
        transform.position = position;
    }
}