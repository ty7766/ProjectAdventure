using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class GroundWarning : MonoBehaviour
{
    [Header("VFX 설정")]
    [SerializeField]
    private VFXType _vfxType = VFXType.SphinxWarning;

    [Header("장판 속성")]
    [SerializeField] 
    private float _blinkSpeed = 10.0f;
    [SerializeField] 
    private SpriteRenderer _spriteRenderer;

    [Header("장판 위치 보정값")]
    [SerializeField]
    private Vector3 _warningOffset = new Vector3(0, 0.08f, 0);
    [SerializeField]
    private Vector3 _warningRotation = new Vector3(90, 0, 0);

    private Coroutine _blinkCoroutine;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }


    /// <summary>
    /// sphinx가 돌을 떨어뜨리기 전 호출하는 이펙트 호출 함수
    /// </summary>
    /// <param name="vfxType">생성될 vfxType</param>
    /// <param name="position">생성될 위치</param>
    /// <param name="duration">지속 시간</param>
    public static void CreateGroundWarningEffects(VFXType vfxType, Vector3 position, float duration)
    {
        if (VFXManager.Instance == null)
        {
            return;
        }

        GameObject vfxObject = VFXManager.Instance.PlayVFX(vfxType, position, Quaternion.identity);

        if (vfxObject != null && vfxObject.TryGetComponent(out GroundWarning warning))
        {
            warning._vfxType = vfxType;
            warning.InitializeTransform(position);
            warning.ActivateBlinkCoroutine(duration);
        }
    }

    public void ActivateBlinkCoroutine(float duration)
    {
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
        }
        _blinkCoroutine = StartCoroutine(BlinkAndReturn(duration));
    }

    private void InitializeTransform(Vector3 centerPos)
    {
        transform.position = centerPos + _warningOffset;
        transform.rotation = Quaternion.Euler(_warningRotation);
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

        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.ReturnToPool(_vfxType, gameObject);
        }
    }
}