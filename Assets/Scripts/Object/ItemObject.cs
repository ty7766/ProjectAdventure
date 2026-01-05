using UnityEngine;

public class ItemObject : SpecialObject
{

    [Header("플레이어 효과 컨트롤러")]
    [SerializeField] private PlayerEffectController _playerEffectController;
    [Header("아이템 효과")]
    [SerializeField] private StatusEffect _itemEffect;

    protected override void ApplyEffect(GameObject player)
    {
        if (_itemEffect == null)
        {
            Debug.LogWarning($"itemEffect is missing in {this.gameObject.name}");
            return;
        }
        _playerEffectController.ApplyEffect(_itemEffect);
    }
}
