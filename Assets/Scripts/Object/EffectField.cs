using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EffectField : MonoBehaviour
{
    [Header("플레이어 효과 컨트롤러")]
    [SerializeField] private PlayerEffectController _playerEffectController;
    [Header("효과")]
    [SerializeField] private StatusEffect _statusEffect;

    private void OnTriggerEnter(Collider foreign)
    {
        if (foreign.TryGetComponent<PlayerEffectController>(out var controller))
        {
            // 들어올 땐 무한 지속 모드로 시작
            controller.StartPermanentEffect(_statusEffect);
        }
    }

    private void OnTriggerExit(Collider foreign)
    {
        if (foreign.TryGetComponent<PlayerEffectController>(out var controller))
        {
            // 나갈 땐 무한 지속 해제
            controller.StopPermanentEffect(_statusEffect);
        }
    }
}
