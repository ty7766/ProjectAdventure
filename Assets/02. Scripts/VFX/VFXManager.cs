using UnityEngine;
using System.Collections.Generic;

public class VFXManager : Singleton<VFXManager>
{
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

    private Dictionary<VFXType, Queue<GameObject>> _poolDictionary;
    private Dictionary<VFXType, GameObject> _vfxPrefabDictionary;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
        {
            return;
        }
        InitializePool();
    }

    /// <summary>
    /// 이펙트 생성
    /// </summary>
    /// <param name="type">VFX타입</param>
    /// <param name="position">이펙트 생성될 위치</param>
    public GameObject PlayVFX(VFXType type, Vector3 position, Quaternion rotation = default)
    {
        if(!_poolDictionary.ContainsKey(type))
        {
            CustomDebug.LogWarning($"VFXManager: {type} 타입의 풀이 존재하지 않습니다.");
            return null;
        }

        GameObject vfxObject =  GetOrCreateVFX(type);

        if (vfxObject == null)
        {
            return null;
        }

        vfxObject.transform.SetPositionAndRotation(position, rotation == default ? Quaternion.identity : rotation);
        vfxObject.SetActive(true);

        return vfxObject;
    }

    /// <summary>
    /// 이펙트를 풀에 반납
    /// </summary>
    /// <param name="type">VFX타입</param>
    /// <param name="vfxObject">반납할 이펙트</param>
    public void ReturnToPool(VFXType type, GameObject vfxObject)
    {
        if(!_poolDictionary.ContainsKey(type))
        {
            Destroy(vfxObject);
            return;
        }

        vfxObject.SetActive(false);
        _poolDictionary[type].Enqueue(vfxObject);
    }

    private GameObject GetOrCreateVFX(VFXType type)
    {
        if (_poolDictionary[type].Count == 0)
        {
            if (_vfxPrefabDictionary.TryGetValue(type, out GameObject prefab))
            {
                return CreateNewVFXObject(type, prefab);
            }

            CustomDebug.LogWarning($"VFXManager: {type}의 Prefab을 찾을 수 없습니다.");
            return null;
        }

        GameObject vfxObject = _poolDictionary[type].Dequeue();
        if(vfxObject == null)
        {
            return GetOrCreateVFX(type);
        }

        return vfxObject;
    }

    //설정된 개수만큼 미리 생성
    private void InitializePool()
    {
        _poolDictionary = new Dictionary<VFXType, Queue<GameObject>>();
        _vfxPrefabDictionary = new Dictionary<VFXType, GameObject>();

        foreach (var vfxData in _vfxList)
        {
            if (!ValidateVFXData(vfxData))
            {
                continue;
            }

            InitializeVFXType(vfxData);
        }
    }

    private bool ValidateVFXData(VFXData data)
    {
        if(_poolDictionary.ContainsKey(data.Type))
        {
            CustomDebug.LogWarning($"VFXManager: {data.Type}이 중복 등록되었습니다.");
            return false;
        }

        if(data.Prefab == null)
        {
            CustomDebug.LogWarning($"VFXManager: {data.Type}의 Prefab이 null입니다.");
            return false;
        }

        return true;
    }

    private void InitializeVFXType(VFXData data)
    {
        //Dictionary Init
        _poolDictionary[data.Type] = new Queue<GameObject>();
        _vfxPrefabDictionary[data.Type] = data.Prefab;

        for (int i = 0; i < data.PoolSize; i++)
        {
            GameObject newVfx = CreateNewVFXObject(data.Type, data.Prefab);
            _poolDictionary[data.Type].Enqueue(newVfx);
        }
    }

    private GameObject CreateNewVFXObject(VFXType type, GameObject prefab)
    {
        GameObject vfxObject = Instantiate(prefab, transform);

        SetupVFXReturnToScript(vfxObject, type);

        vfxObject.SetActive(false);

        return vfxObject;
    }

    private void SetupVFXReturnToScript(GameObject vfxObject, VFXType type)
    {
        if(vfxObject.TryGetComponent<VFXReturnToPool>(out var returnScript) == false)
        {
            returnScript = vfxObject.AddComponent<VFXReturnToPool>();
        }

        returnScript.Setup(type);
    }
}
