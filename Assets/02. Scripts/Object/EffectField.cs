using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EffectField : MonoBehaviour
{
    //--- Settings ---//
    [Header("효과")]
    [SerializeField] private StatusEffect _statusEffect;

    //--- Fields ---//
    private PlayerEffectController _cachedController;

    //--- Unity Methods ---//
    private void OnTriggerEnter(Collider foreign)
    {
        if (!IsCollidedWithPlayer(foreign))
        {
            return;
        }

        StartPermenantEffect(foreign);
    }

    private void OnTriggerExit(Collider foreign)
    {
        StopPermenantEffect();
    }

    //--- Private Methods ---//
    private void CacheController(Collider foreign)
    {
        if (!foreign.TryGetComponent<PlayerEffectController>(out _cachedController))
        {
            Debug.LogWarning($"PlayerEffectController component is missing on {foreign.gameObject.name}");
            this.gameObject.SetActive(false);
        }
    }

    private void StartPermenantEffect(Collider foreign)
    {
        if (_cachedController != null)
        {
            _cachedController.StartPermanentEffect(_statusEffect);
        }
        else
        {
            CacheController(foreign);
            _cachedController?.StartPermanentEffect(_statusEffect);
        }
    }

    private static bool IsCollidedWithPlayer(Collider foreign)
    {
        return foreign.CompareTag("Player");
    }

    private void StopPermenantEffect()
    {
        if (_cachedController != null)
        {
            _cachedController.StopPermanentEffect(_statusEffect);
        }
    }
}
