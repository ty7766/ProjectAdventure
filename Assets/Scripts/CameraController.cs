using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("카메라 설정")]
    public Transform pivotPoint; // 카메라가 회전할 고정된 중심축
    public Transform target;     // 카메라가 항상 바라볼 타겟 (플레이어)

    public float distance = 5.0f;      // 중심축과의 거리
    public float rotationSpeed = 5.0f; // 회전 속도

    // 0: 동(East), 1: 북(North), 2: 서(West), 3: 남(South)
    public static int directionState = 0;
    private float targetRotationY = 45.0f;

    void Start()
    {
        // 게임 시작 시 초기 카메라 각도 설정
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, targetRotationY, 0);
    }

    void Update()
    {
        bool hasRotated = false;
        if (Input.GetKeyDown(KeyCode.Q))
        {
            directionState--; // 왼쪽으로 상태 변경
            hasRotated = true;
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            directionState++; // 오른쪽으로 상태 변경
            hasRotated = true;
        }

        if (hasRotated)
        {
            // directionState 값을 0~3 사이로 유지
            directionState = (directionState % 4 + 4) % 4;
            // 상태에 따라 목표 회전 각도를 계산
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