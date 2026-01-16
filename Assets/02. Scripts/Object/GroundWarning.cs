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

    private void Awake()
    {
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        //색상 초기화
        if(_spriteRenderer != null)
        {
            Color color = _spriteRenderer.color;
            color.a = 0f;
            _spriteRenderer.color = color;
        }
    }

    /// <summary>
    /// 오브젝트가 낙하할 지점에 깜빡거림을 표시해줌
    /// </summary>
    /// <param name="duration">주기</param>
    public void Activate(float duration)
    {
        StopAllCoroutines();
        StartCoroutine(BlinkRoutine(duration));
    }

    private IEnumerator BlinkRoutine(float duration)
    {
        float timer = 0f;
        Color originalColor = _spriteRenderer.color;

        while(timer < duration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(0.2f, 0.8f, (Mathf.Abs(timer * _blinkSpeed)));

            Color newColor = originalColor;
            newColor.a = alpha;
            _spriteRenderer.color = newColor;

            yield return null;
        }

        //VFXManager의 Pool로 되돌아감
        //연출시간이 가변적이므로 VFXReturnToPool 사용X
        gameObject.SetActive(false);
    }
}
