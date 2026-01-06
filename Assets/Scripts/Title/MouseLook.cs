using UnityEngine;

[RequireComponent(typeof(Animator))]
public class MouseLook : MonoBehaviour
{
    [Header("설정값")]
    [SerializeField]
    [Tooltip("1에 가까울 수록 확실히 바라봄")]
    private float _lookWeight = 1.0f;
    [SerializeField]
    [Tooltip("시선 이동의 부드러움")]
    private float _smoothTime = 0.1f;   
    [SerializeField]
    [Tooltip("최대 시선 각도 (목이 뒤로 꺾이지 않도록 제한)")]
    private float _maxAngle = 90f;     
    [SerializeField]
    [Tooltip("캐릭터가 바라볼 가상의 평면의 거리")]
    private float _planeDistance = 2.0f;

    private Animator _anim;
    private Camera _mainCamera;
    private Vector3 _currentLookPos;
    private Vector3 _targetLookPos;
    private Vector3 _velocity;

    void Start()
    {
        _currentLookPos = transform.position + transform.forward;
    }

    private void Awake()
    {
        _anim = GetComponent<Animator>();
        if(Camera.main == null)
        {
            Debug.LogError("메인 카메라를 찾을수가 없습니다. 오브젝트를 비활성화합니다.");
            gameObject.SetActive(false);
        }
        else
        {
            _mainCamera = Camera.main;
        }
    }

    void Update()
    {
        LookAtMouse();
    }

    private void LookAtMouse()
    {
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        Vector3 planePoint = transform.position + (-_mainCamera.transform.forward * _planeDistance);
        Plane plane = new Plane(-_mainCamera.transform.forward, planePoint);

        float distance;
        if (plane.Raycast(ray, out distance))
        {
            Vector3 hitPoint = ray.GetPoint(distance);
            _targetLookPos = hitPoint;
        }

        _currentLookPos = Vector3.SmoothDamp(_currentLookPos, _targetLookPos, ref _velocity, _smoothTime);
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (_anim)
        {
            Vector3 direction = _currentLookPos - transform.position;
            float angle = Vector3.Angle(transform.forward, direction);

            if (angle < _maxAngle)
            {
                _anim.SetLookAtWeight(_lookWeight, 0.3f, 0.8f, 1.0f, 0.5f);
                _anim.SetLookAtPosition(_currentLookPos);
            }
            else
            {
                _anim.SetLookAtWeight(0);
            }
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(_targetLookPos, 0.2f); // 0.2f는 공 크기

        if (_mainCamera != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, -_mainCamera.transform.forward * 2.0f);
        }

        Gizmos.color = Color.yellow;
        if (_mainCamera != null)
            Gizmos.DrawLine(_mainCamera.transform.position, _targetLookPos);
    }
#endif
}