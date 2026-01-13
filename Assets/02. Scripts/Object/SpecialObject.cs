using System.Collections;
using UnityEngine;
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Renderer))]
public abstract class SpecialObject : MonoBehaviour
{
    //--- Components ---//
    protected Collider _col;
    protected Renderer _rend;

    //--- Fields ---//
    [Header("속성")]
    [SerializeField] private bool _isRespawnable = false;
    [SerializeField] private float _respawnTime = 5f;

    //--- Properties ---//
    protected bool IsRespawnable => _isRespawnable;
    protected float RespawnTime => _respawnTime;


    //--- Unity Methods ---//
    protected virtual void Awake()
    {
        GetComponents();
    }

    private void GetComponents()
    {
        _col = GetComponent<Collider>();
        _rend = GetComponent<Renderer>();
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ApplyEffect(other.gameObject);
            if(IsRespawnable)
            {
                DisableObjectAndRespawnAfter();
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    private void DisableObjectAndRespawnAfter()
    {
        _col.enabled = false;
        _rend.enabled = false;
        StartCoroutine(RespawnAfterSeconds(RespawnTime));
    }

    protected IEnumerator RespawnAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        _col.enabled = true;
        _rend.enabled = true;
    }

    protected abstract void ApplyEffect(GameObject player);
}