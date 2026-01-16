using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build;

public class EmissionOptions
{
    public const string EmissionKeyword = "_EMISSION";
    public const string EmissionColor = "_EmissionColor";
}

public class Sphinx : MonoBehaviour
{
    [Header("연결 요소")]
    [SerializeField]
    private GameObject _rockPrefab;
    [SerializeField]
    private GameObject _warningPrefab;
    [SerializeField]
    private Renderer _sphinxEyeRenderer;

    [Header("기믹 패턴 설정")]
    [SerializeField]
    private int _rockCount = 5;
    [SerializeField]
    private float _spawnRadius = 10f;
    [SerializeField]
    private float _dropHeight = 15f;
    [SerializeField]
    private float _patternInterval = 5;

    [Header("기믹 타이밍 설정")]
    [SerializeField]
    private float _eyeGlowDuration = 2.0f;
    [SerializeField]
    private float _warningDuration = 1.5f;
    [SerializeField]
    private float _randomDurationMin = 0.1f;
    [SerializeField]
    private float _randomDurationMax = 0.3f;

    [Header("눈 이펙트")]
    [SerializeField]
    [ColorUsage(true, true)]        //HDR 컬러 사용을 위해 추가
    private Color _glowColor = Color.red * 5f;
    private Color _originalEyeColor;
    private Material _eyeMaterial;

    private void Start()
    {
        CheckMaterialInstanceForSphinxEyes();
        StartCoroutine(PatternLoop());
    }

    private void CheckMaterialInstanceForSphinxEyes()
    {
        if(_sphinxEyeRenderer != null)
        {
            _eyeMaterial = _sphinxEyeRenderer.material;
            _eyeMaterial.EnableKeyword(EmissionOptions.EmissionKeyword);
            _originalEyeColor = _eyeMaterial.GetColor(EmissionOptions.EmissionColor);
        }
    }

    private IEnumerator PatternLoop()
    {
        while(true)
        {
            yield return new WaitForSeconds(_patternInterval);

            //눈 이펙트 활성
            SetEyeEmission(true);
            yield return new WaitForSeconds(_eyeGlowDuration);
            SetEyeEmission(false);

            //공격 시작
            StartCoroutine(SpawnRocksSequance());
        }
    }

    private IEnumerator SpawnRocksSequance()
    {
        //돌 개수만큼 낙하 지점 생성
        for (int i = 0; i < _rockCount; i++)
        {
            Vector3 targetPos = GetRandomPosition();
            SpawnWarning(targetPos);
            StartCoroutine(DropRockRoutine(targetPos));

            yield return new WaitForSeconds(Random.Range(_randomDurationMin, _randomDurationMax));
        }
    }

    private IEnumerator DropRockRoutine(Vector3 targetPos)
    {
        yield return new WaitForSeconds(_warningDuration);

        Vector3 spawnPos = targetPos + Vector3.up * _dropHeight;

        Instantiate(_rockPrefab, spawnPos, Random.rotation);
    }

    private void SpawnWarning(Vector3 targetPos)
    {
        if (_warningPrefab == null) return;

        Vector3 warningPos = targetPos + Vector3.up * 0.05f;

        GameObject marker = Instantiate(_warningPrefab, warningPos, Quaternion.Euler(90, 0, 0));

        GroundWarning groundWarningScript = marker.GetComponent<GroundWarning>();

        if (groundWarningScript != null)
        {
            groundWarningScript.Activate(_warningDuration);
        }
    }

    private void SetEyeEmission(bool isGlowing)
    {
        if(_eyeMaterial == null)
        {
            return;
        }

        Color targetColor = isGlowing ? _glowColor : _originalEyeColor;
        _eyeMaterial.SetColor(EmissionOptions.EmissionColor, targetColor);
    }

    private Vector3 GetRandomPosition()
    {
        Vector2 randomCircle = Random.insideUnitCircle * _spawnRadius;
        Vector3 pos = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

        pos.y = 0;

        return pos;
    }
}
