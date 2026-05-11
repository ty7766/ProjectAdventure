using System.Collections.Generic;
using UnityEngine;

public class BuffSlotPool
{
    private readonly Transform _container;
    private readonly BuffSlotView _slotPrefab;
    private readonly List<BuffSlotView> _slots = new List<BuffSlotView>();

    public BuffSlotPool(Transform container, BuffSlotView slotPrefab)
    {
        _container = container;
        _slotPrefab = slotPrefab;
    }

    /// <summary>
    /// 활성 효과 목록에 맞춰 슬롯을 갱신, 기존 슬롯을 재사용
    /// </summary>
    /// <param name="activeEffects"></param>
    public void Refresh(List<ActiveEffect> activeEffects)
    {
        if (activeEffects == null)
        {
            HideAll();
            return;
        }

        EnsureCapacity(activeEffects.Count);

        for (int i = 0; i < activeEffects.Count; i++)
        {
            BuffSlotView slot = _slots[i];
            
            if (!slot.gameObject.activeSelf)
            {
                slot.gameObject.SetActive(true);
            }
            slot.Setup(activeEffects[i]);
        }

        for (int i = activeEffects.Count; i < _slots.Count; i++)
        {
            BuffSlotView slot = _slots[i];
            if (slot.gameObject.activeSelf)
            {
                slot.Clear();
                slot.gameObject.SetActive(false);
            }
        }
    }

    private void EnsureCapacity(int required)
    {
        while(_slots.Count < required)
        {
            BuffSlotView newSlot = Object.Instantiate(_slotPrefab, _container);
            newSlot.gameObject.SetActive(false);
            _slots.Add(newSlot);
        }
    }

    private void HideAll()
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            BuffSlotView slot = _slots[i];
            if (slot.gameObject.activeSelf)
            {
                slot.Clear();
                slot.gameObject.SetActive(false);
            }
        }
    }
}
