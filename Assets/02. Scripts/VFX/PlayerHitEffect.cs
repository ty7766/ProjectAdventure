using System.Collections;
using UnityEngine;

public class PlayerHitEffect : MonoBehaviour
{
    [Header("이펙트 설정")]
    [SerializeField]
    private int _blinkCount = 4; // 깜빡일 횟수
    [SerializeField]
    private float _blinkInterval = 0.1f; // 깜빡이는 간격

    private SkinnedMeshRenderer[] _skinnedMeshRenderers;
    private WaitForSeconds _waitForInterval;

    private void Awake()
    {
        _skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        _waitForInterval = new WaitForSeconds(_blinkInterval);
    }

    /// <summary>
    /// 플레이어 피격 이펙트 메소드
    /// </summary>
    public void PlayHitEffect()
    {
        StopAllCoroutines();
        StartCoroutine(BlinkRoutine());
    }

    private IEnumerator BlinkRoutine()
    {
        for (int i = 0; i < _blinkCount; i++)
        {
            ToggleRenderers(false);
            yield return _waitForInterval;

            ToggleRenderers(true);
            yield return _waitForInterval;
        }

        ToggleRenderers(true);
    }

    private void ToggleRenderers(bool isEnabled)
    {
        foreach (SkinnedMeshRenderer renderer in _skinnedMeshRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = isEnabled;
            }
        }
    }
}