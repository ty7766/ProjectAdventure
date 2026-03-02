using UnityEngine;
using System.Collections;

public class VFXReturnToPool : MonoBehaviour
{
    private const float DEFAULT_RETURN_TIME = 2.0f;

    private VFXType _myType;
    private ParticleSystem _particleSystem;
    private Coroutine _returnCoroutine;
    private WaitForSeconds _defaultWait;

    private void Awake()
    {
        _particleSystem = GetComponent<ParticleSystem>();
        _defaultWait = new WaitForSeconds(DEFAULT_RETURN_TIME);
    }

    /// <summary>
    /// 생성 시 Manager가 Type을 알려주는 메소드
    /// </summary>
    /// <param name="type">VFX타입</param>
    public void Setup(VFXType type)
    {
        _myType = type;
    }

    private void OnEnable()
    {
        if (_returnCoroutine != null)
        {
            StopCoroutine(_returnCoroutine);
        }

        _returnCoroutine = StartCoroutine(ReturnWhenFinished());
    }
    private void OnDisable()
    {
        if(_returnCoroutine != null)
        {
            StopCoroutine(_returnCoroutine);
            _returnCoroutine = null;
        }
    }

    private IEnumerator ReturnWhenFinished()
    {
        if (_particleSystem != null)
        {
            while (_particleSystem.IsAlive(true))
            {
                yield return null;
            }
        }
        else
        {
            yield return _defaultWait;
        }

        //파티클 끝나면 반납
        VFXManager.Instance.ReturnToPool(_myType, gameObject);
        _returnCoroutine = null;
    }
}