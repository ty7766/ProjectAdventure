using UnityEngine;
using System.Collections.Generic;

public class VFXManager : MonoBehaviour
{
    private static VFXManager _instance;

    public static VFXManager Instance
    {
        get
        {
            return _instance;
        }
    }

    [System.Serializable]
    public struct VFXData
    {
        public VFXType Type;
        public GameObject Prefab;
        public int PoolSize;
    }

    [Header("이펙트 등록 (Inspector")]
    [SerializeField]
    private List<VFXData> _vfxList;

    private Dictionary<VFXType, Queue<GameObject>> _poolDict = new Dictionary<VFXType, Queue<GameObject>>();

    private void Awake()
    {
        if(_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializePool();
    }

    /// <summary>
    /// 이펙트 생성
    /// </summary>
    /// <param name="type">VFX타입</param>
    /// <param name="position">이펙트 생성될 위치</param>
    public void PlayVFX(VFXType type, Vector3 position)
    {
        if(!_poolDict.ContainsKey(type))
        {
            return;
        }

        //대기열 비었으면 추가 생성
        if (_poolDict[type].Count == 0)
        {
            var data = _vfxList.Find(x => x.Type == type);
            if(data.Prefab != null)
            {
                CreateNewObject(type, data.Prefab);
            }
            else
            {
                Clog.LogWarning($"VFXManager: 프리팹 타입 {type} 이 null 입니다. 새 오브젝트를 생성할 수 없습니다.");
                return;
            }
        }


        GameObject obj = _poolDict[type].Dequeue();
        obj.transform.position = position;
        obj.SetActive(true);
    }

    /// <summary>
    /// 이펙트를 풀에 반납
    /// </summary>
    /// <param name="type">VFX타입</param>
    /// <param name="obj">반납할 이펙트</param>
    public void ReturnToPool(VFXType type, GameObject obj)
    {
        obj.SetActive(false);
        _poolDict[type].Enqueue(obj);
    }

    //설정된 개수만큼 미리 생성
    private void InitializePool()
    {
        foreach(var data in _vfxList)
        {
            if(!_poolDict.ContainsKey(data.Type))
            {
                _poolDict.Add(data.Type, new Queue<GameObject>());
            }

            for(int i = 0; i < data.PoolSize; i++)
            {
                CreateNewObject(data.Type, data.Prefab);
            }
        }
    }

    private GameObject CreateNewObject(VFXType type, GameObject prefab)
    {
        GameObject obj = Instantiate(prefab, transform);

        var returnScript = obj.GetComponent<VFXReturnToPool>();
        if(returnScript == null)
        {
            returnScript = obj.AddComponent<VFXReturnToPool>();
        }

        returnScript.Setup(type);

        obj.SetActive(false);
        _poolDict[type].Enqueue(obj);
        return obj;
    }
}
