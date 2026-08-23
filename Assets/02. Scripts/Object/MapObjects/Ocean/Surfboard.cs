using UnityEngine;
using System.Collections;

public class Surfboard : MonoBehaviour, IMapTransitionHandler
{
    [Header("경로 설정")]
    [Tooltip("서핑보드가 이동할 지점들을 순서대로 넣으세요.")]
    [SerializeField]
    private Transform[] _wayPoints;

    [Header("보드 움직임 설정")]
    [SerializeField]
    private float _moveSpeed = 3.0f;
    [SerializeField]
    private float _turnSpeed = 2.0f;
    [SerializeField]
    private float _reachThreshold = 0.1f;   //도착 판정 거리

    [Header("종점 감속 설정")]
    [SerializeField, Tooltip("양 끝 지점으로부터 이 거리 안에 들어오면 감속을 시작합니다. 0이면 감속하지 않습니다.")]
    private float _easeDistance = 3.0f;
    [SerializeField, Range(0.05f, 1f), Tooltip("양 끝 지점에서의 최저 속도 비율")]
    private float _minSpeedRatio = 0.25f;

    private int _currentIndex = 0;
    private bool _isMovingForward = true;
    private bool _shouldSnapRotation = false;   //즉시 회전 변수

    private Transform _attachedPlayer;

    private void Awake()
    {
        if (_wayPoints == null)
        {
            CustomDebug.LogError($"[Surfboard] '{name}'에 WayPoints 배열이 할당되지 않았습니다. 스크립트를 비활성화합니다.");
            this.enabled = false;
            return;
        }

        if (_wayPoints.Length < 2)
        {
            CustomDebug.LogError($"[Surfboard] '{name}'의 WayPoints는 최소 2개 이상이어야 합니다. 스크립트를 비활성화합니다.");
            this.enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        _isMovingForward = true;
        _shouldSnapRotation = false;
        _attachedPlayer = null;
        InitializePositionAndRotation();
    }

    private void FixedUpdate()
    {
        ProcessSurfboardMovement();
    }

    private void InitializePositionAndRotation()
    {
        // 시작 위치 설정
        if (_wayPoints[0] != null)
        {
            transform.position = _wayPoints[0].position;
        }

        // 시작하자마자 1번 포인트를 바라보게 '즉시' 회전
        if (_wayPoints[1] != null)
        {
            Vector3 startDirection = _wayPoints[1].position - _wayPoints[0].position;
            startDirection.y = 0; // 수평 유지

            if (startDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(startDirection);
            }

            _currentIndex = 1;
        }
    }
    private void ProcessSurfboardMovement()
    {
        // 현재 목표 지점이 유효하지 않으면 인덱스 갱신 시도
        if (_wayPoints[_currentIndex] == null)
        {
            UpdateTargetIndex();
            return;
        }

        Transform targetPoint = _wayPoints[_currentIndex];

        MoveTowardsTarget(targetPoint);
        RotateTowardsTarget(targetPoint);
        CheckArrival(targetPoint);
    }
    private void MoveTowardsTarget(Transform targetPoint)
    {
        float currentSpeed = CalculateEasedSpeed();
        transform.position = Vector3.MoveTowards(transform.position, targetPoint.position, currentSpeed * Time.fixedDeltaTime);
    }

    //양 끝 지점에 가까울수록 감속하고, 멀어질수록 원래 속도로 복구
    private float CalculateEasedSpeed()
    {
        if (_easeDistance <= 0f)
        {
            return _moveSpeed;
        }

        float sqrDistance = GetSqrDistanceToNearestEndPoint();
        if (sqrDistance < 0f)
        {
            return _moveSpeed;
        }

        float ratio = Mathf.Clamp01(Mathf.Sqrt(sqrDistance) / _easeDistance);
        return _moveSpeed * Mathf.Lerp(_minSpeedRatio, 1f, Mathf.SmoothStep(0f, 1f, ratio));
    }

    //양 끝 웨이포인트 중 가까운 쪽까지의 제곱 거리, 둘 다 유효하지 않으면 -1
    private float GetSqrDistanceToNearestEndPoint()
    {
        float nearestSqrDistance = -1f;

        Transform firstPoint = _wayPoints[0];
        if (firstPoint != null)
        {
            nearestSqrDistance = (transform.position - firstPoint.position).sqrMagnitude;
        }

        Transform lastPoint = _wayPoints[_wayPoints.Length - 1];
        if (lastPoint != null)
        {
            float sqrDistance = (transform.position - lastPoint.position).sqrMagnitude;
            if (nearestSqrDistance < 0f || sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
            }
        }

        return nearestSqrDistance;
    }

    private void RotateTowardsTarget(Transform targetPoint)
    {
        Vector3 direction = targetPoint.position - transform.position;
        direction.y = 0;

        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // 시작점과 끝점(반환점)인 경우 부드러운 회전 대신 즉시 반전
        if (_shouldSnapRotation)
        {
            SnapRotationWithPlayerDetachment(targetRotation);
        }
        else
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _turnSpeed * Time.fixedDeltaTime);
        }
    }

    //보트가 즉시 반전될 때 플레이어가 보트 중앙을 기준으로 회전하므로
    //플레이어가 순간이동하는 것을 방지하기 위함
    private void SnapRotationWithPlayerDetachment(Quaternion targetRotation)
    {
        _shouldSnapRotation = false;

        if (_attachedPlayer == null)
        {
            transform.rotation = targetRotation;
            return;
        }

        //회전 전에 플레이어를 분리해야 보드 중심을 기준으로 플레이어 위치가 반전되지 않음
        //SetParent는 월드 좌표를 유지하므로 재부착 후에도 플레이어는 서 있던 자리를 지킴
        _attachedPlayer.SetParent(null);
        transform.rotation = targetRotation;
        _attachedPlayer.SetParent(transform);
    }

    private void CheckArrival(Transform targetPoint)
    {
        float distance = Vector3.Distance(transform.position, targetPoint.position);
        if (distance <= _reachThreshold)
        {
            UpdateTargetIndex();
        }
    }

    private void UpdateTargetIndex()
    {
        if(_isMovingForward)
        {
            //정방향 이동
            if(_currentIndex < _wayPoints.Length - 1)
            {
                _currentIndex++;
            }
            else
            {
                _isMovingForward = false;
                _currentIndex--;
                _shouldSnapRotation = true;
            }
        }
        else
        {
            //역방향 이동
            if(_currentIndex > 0)
            {
                _currentIndex--;
            }
            else
            {
                _isMovingForward = true;
                _currentIndex++;
                _shouldSnapRotation = true;
            }
        }
    }

    // IMapTransitionHandler
    public void OnMapExitStart() { }
    public void OnMapEnterStart() => this.enabled = false; // FixedUpdate 중단 → 맵과 함께 이동
    public void OnMapEnterEnd() => this.enabled = true;    // OnEnable → InitializePositionAndRotation

    //플레이어 탑승
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(transform);
            _attachedPlayer = collision.transform;
        }
    }

    //플레이어 내림
    private void OnCollisionExit(Collision collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            if(collision.transform.parent == transform)
            {
                collision.transform.SetParent(null);
            }

            if (_attachedPlayer == collision.transform)
            {
                _attachedPlayer = null;
            }
        }
    }
}
