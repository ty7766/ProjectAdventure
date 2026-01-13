using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EffectObject : SpecialObject
{
    [Header("아이템 효과")]
    [SerializeField] private StatusEffect _itemEffect;

    protected override void ApplyEffect(GameObject player)
    {
        ApplyItemEffectTo(player);
    }

    private void ApplyItemEffectTo(GameObject player)
    {
        if (_itemEffect != null)
        {
           if(player.TryGetComponent<PlayerEffectController>(out var effectController))
           {
               effectController.ApplyEffect(_itemEffect);
           }
           else
           {
               CustomDebug.LogWarning($"PlayerEffectController component is missing on {player.name}", player);
            }
        }
        else
        {
            CustomDebug.LogWarning($"itemEffect is missing in {name}", this);
        }
    }
}
