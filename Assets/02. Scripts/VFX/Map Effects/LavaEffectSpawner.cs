using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class LavaSurfaceSpawner : MonoBehaviour
{
    [Header("생성 설정")]
    [SerializeField] 
    private float _minInterval = 0.5f;
    [SerializeField] 
    private float _maxInterval = 2.0f;

    [Header("위치 보정")]
    [SerializeField]
    private float _surfaceYOffset = 0f;

    private BoxCollider _spawnArea;

    private void Awake()
    {
        _spawnArea = GetComponent<BoxCollider>();
        _spawnArea.isTrigger = true; // 물리 충돌 방지
    }

    private void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            float waitTime = Random.Range(_minInterval, _maxInterval);
            yield return new WaitForSeconds(waitTime);

            SpawnLavaEffect();
        }
    }

    private void SpawnLavaEffect()
    {
        Bounds bounds = _spawnArea.bounds;

        float randX = Random.Range(bounds.min.x, bounds.max.x);
        float randZ = Random.Range(bounds.min.z, bounds.max.z);

        float fixedY = bounds.center.y + _surfaceYOffset;

        Vector3 spawnPos = new Vector3(randX, fixedY, randZ);

        VFXManager.Instance.PlayVFX(VFXType.LavaPop, spawnPos, Quaternion.Euler(-90, 0, 0));
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;

        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
#endif
}