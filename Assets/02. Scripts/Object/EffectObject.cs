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
            player.GetComponent<PlayerEffectController>()?.ApplyEffect(_itemEffect);
        }
        else
        {
            Debug.LogWarning($"itemEffect is missing in {this.gameObject.name}");
        }
    }
}
