using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD 아이콘(하트/보석) 증감 반응 연출. 상태 없이 트윈만 재생한다.
/// </summary>
public static class HUDIconFeedback
{
    /// <summary>
    /// 하트 증감 반응. 잃은 하트는 움찔하며 흔들리고, 회복한 하트는 작게 시작해 튕기며 차오른다.
    /// </summary>
    public static void AnimateHealthChange(IReadOnlyList<Image> hearts, int previousHealth, int currentHealth, float lostShakeStrength)
    {
        int count = hearts.Count;
        if (currentHealth < previousHealth)
        {
            for (int i = Mathf.Clamp(currentHealth, 0, count); i < Mathf.Clamp(previousHealth, 0, count); i++)
            {
                RectTransform heart = hearts[i].rectTransform;
                heart.DOComplete();
                heart.DOPunchScale(Vector3.one * -0.35f, 0.35f, 8, 0.5f).SetUpdate(true);
                heart.DOShakeAnchorPos(0.35f, lostShakeStrength, 20, 90f, false, true).SetUpdate(true);
            }
        }
        else
        {
            for (int i = Mathf.Clamp(previousHealth, 0, count); i < Mathf.Clamp(currentHealth, 0, count); i++)
            {
                RectTransform heart = hearts[i].rectTransform;
                heart.DOComplete();
                heart.localScale = Vector3.one * 0.4f;
                heart.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
            }
        }
    }

    /// <summary>아이콘이 한 번 튀어오르는 반응 (보석 획득 등)</summary>
    public static void PlayPunch(RectTransform icon, float punch)
    {
        icon.DOComplete();
        icon.DOPunchScale(Vector3.one * punch, 0.35f, 6, 0.5f).SetUpdate(true);
    }
}
