using System.Collections;
using UnityEngine;

public abstract class SpecialObject : MonoBehaviour
{
    //--- Components ---//
    protected Collider col;
    protected Renderer rend;

    //--- Fields ---//
    [Header("속성")]
    [SerializeField] private bool isRespawnable = false;
    [SerializeField] private float respawnTime = 5f;


    //--- Unity Methods ---//
    protected virtual void Start()
    {
        if(!TryGetComponent<Collider>(out col))
        {
            Debug.LogWarning("Collider Component not found on SpecialObject");
        }
        if(!TryGetComponent<Renderer>(out rend))
        {
            Debug.LogWarning("Renderer Component not found on SpecialObject");
        }
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        // 태그가 Player인 녀석만 반응하도록 필터링
        if (other.CompareTag("Player"))
        {
            // 1. 자식 클래스에서 정의한 실제 효과 발동!
            ApplyEffect(other.gameObject);
            if(isRespawnable)
            {
                // 2. 오브젝트 비활성화
                if(col != null && rend != null)
                {
                    col.enabled = false;
                    rend.enabled = false;
                    // 3. 일정 시간 후 재생성
                    StartCoroutine(RespawnAfterSeconds(respawnTime));
                }
                else
                {
                    Debug.LogWarning("Collider or Renderer is null in SpecialObject. Destroy Instead!");
                    Destroy(gameObject); //리스폰 로직 대신 파괴 수행
                }

            }
            else
            {
                // 오브젝트 파괴
                Destroy(gameObject);
            }
        }
    }

    protected IEnumerator RespawnAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        col.enabled = true;
        rend.enabled = true;
    }

    protected abstract void ApplyEffect(GameObject player);
}