using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(Rigidbody))]
public class FallingRock : SpecialObject
{
    [Header("VFX 설정")]
    [SerializeField]
    private VFXType _vfxType = VFXType.FallingRockHit;

    [Header("돌 속성 설정")]
    [SerializeField] 
    private int _damageAmount = 1;

    [SerializeField] 
    private float _lifeTime = 5.0f;

    [SerializeField]
    [Tooltip("회전 힘의 크기")]
    private float _tumbleForce = 10f;

    private Rigidbody _rigidbody;
    public float LifeTime => _lifeTime;

    public event System.Action<FallingRock> OnDestroyed;

    protected override void Awake()
    {
        base.Awake();

        _rigidbody = GetComponent<Rigidbody>();
        Assert.IsNotNull(_rigidbody, $"[FallingRock] '{name}'에 Rigidbody가 없습니다. (RequireComponent 확인 필요)");
    }

    private void Start()
    {
        ApplyInitialTumble();
    }

    private void OnDestroy()
    {
        OnDestroyed?.Invoke(this);
    }

    protected override void ApplyEffect(GameObject player)
    {
        VFXManager.Instance.PlayVFX(_vfxType, transform.position, Quaternion.identity);
        SoundManager.Instance.PlaySFX_3D(SoundType.SFX_FallingRock, transform.position);
        ApplyPlayerDamage(player);
    }

    protected override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);
        DestroyIfCollidedWithEnvironment(other);
    }

    private void ApplyInitialTumble()
    {
        _rigidbody.angularVelocity = Random.insideUnitSphere * _tumbleForce;
    }

    private void DestroyIfCollidedWithEnvironment(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            VFXManager.Instance.PlayVFX(_vfxType, transform.position, Quaternion.identity);
            SoundManager.Instance.PlaySFX_3D(SoundType.SFX_FallingRock, transform.position);
            Destroy(gameObject);
        }
    }

    private void ApplyPlayerDamage(GameObject player)
    {
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.TakeDamage(_damageAmount);
        }
    }
}