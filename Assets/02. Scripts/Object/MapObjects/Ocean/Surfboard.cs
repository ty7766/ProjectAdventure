using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Surfboard : MonoBehaviour
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
        }
    }

    private void Start()
    {
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
        Vector3 newPosition = Vector3.MoveTowards(transform.position, targetPoint.position, _moveSpeed * Time.fixedDeltaTime);
        transform.position = newPosition;
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
        if (_attachedPlayer != null)
        {
            _attachedPlayer.SetParent(null);
        }

        transform.rotation = targetRotation;

        if (_attachedPlayer != null)
        {
            _attachedPlayer.SetParent(transform);
        }

        _shouldSnapRotation = false;
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

    //플레이어 탑승
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(this.transform);
            _attachedPlayer = collision.transform;
        }
    }

    //플레이어 내림
    private void OnCollisionExit(Collision collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            if(collision.transform.parent == this.transform)
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
