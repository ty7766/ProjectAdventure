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

    [Header("장판 위치 보정값")]
    [SerializeField]
    private Vector3 _warningOffset = new Vector3(0, 0.08f, 0);
    [SerializeField]
    private Vector3 _warningRotation = new Vector3(90, 0, 0);

    private SpriteRenderer _spriteRenderer;
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
        GameObject vfxObject = VFXManager.Instance.PlayVFX(vfxType, position, Quaternion.identity);

        if (vfxObject != null && vfxObject.TryGetComponent(out GroundWarning warning))
        {
            warning.InitializeTransform(position);
            warning.ActivateBlinkCoroutine(duration);
        }
    }

    private void ActivateBlinkCoroutine(float duration)
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

        Color baseColor = _spriteRenderer.color;
        baseColor.a = 0f;
        _spriteRenderer.color = baseColor;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float alpha = Mathf.Lerp(0.2f, 0.8f, Mathf.Abs(Mathf.Sin(timer * _blinkSpeed)));

            Color newColor = baseColor;
            newColor.a = alpha;
            _spriteRenderer.color = newColor;

            yield return null;
        }

        VFXManager.Instance.ReturnToPool(_vfxType, gameObject);
    }
}