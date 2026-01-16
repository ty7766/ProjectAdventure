using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Sphinx : MonoBehaviour
{
    [Header("연결 요소")]
    [SerializeField]
    private GameObject _rockPrefab;

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

    private void Start()
    {
        SetupEyeEffects();
        StartCoroutine(PatternLoop());
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
    private IEnumerator PatternLoop()
    {
        while(true)
        {
            yield return new WaitForSeconds(_patternInterval);

            //눈 이펙트 활성
            SetEyeEffectActive(true);
            yield return new WaitForSeconds(_eyeGlowDuration);
            SetEyeEffectActive(false);

            //공격 시작
            StartCoroutine(SpawnRocksSequence());
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
            Vector3 targetPos = GetRandomPosition();
            SpawnWarningEffect(targetPos);
            StartCoroutine(DropRockRoutine(targetPos));

            yield return new WaitForSeconds(Random.Range(_randomDurationMin, _randomDurationMax));
        }
    }

    private void SpawnWarningEffect(Vector3 position)
    {
        Vector3 spawnPos = position + Vector3.up * 0.05f;
        Quaternion rot = Quaternion.Euler(90, 0, 0);

        //VFXManager에게 요청
        GameObject warningObj = VFXManager.Instance.PlayVFX(VFXType.SphinxWarning, spawnPos, rot);

        // 가져온 오브젝트에서 스크립트 꺼내서 실행
        if (warningObj != null)
        {
            GroundWarning warningScript = warningObj.GetComponent<GroundWarning>();
            if (warningScript != null)
            {
                warningScript.Activate(_warningDuration);
            }
        }
    }

    private IEnumerator DropRockRoutine(Vector3 targetPos)
    {
        yield return new WaitForSeconds(_warningDuration);

        Vector3 spawnPos = targetPos + Vector3.up * _dropHeight;

        Instantiate(_rockPrefab, spawnPos, Random.rotation);
    }

    private Vector3 GetRandomPosition()
    {
        Vector2 circle = Random.insideUnitCircle * _spawnRadius;
        return transform.position + new Vector3(circle.x, 0, circle.y);
    }
}
