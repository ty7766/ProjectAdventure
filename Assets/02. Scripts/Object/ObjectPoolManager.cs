using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

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

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializePool();
        }
        else
        {
            Destroy(gameObject);
        }
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
        if (!_poolDictionary.ContainsKey(type))
        {
            CustomDebug.LogWarning($"[ObjectPoolManager] {type} 타입의 풀이 없습니다.");
            return null;
        }

        if (_poolDictionary[type].Count == 0)
        {
            CreateNewObject(type);
        }

        GameObject obj = _poolDictionary[type].Dequeue();
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);

        return obj;
    }

    /// <summary>
    /// 오브젝트를 풀에 반납
    /// </summary>
    /// <param name="type">오브젝트 타입</param>
    /// <param name="obj">기믹 오브젝트</param>
    public void ReturnObject(PoolObjectType type, GameObject obj)
    {
        obj.SetActive(false);
        _poolDictionary[type].Enqueue(obj);
    }

    private void InitializePool()
    {
        foreach (var poolInfo in _poolList)
        {
            if (!_poolDictionary.ContainsKey(poolInfo.Type))
            {
                _poolDictionary.Add(poolInfo.Type, new Queue<GameObject>());
            }

            for (int i = 0; i < poolInfo.PoolSize; i++)
            {
                CreateNewObject(poolInfo.Type, poolInfo.Prefab);
            }
        }
    }

    private void CreateNewObject(PoolObjectType type, GameObject prefab = null)
    {
        // prefab이 null이면 리스트에서 찾음
        if (prefab == null)
        {
            var info = _poolList.Find(x => x.Type == type);
            prefab = info.Prefab;
        }

        if (prefab != null)
        {
            GameObject obj = Instantiate(prefab, transform);
            obj.SetActive(false);
            _poolDictionary[type].Enqueue(obj);
        }
    }
}