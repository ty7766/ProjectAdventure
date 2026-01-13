using UnityEngine;
using System.Collections;

public class VFXReturnToPool : MonoBehaviour
{
    private VFXType _myType;
    private ParticleSystem _particleSystem;

    private void Awake()
    {
        _particleSystem = GetComponent<ParticleSystem>();
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
        StartCoroutine(CheckIfAlive());
    }

    private IEnumerator CheckIfAlive()
    {
        //파티클이 재생중이면 대기
        if (_particleSystem != null)
        {
            yield return new WaitWhile(() => _particleSystem.IsAlive(true));
        }
        else
        {
            yield return new WaitForSeconds(2.0f);
        }

        //파티클 끝나면 반납
        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.ReturnToPool(_myType, this.gameObject);
        }
    }
}
