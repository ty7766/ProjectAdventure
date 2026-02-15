using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("따라갈 대상")]
    [SerializeField] 
    private Transform _target;

    [Header("따라가는 속도")]
    [SerializeField]
    private float _smoothSpeed = 0.1f;

    private Vector3 _playerOffset;

    void Start()
    {
        if (_target != null)
        {
            _playerOffset = transform.position - _target.position;
        }
    }

    void LateUpdate()
    {
        if (_target == null) return;

        Vector3 desiredPosition = _target.position + _playerOffset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, _smoothSpeed);
        transform.position = smoothedPosition;

        //카메라는 항상 플레이어를 주시하고 싶다면 아래 주석 해제
        // transform.LookAt(_target); 
    }
}