using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class SnowBall : MonoBehaviour
{
    [Header("충돌 설정")]
    [SerializeField]
    private int _damageAmount = 1;

    [Header("VFX 설정")]
    [SerializeField]
    private VFXType _destroyVFXType = VFXType.SnowBallHit;

    [Header("오브젝트 풀링 설정")]
    [SerializeField]
    private PoolObjectType _objectType = PoolObjectType.SnowBall;
    [SerializeField]
    private float _lifeTime = 10f;

    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.Sleep();
            _rb.WakeUp();
        }
        StartCoroutine(ActivateSnowBallRoutine());
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
        if(collision.gameObject.TryGetComponent<PlayerController>(out var playerController))
        {
            playerController.TakeDamage(_damageAmount);
        }

        if(VFXManager.Instance != null)
        {
            ContactPoint contact = collision.GetContact(0);
            VFXManager.Instance.PlayVFX(_destroyVFXType, contact.point, Quaternion.LookRotation(contact.normal));
        }

        ReturnToPool();
    }

    private IEnumerator ActivateSnowBallRoutine()
    {
        yield return new WaitForSeconds(_lifeTime);
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        StopAllCoroutines();

        if(ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnObject(_objectType, this.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
