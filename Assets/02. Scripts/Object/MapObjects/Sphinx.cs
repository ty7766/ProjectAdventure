using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Sphinx : SpawnedObjectManager<Transform>
{
    [Header("오브젝트 풀링 설정")]
    [SerializeField]
    private PoolObjectType _objectType = PoolObjectType.SphinxFallingRock;

    

    [Header("눈 이펙트 및 위치 설정")]
    [SerializeField]
    private GameObject _eyeEffectPrefab;
    [SerializeField]
    private Transform[] _eyes;

    [Header("기믹 패턴 설정")]
    [SerializeField]
    private int _rockCount = 5;
    [SerializeField]
    private float _spawnRadius = 10f;
    [SerializeField]
    private float _dropHeight = 15f;
    [SerializeField]
    private float _patternInterval = 5f;
    [SerializeField]
    private Transform _spawnCenterPoint;

    [Header("기믹 타이밍 설정")]
    [SerializeField]
    private float _eyeGlowDuration = 2.0f;
    [SerializeField]
    private float _warningDuration = 1.5f;
    [SerializeField]
    private float _randomDurationMin = 0.1f;
    [SerializeField]
    private float _randomDurationMax = 0.3f;

    private List<GameObject> _spawnedEyeEffects = new List<GameObject>();
    private WaitForSeconds _waitPatternInterval;
    private WaitForSeconds _waitEyeGlow;
    private WaitForSeconds _waitWarning;

    private void Awake()
    {
        CachingCoroutines();
        SetupEyeEffects();
    }

    //SpawnedObjectManager 상속
    protected override IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return _waitPatternInterval;

            // 눈 이펙트 활성
            SetEyeEffectActive(true);
            yield return _waitEyeGlow;
            SetEyeEffectActive(false);

            // 공격 시작
            yield return StartCoroutine(SpawnRocksSequence());
        }
    }
    //SpawnedObjectManager 상속
    protected override void ReturnObjectToPool(Transform rock)
    {
        UnregisterObject(rock);
        if (ObjectPoolManager.Instance != null && rock != null)
        {
            ObjectPoolManager.Instance.ReturnObject(_objectType, rock.gameObject);
        }
    }

    private void CachingCoroutines()
    {
        _waitPatternInterval = new WaitForSeconds(_patternInterval);
        _waitEyeGlow = new WaitForSeconds(_eyeGlowDuration);
        _waitWarning = new WaitForSeconds(_warningDuration);
    }

    private void SetupEyeEffects()
    {
        if (_eyeEffectPrefab == null || _eyes == null || _eyes.Length == 0)
        {
            Debug.LogError("스핑크스 눈 이펙트 설정이 누락되었습니다!");
            return;
        }

        foreach (Transform anchor in _eyes)
        {
            if (anchor != null)
            {
                GameObject effect = Instantiate(_eyeEffectPrefab, anchor.position, anchor.rotation, anchor);
                effect.SetActive(false);
                _spawnedEyeEffects.Add(effect);
            }
        }
    }

    private void SetEyeEffectActive(bool isActive)
    {
        foreach (GameObject effect in _spawnedEyeEffects)
        {
            if (effect != null)
            {
                effect.SetActive(isActive);
            }
        }
    }

    private IEnumerator SpawnRocksSequence()
    {
        //돌 개수만큼 낙하 지점 생성
        for (int i = 0; i < _rockCount; i++)
        {
            Vector3 targetPosition = GetRandomPosition();
            GroundWarning.CreateGroundWarningEffects(VFXType.SphinxWarning, targetPosition, _warningDuration);
            StartCoroutine(DropRockRoutine(targetPosition));

            yield return new WaitForSeconds(Random.Range(_randomDurationMin, _randomDurationMax));
        }
    }

    private IEnumerator DropRockRoutine(Vector3 targetPosition)
    {
        yield return _waitWarning;

        Vector3 spawnPosition = targetPosition + Vector3.up * _dropHeight;

        if (ObjectPoolManager.Instance != null)
        {
            GameObject rockObject = ObjectPoolManager.Instance.SpawnObject(_objectType, spawnPosition, Random.rotation);

            if (rockObject != null)
            {
                RegisterObject(rockObject.transform);
            }
        }
    }
    private Vector3 GetRandomPosition()
    {
        Vector2 circle = Random.insideUnitCircle * _spawnRadius;

        Vector3 centerPosition = (_spawnCenterPoint != null) ? _spawnCenterPoint.position : transform.position;

        Vector3 finalPosition = centerPosition + new Vector3(circle.x, 0, circle.y);

        finalPosition.y = centerPosition.y;

        return finalPosition;
    }
}
