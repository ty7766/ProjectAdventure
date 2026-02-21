using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("따라갈 대상")]
    [SerializeField]
    private Transform _target;

    [Header("따라가는 속도")]
    [SerializeField]
    private float _smoothSpeed = 0.1f;

    [Header("카메라 고정 오프셋")]
    [SerializeField]
    private Vector3 _playerOffset;

    private void LateUpdate()
    {
        if (_target == null)
        {
            return;
        }

        Vector3 desiredPosition = _target.position + _playerOffset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, _smoothSpeed);
        transform.position = smoothedPosition;
    }

    [ContextMenu("현재 씬의 각도를 오프셋으로 저장")]
    private void SaveCurrentOffset()
    {
        if (_target != null)
        {
            _playerOffset = transform.position - _target.position;
        }
        else
        {
            CustomDebug.LogWarning("Target이 지정되지 않아 오프셋을 계산할 수 없습니다");
        }
    }
}