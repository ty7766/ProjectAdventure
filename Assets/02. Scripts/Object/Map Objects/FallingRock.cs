using UnityEngine;

public class FallingRock : SpecialObject
{
    [Header("화산탄 속성 설정")]
    [SerializeField] 
    private int _damageAmount = 1;
    [SerializeField] 
    private float _lifeTime = 5.0f;
    [SerializeField] 
    private float _rotateSpeed = 200f;

    private Rigidbody _rb;

    protected override void Start()
    {
        base.Start();

        if (!TryGetComponent<Rigidbody>(out _rb))
        {
            _rb = gameObject.AddComponent<Rigidbody>();
        }

        Destroy(gameObject, _lifeTime);

        //떨어질 때 랜덤한 방향으로 회전력 주기
        _rb.angularVelocity = Random.insideUnitSphere * 10f;
    }

    private void Update()
    {
        transform.Rotate(Vector3.right * _rotateSpeed * Time.deltaTime);
    }

    protected override void ApplyEffect(GameObject player)
    {
        // 1. 플레이어 컨트롤러 가져오기
        PlayerController playerController = player.GetComponent<PlayerController>();

        // 2. 플레이어가 맞으면 데미지 함수 호출
        if (playerController != null)
        {
            Debug.Log("[화산탄] 플레이어 명중!");
            playerController.TakeDamage(_damageAmount);
        }
    }

    protected override void OnTriggerEnter(Collider other)
    {
        // 1. 부모의 로직 먼저 실행
        base.OnTriggerEnter(other);

        // 2. 플레이어가 아닌 다른 물체(땅, 바닥 등)에 닿으면 즉시 파괴
        if (!other.CompareTag("Player"))
        {
            //추후 이펙트 추가
            Destroy(gameObject);
        }
    }
}