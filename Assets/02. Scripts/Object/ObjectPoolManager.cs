using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : Singleton<ObjectPoolManager>
{
    [System.Serializable]
    public struct PoolInfo
    {
        public PoolObjectType Type;
        public GameObject Prefab;
        public int PoolSize;
    }

    [Header("오브젝트 등록")]
    [SerializeField]
    private List<PoolInfo> _poolList;

    private Dictionary<PoolObjectType, Queue<GameObject>> _poolDictionary = new Dictionary<PoolObjectType, Queue<GameObject>>();
    private Dictionary<PoolObjectType, GameObject> _prefabDictionary = new Dictionary<PoolObjectType, GameObject>();

    protected override void Awake()
    {
        base.Awake();   //싱글톤 Awake 실행
        if(Instance != this)
        {
            return;
        }
        InitializePool();
    }

    /// <summary>
    /// 오브젝트를 풀에서 가져오기
    /// </summary>
    /// <param name="type">오브젝트 타입</param>
    /// <param name="position">오브젝트 포지션</param>
    /// <param name="rotation">오브젝트 회전</param>
    /// <returns></returns>
    public GameObject SpawnObject(PoolObjectType type, Vector3 position, Quaternion rotation)
    {
        if (!_poolDictionary.TryGetValue(type, out Queue<GameObject> pool))
        {
            CustomDebug.LogWarning($"[ObjectPoolManager] {type} 타입의 풀이 없습니다.");
            return null;
        }

        GameObject spawnedObject = AcquireFromPool(type, pool);
        if (spawnedObject == null)
        {
            return null;
        }
        spawnedObject.transform.position = position;
        spawnedObject.transform.rotation = rotation;
        spawnedObject.SetActive(true);

        return spawnedObject;
    }

    /// <summary>
    /// 오브젝트를 풀에 반납
    /// </summary>
    /// <param name="type">오브젝트 타입</param>
    /// <param name="obj">기믹 오브젝트</param>
    public void ReturnObject(PoolObjectType type, GameObject obj)
    {
        if (obj == null)
        {
            return;
        }
        if (!_poolDictionary.TryGetValue(type, out Queue<GameObject> pool))
        {
            CustomDebug.LogWarning($"[ObjectPoolManager] {type} 타입의 풀이 없습니다.");
            return;
        }
        obj.SetActive(false);
        pool.Enqueue(obj);
    }

    private void InitializePool()
    {
        foreach (var poolInfo in _poolList)
        {
            if (!_poolDictionary.ContainsKey(poolInfo.Type))
            {
                _poolDictionary.Add(poolInfo.Type, new Queue<GameObject>());
                _prefabDictionary.Add(poolInfo.Type, poolInfo.Prefab);
            }

            for (int i = 0; i < poolInfo.PoolSize; i++)
            {
                CreateNewObject(poolInfo.Type, poolInfo.Prefab);
            }
        }
    }

    private bool CreateNewObject(PoolObjectType type, GameObject prefab = null)
    {
        if (prefab == null)
        {
            _prefabDictionary.TryGetValue(type, out prefab);
        }

        if (prefab != null)
        {
            GameObject obj = Instantiate(prefab, transform);
            obj.SetActive(false);
            _poolDictionary[type].Enqueue(obj);
            return true;
        }
        else
        {
            CustomDebug.LogWarning($"[ObjectPoolManager] {type} 생성 실패(프리팹 누락 가능).");
            return false;
        }
    }

    private GameObject AcquireFromPool(PoolObjectType type, Queue<GameObject> pool)
    {
        while (pool.Count > 0)
        {
            GameObject candidate = pool.Dequeue();
            if (candidate != null)
            {
                return candidate;
            }
            //state null은 버리고 다음항목 시도
        }

        //풀이 비어 있으면 1개 새로 생성
        if(!CreateNewObject(type))
        {
            return null;
        }
        return pool.Dequeue();
    }
}