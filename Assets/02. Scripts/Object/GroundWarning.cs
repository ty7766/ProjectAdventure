using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class GroundWarning : MonoBehaviour
{
    [Header("연출 설정")]
    [SerializeField]
    private float _blinkSpeed = 10.0f;      //깜빡이는 속도
    [SerializeField]
    private SpriteRenderer _spriteRenderer;

    private bool _isActivate = false;

    private void Awake()
    {
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// 오브젝트가 낙하할 지점에 깜빡거림을 표시해줌
    /// </summary>
    /// <param name="duration">주기</param>
    public void Activate(float duration)
    {
        _isActivate = true;
        StartCoroutine(BlinkRoutine(duration));
    }

    private IEnumerator BlinkRoutine(float duration)
    {
        float timer = 0f;
        Color originalColor = _spriteRenderer.color;

        originalColor.a = 0f;
        _spriteRenderer.color = originalColor;

        while(timer < duration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(0.2f, 0.8f, (Mathf.Sin(timer * _blinkSpeed)));

            Color newColor = originalColor;
            newColor.a = alpha;
            _spriteRenderer.color = newColor;

            yield return null;
        }

        Destroy(gameObject);
    }
}
