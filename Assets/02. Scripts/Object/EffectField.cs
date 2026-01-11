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

        StartPermanantEffect(foreign);
    }

    private void OnTriggerExit(Collider foreign)
    {
        if (!IsCollidedWithPlayer(foreign))
        {
            return;
        }

        StopPermanantEffect();
    }

    //--- Private Methods ---//
    private void CacheController(Collider foreign)
    {
        if (!foreign.TryGetComponent<PlayerEffectController>(out _cachedController))
        {
            Debug.LogWarning($"PlayerEffectController component is missing on {foreign.gameObject.name}");
        }
    }

    private void StartPermanantEffect(Collider foreign)
    {
        if (_cachedController == null)
        {
            CacheController(foreign);
        }

        _cachedController?.StartPermanentEffect(_statusEffect);
    }

    private static bool IsCollidedWithPlayer(Collider foreign)
    {
        return foreign.CompareTag("Player");
    }

    private void StopPermanantEffect()
    {
        if (_cachedController != null)
        {
            _cachedController.StopPermanentEffect(_statusEffect);
        }
    }
}
