using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MovingRing : MonoBehaviour
{
    [Header("풀링 설정")]
    [SerializeField]
    private PoolObjectType _objectType = PoolObjectType.MovingRing;

    private float _speed;
    private float _maxDistance;
    private float _movedDistance;
    private Vector3 _moveDirection;
    private Rigidbody _rigidbody;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (_movedDistance >= _maxDistance)
        {
            ReturnToPool();
            return;
        }
        float step = _speed * Time.fixedDeltaTime;

        _rigidbody.MovePosition(_rigidbody.position + _moveDirection * step);

        _movedDistance += step;
    }


    /// <summary>
    /// 움직이는 링의 속성을 초기화하는 함수 (MovingRingSpawner.cs에서 속성 할당)
    /// </summary>
    /// <param name="speed">링 속도</param>
    /// <param name="maxDistance">링이 이동할 거리</param>
    public void InitializeForRingAttributs(float speed, float maxDistance, Vector3 direction)
    {
        _speed = speed;
        _maxDistance = maxDistance;
        _moveDirection = direction;
        _movedDistance = 0f;
    }

    /// <summary>
    /// 링을 풀에 반납하는 함수
    /// </summary>
    public void ReturnToPool()
    {
        //플레이어가 타고 있으면 내리게 함
        if (transform.childCount > 0)
        {
            transform.DetachChildren();
        }

        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnObject(_objectType, this.gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(transform);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(null);
        }
    }
}
