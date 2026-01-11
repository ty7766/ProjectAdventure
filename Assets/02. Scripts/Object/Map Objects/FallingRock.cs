using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FallingRock : SpecialObject
{
    //--- Settings ---//
    [Header("화산탄 속성 설정")]
    [SerializeField] 
    private int _damageAmount = 1;

    [SerializeField] 
    private float _lifeTime = 5.0f;

    [SerializeField] 
    private float _rotateSpeed = 200f;

    //--- Components ---//
    private Rigidbody _rb;

    //--- Unity Methods ---//
    protected override void Awake()
    {
        base.Awake();
        GetAdditionalComponents();
    }

    private void Start()
    {
        ReserveDestroyAfterLifetime();
        ApplyInitialAngularVelocity();
    }

    private void Update()
    {
        UpdateRotation();
    }

    protected override void ApplyEffect(GameObject player)
    {
        ApplyPlayerDamage(player);
    }

    protected override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);
        DestroyIfCollidedWithEnvironment(other);
    }

    //--- Private Methods ---//
    private void GetAdditionalComponents()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void UpdateRotation()
    {
        transform.Rotate(Vector3.right * _rotateSpeed * Time.deltaTime);
    }

    private void ApplyInitialAngularVelocity()
    {
        _rb.angularVelocity = Random.insideUnitSphere * 10f;
    }

    private void ReserveDestroyAfterLifetime()
    {
        Destroy(gameObject, _lifeTime);
    }

    private void DestroyIfCollidedWithEnvironment(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            //TODO : 추후 이펙트 추가
            Destroy(gameObject);
        }
    }

    private void ApplyPlayerDamage(GameObject player)
    {
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            Debug.Log("[화산탄] 플레이어 명중!");
            playerController.TakeDamage(_damageAmount);
        }
    }
}