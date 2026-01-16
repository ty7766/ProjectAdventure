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
    private Renderer _sphinxEyeRenderer;

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

    [Header("눈 이펙트")]
    [SerializeField]
    [ColorUsage(true, true)]        //HDR 컬러 사용을 위해 추가
    private Color _glowColor = Color.red * 5f;

    private MaterialPropertyBlock _materialPropertyBlock;
    private int _emissionColorID;

    private void Awake()
    {
        _emissionColorID = Shader.PropertyToID(EmissionOptions.EmissionColor);
        _materialPropertyBlock = new MaterialPropertyBlock();
    }
    private void Start()
    {
        StartCoroutine(PatternLoop());
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
            StartCoroutine(SpawnRocksSequence());
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

    private void SetEyeEmission(bool isGlowing)
    {
        if(_sphinxEyeRenderer == null)
        {
            return;
        }

        _sphinxEyeRenderer.GetPropertyBlock(_materialPropertyBlock);
        Color targetColor = isGlowing ? _glowColor : Color.black;
        _materialPropertyBlock.SetColor(_emissionColorID, targetColor);
        _sphinxEyeRenderer.SetPropertyBlock(_materialPropertyBlock);
    }

    private Vector3 GetRandomPosition()
    {
        Vector2 circle = Random.insideUnitCircle * _spawnRadius;
        return transform.position + new Vector3(circle.x, 0, circle.y);
    }
}
