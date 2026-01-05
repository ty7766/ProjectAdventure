using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EffectField : MonoBehaviour
{
    [Header("효과")]
    [SerializeField] private StatusEffect _statusEffect;
    private PlayerEffectController _cachedController;

    private void OnTriggerEnter(Collider foreign)
    {
        // 플레이어가 아니면 바로 리턴 (매우 빠름)
        if (!foreign.CompareTag("Player"))
        {
            return;
        }

        // 캐시가 없을 때만 TryGetComponent 호출
        if (_cachedController == null)
        {
            if (!foreign.TryGetComponent<PlayerEffectController>(out _cachedController))
            {
                return;
            }
        }

        // 들어올 땐 무한 지속 모드로 시작
        _cachedController.StartPermanentEffect(_statusEffect);
    }

    private void OnTriggerExit(Collider foreign)
    {
        if (_cachedController != null)
        {
            _cachedController.StopPermanentEffect(_statusEffect);
        }
    }
}
