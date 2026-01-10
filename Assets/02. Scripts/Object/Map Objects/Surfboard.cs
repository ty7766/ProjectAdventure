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

    private void Start()
    {
        if (_wayPoints == null || _wayPoints.Length == 0)
        {
            enabled = false;
            return;
        }

        if (_wayPoints[0] != null)
        {
            transform.position = _wayPoints[0].position;
        }

        //시작하자마자 1번 포인트를 바라보게 '즉시' 회전
        if (_wayPoints.Length > 1 && _wayPoints[1] != null)
        {
            Vector3 startDir = _wayPoints[1].position - _wayPoints[0].position;
            startDir.y = 0; // 수평 유지

            if (startDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(startDir);
            }

            // 0번에는 이미 도착해 있으니, 목표를 바로 1번으로 설정
            _currentIndex = 1;
        }
    }

    private void FixedUpdate()
    {
        if (_wayPoints == null || _wayPoints.Length < 2)
        {
            return;
        }

        MoveSurfboard();
    }

    private void MoveSurfboard()
    {
        //현재 목표지점이 없는 경우 다음 인덱스로 복구 시도
        if (_wayPoints[_currentIndex] == null)
        {
            HandleIndexChange();
            return;
        }

        Transform targetPoint = _wayPoints[_currentIndex];

        //1. 다음 포인트로 이동
        Vector3 newPosition = Vector3.MoveTowards(transform.position, targetPoint.position, _moveSpeed * Time.fixedDeltaTime);
        transform.position = newPosition;

        //2. 다음 포인트가 현재 포인트와 같은 선상이 아닌경우 회전
        Vector3 direction = targetPoint.position - transform.position;
        direction.y = 0;

        if(direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            //시작점과 끝점인 경우 회전 모션없이 즉시 보드 반전
            if(_shouldSnapRotation)
            {
                Transform player = null;
                foreach(Transform child in transform)
                {
                    if(child.CompareTag("Player"))
                    {
                        player = child;
                        break; 
                    }
                }

                if(player != null)
                {
                    player.SetParent(null);
                }
                transform.rotation = targetRotation;
                if (player != null)
                {
                    player.SetParent(transform);
                }
                _shouldSnapRotation = false;
            }
            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _turnSpeed * Time.fixedDeltaTime);
            }
        }

        //3. 도착 판정
        float distance = Vector3.Distance(transform.position, targetPoint.position);
        if(distance <= _reachThreshold)
        {
            HandleIndexChange();
        }
    }

    //다음 목표 계산
    private void HandleIndexChange()
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
        }
    }
}
