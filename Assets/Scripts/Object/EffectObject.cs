using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EffectObject : SpecialObject
{
    [Header("아이템 효과")]
    [SerializeField] private StatusEffect _itemEffect;

    protected override void ApplyEffect(GameObject player)
    {
        if (_itemEffect == null)
        {
            Debug.LogWarning($"itemEffect is missing in {this.gameObject.name}");
            return;
        }
        if(player.TryGetComponent<PlayerEffectController>(out var controller))
        {
            controller.ApplyEffect(_itemEffect);
        }
    }
}
