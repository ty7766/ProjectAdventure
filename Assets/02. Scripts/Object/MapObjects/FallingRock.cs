using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(Rigidbody))]
public class FallingRock : SpecialObject
{
    //--- Settings ---//
    [Header("돌 속성 설정")]
    [SerializeField] 
    private int _damageAmount = 1;

    [SerializeField] 
    private float _lifeTime = 5.0f;

    [SerializeField]
    [Tooltip("회전 힘의 크기")]
    private float _tumbleForce = 10f;

    //--- Components ---//
    private Rigidbody _rigidbody;

    //--- Unity Methods ---//
    protected override void Awake()
    {
        base.Awake();
        GetAdditionalComponents();

        Assert.IsNotNull(_rigidbody, $"[FallingRock] '{name}'에 Rigidbody가 없습니다. (RequireComponent 확인 필요)");
    }

    private void Start()
    {
        ReserveDestroyAfterLifetime();
        ApplyInitialTumble();
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
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void ApplyInitialTumble()
    {
        _rigidbody.angularVelocity = Random.insideUnitSphere * _tumbleForce;
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
            CustomDebug.Log("[화산탄] 플레이어 명중!");
            playerController.TakeDamage(_damageAmount);
        }
    }
}