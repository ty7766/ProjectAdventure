using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class SnowBall : MonoBehaviour
{
    [Header("VFX 설정")]
    [SerializeField]
    private VFXType _destroyVFXType = VFXType.SnowBallHit;

    [Header("오브젝트 풀링 설정")]
    [SerializeField]
    private PoolObjectType _objectType = PoolObjectType.SnowBall;

    [Header("스노우볼 속성 설정")]
    [SerializeField]
    private int _damageAmount = 1;
    [SerializeField]
    private float _lifeTime = 10f;

    private Rigidbody _rigidBody;
    private WaitForSeconds _lifeTimeInterval;

    private void Awake()
    {
        _rigidBody = GetComponent<Rigidbody>();
        _lifeTimeInterval = new WaitForSeconds(_lifeTime);
    }

    private void OnEnable()
    {
        InitializeSnowBallRigidBody();
        StartCoroutine(ActivateSnowBallRoutine());
    }

    /// <summary>
    /// SnowBall을 Pool에 반납
    /// </summary>
    public void ReturnToPool()
    {
        StopAllCoroutines();

        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnObject(_objectType, this.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeSnowBallRigidBody()
    {
        if (_rigidBody != null)
        {
            _rigidBody.linearVelocity = Vector3.zero;
            _rigidBody.angularVelocity = Vector3.zero;
            _rigidBody.Sleep();
            _rigidBody.WakeUp();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            HandlePlayerHit(collision);
        }
    }

    private void HandlePlayerHit(Collision collision)
    {
        //데미지 적용
        if(collision.gameObject.TryGetComponent<PlayerController>(out var playerController))
        {
            playerController.TakeDamage(_damageAmount);
        }

        //VFX 재생
        ContactPoint contact = collision.GetContact(0);
        VFXManager.Instance.PlayVFX(_destroyVFXType, contact.point, Quaternion.LookRotation(contact.normal));

        ReturnToPool();
    }

    private IEnumerator ActivateSnowBallRoutine()
    {
        yield return _lifeTimeInterval;
        ReturnToPool();
    }
}
