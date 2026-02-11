using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SpawnedObjectManager<T> : MonoBehaviour where T : Component
{
    // 공통 리스트
    protected List<T> _spawnedObjects = new List<T>();

    // 공통 코루틴 변수
    protected Coroutine _spawnCoroutine;

    protected virtual void OnEnable()
    {
        if (_spawnCoroutine == null)
        {
            _spawnCoroutine = StartCoroutine(SpawnRoutine());
        }
    }

    protected virtual void OnDisable()
    {
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }

        foreach (var obj in _spawnedObjects)
        {
            if (obj != null && obj.gameObject.activeInHierarchy)
            {
                ReturnObjectToPool(obj); // 구체적인 반납 방식은 자식이 결정
            }
        }

        // 3. 리스트 초기화
        _spawnedObjects.Clear();
    }

    /// <summary>
    /// 생성된 오브젝트를 관리 리스트에 등록 (자식 클래스에서 호출)
    /// </summary>
    protected void RegisterObject(T obj)
    {
        _spawnedObjects.Add(obj);
    }

    /// <summary>
    /// 실제 스폰 로직 (자식마다 다르므로 추상 메서드)
    /// </summary>
    protected abstract IEnumerator SpawnRoutine();

    /// <summary>
    /// 오브젝트 반납 로직 (자식마다 ReturnToPool 사용법이 다를 수 있으므로 추상 메서드)
    /// </summary>
    protected abstract void ReturnObjectToPool(T obj);
}
