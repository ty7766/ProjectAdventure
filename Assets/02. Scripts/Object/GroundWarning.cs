using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class GroundWarning : MonoBehaviour
{
    [SerializeField] 
    private float _blinkSpeed = 10.0f;
    [SerializeField] 
    private SpriteRenderer _spriteRenderer;

    private Coroutine _blinkCoroutine;

    private void Awake()
    {
            _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Activate(float duration)
    {
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
        }
        _blinkCoroutine = StartCoroutine(BlinkAndReturn(duration));
    }

    private IEnumerator BlinkAndReturn(float duration)
    {
        float timer = 0f;
        Color originalColor = _spriteRenderer.color;

        // 초기화
        originalColor.a = 0f;
        _spriteRenderer.color = originalColor;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float alpha = Mathf.Lerp(0.2f, 0.8f, Mathf.Abs(Mathf.Sin(timer * _blinkSpeed)));

            Color newColor = originalColor;
            newColor.a = alpha;
            _spriteRenderer.color = newColor;

            yield return null;
        }

        VFXManager.Instance.ReturnToPool(VFXType.SphinxWarning, gameObject);
    }
}