using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

public enum SceneTransitionStyle
{
    /// <summary>화면 전체가 어두워졌다 밝아짐 (같은 자리 재시작처럼 가벼운 전환)</summary>
    Fade,
    /// <summary>원형으로 닫히고 열림 (다른 곳으로 이동하는 전환). 플레이어가 있으면 플레이어를 중심으로</summary>
    Iris,
}

/// <summary>
/// 전환 한 번의 설정. 덮을 때와 걷을 때의 연출을 따로 고를 수 있다.
/// </summary>
public struct SceneTransitionOptions
{
    public SceneTransitionStyle Cover;
    public SceneTransitionStyle Reveal;
    /// <summary>덮인 동안 보여줄 타이틀 카드 (비우면 카드 없음)</summary>
    public string CardTitle;
    public string CardSubtitle;

    /// <summary>스테이지 이동: 원형으로 닫히고 스테이지 카드를 보여준 뒤 새 시작 지점을 중심으로 열림</summary>
    public static SceneTransitionOptions StageChange(int stageNumber) => new SceneTransitionOptions
    {
        Cover = SceneTransitionStyle.Iris,
        Reveal = SceneTransitionStyle.Iris,
        CardTitle = $"STAGE {stageNumber}",
    };

    /// <summary>같은 스테이지 재시작: 짧게 어두워졌다 밝아짐</summary>
    public static SceneTransitionOptions Retry => new SceneTransitionOptions
    {
        Cover = SceneTransitionStyle.Fade,
        Reveal = SceneTransitionStyle.Fade,
    };

    /// <summary>타이틀 복귀: 플레이어를 중심으로 닫히고, 타이틀은 페이드로 드러남 (타이틀 UI 등장 연출과 겹치도록)</summary>
    public static SceneTransitionOptions ToTitle => new SceneTransitionOptions
    {
        Cover = SceneTransitionStyle.Iris,
        Reveal = SceneTransitionStyle.Fade,
    };
}

/// <summary>
/// 씬/스테이지 전환 연출. 모든 씬 위에 그려지는 전용 오버레이 캔버스를 가지며 씬 로드에도 유지된다.
/// - 처음 사용할 때 Resources/SceneTransition 프리팹으로 자동 생성되므로 씬에 미리 배치할 필요가 없다.
/// - 모든 연출은 언스케일드 시간으로 동작한다. (클리어/일시정지로 timeScale = 0인 상태에서 시작될 수 있음)
/// - 덮인 동안에는 입력을 막고, 씬 로드 직후 프레임이 안정될 때까지 기다렸다가 걷는다.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    private const string RESOURCE_PATH = "SceneTransition";
    private const int STABLE_FRAME_COUNT = 3;
    private const float STABLE_FRAME_DELTA = 0.05f;
    private const float MAX_STABLE_WAIT = 2f;

    //--- Settings ---//
    [Header("References")]
    [SerializeField] private RectTransform _canvasRect;
    [SerializeField] private CanvasGroup _rootGroup;
    [SerializeField, Tooltip("페이드 및 완전히 닫힌 상태를 담당하는 단색 화면")] private Image _solid;
    [SerializeField, Tooltip("가운데가 뚫린 링 (테두리 두께로 구멍 크기를 제어)")] private ProceduralImage _iris;
    [SerializeField] private CanvasGroup _card;
    [SerializeField] private TextMeshProUGUI _cardTitle;
    [SerializeField] private TextMeshProUGUI _cardSubtitle;
    [SerializeField] private TextMeshProUGUI _loadingDots;

    [Header("Fade")]
    [SerializeField, Range(0.1f, 1f)] private float _fadeCoverDuration = 0.28f;
    [SerializeField, Range(0.1f, 1f)] private float _fadeRevealDuration = 0.4f;

    [Header("Iris")]
    [SerializeField, Range(0.1f, 1.5f)] private float _irisCloseDuration = 0.5f;
    [SerializeField, Range(0.1f, 1.5f)] private float _irisOpenDuration = 0.55f;
    [SerializeField, Tooltip("완전히 닫히기 직전/열리기 직후 잠깐 머무는 구멍 반지름 (캔버스 단위)")] private float _irisFocusRadius = 110f;
    [SerializeField, Range(0f, 0.5f)] private float _irisFocusHold = 0.12f;
    [SerializeField, Tooltip("플레이어 발밑이 아닌 몸통 쪽을 중심으로 삼기 위한 월드 오프셋")] private Vector3 _playerFocusOffset = new Vector3(0f, 1f, 0f);

    [Header("Card")]
    [SerializeField, Range(0f, 2f), Tooltip("카드가 보이는 최소 시간 (로딩이 빨라도 읽을 수 있게)")] private float _minCardTime = 0.7f;

    //--- Fields ---//
    private static SceneTransitionManager s_instance;
    private bool _isBusy;
    private bool _isCovered;
    private float _holeRadius;
    private Tween _dotsTween;

    //--- Properties ---//
    /// <summary>전환이 진행 중이면 true (덮는 중 ~ 다 걷힐 때까지)</summary>
    public static bool IsBusy => s_instance != null && s_instance._isBusy;

    /// <summary>화면이 덮여 있으면 true. 걷히기 시작하는 순간 false (등장 연출을 걷힘과 겹쳐 시작하고 싶을 때)</summary>
    public static bool IsScreenCovered => s_instance != null && s_instance._isCovered;

    //--- Unity Methods ---//
    private void Awake()
    {
        if (s_instance != null && s_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        s_instance = this;
        DontDestroyOnLoad(gameObject);
        SetHiddenImmediate();
    }

    private void OnDestroy()
    {
        if (s_instance == this)
        {
            s_instance = null;
        }
        _dotsTween?.Kill();
    }

    //--- Public Methods ---//
    /// <summary>
    /// 같은 씬 안에서의 전환 (스테이지 교체, 재시작 등). 화면이 덮인 순간 onCovered를 실행한다.
    /// </summary>
    /// <returns>다른 전환이 진행 중이라 요청이 무시되면 false</returns>
    public static bool TryRun(SceneTransitionOptions options, Action onCovered)
    {
        SceneTransitionManager manager = GetOrCreate();
        if (manager == null)
        {
            onCovered?.Invoke();
            return true;
        }
        if (manager._isBusy)
        {
            return false;
        }
        manager.StartCoroutine(manager.RunRoutine(options, onCovered, null));
        return true;
    }

    /// <summary>
    /// 씬 전환. 화면이 덮이면 비동기로 씬을 로드하고, 프레임이 안정된 뒤 걷는다.
    /// </summary>
    /// <param name="onCovered">씬 로드 직전에 실행 (timeScale 복구 등)</param>
    public static bool TryLoadScene(string sceneName, SceneTransitionOptions options, Action onCovered = null)
    {
        SceneTransitionManager manager = GetOrCreate();
        if (manager == null)
        {
            onCovered?.Invoke();
            SceneManager.LoadScene(sceneName);
            return true;
        }
        if (manager._isBusy)
        {
            return false;
        }
        manager.StartCoroutine(manager.RunRoutine(options, onCovered, sceneName));
        return true;
    }

    //--- Private Methods ---//
    private static SceneTransitionManager GetOrCreate()
    {
        if (s_instance != null)
        {
            return s_instance;
        }

        var prefab = Resources.Load<SceneTransitionManager>(RESOURCE_PATH);
        if (prefab == null)
        {
            CustomDebug.LogWarning($"[SceneTransitionManager] Resources/{RESOURCE_PATH} 프리팹이 없어 연출 없이 전환합니다.");
            return null;
        }
        Instantiate(prefab);
        return s_instance;
    }

    private IEnumerator RunRoutine(SceneTransitionOptions options, Action onCovered, string sceneName)
    {
        _isBusy = true;
        _rootGroup.blocksRaycasts = true;
        bool hasCard = !string.IsNullOrEmpty(options.CardTitle);

        // 1) 덮기
        yield return Cover(options.Cover);
        _isCovered = true;
        float coveredAt = Time.unscaledTime;
        if (hasCard)
        {
            ShowCard(options);
        }

        // 2) 덮인 상태에서 전환 작업
        onCovered?.Invoke();
        if (!string.IsNullOrEmpty(sceneName))
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
            while (load != null && !load.isDone)
            {
                yield return null;
            }
        }
        // 씬 로드/스테이지 교체 직후의 프레임 스파이크가 걷는 연출을 잡아먹지 않도록 대기
        yield return WaitForStableFrames();

        if (hasCard)
        {
            while (Time.unscaledTime - coveredAt < _minCardTime)
            {
                yield return null;
            }
            yield return HideCard();
        }

        // 3) 걷기
        _isCovered = false;
        yield return Reveal(options.Reveal);

        SetHiddenImmediate();
        _isBusy = false;
    }

    //--- Cover / Reveal ---//
    private IEnumerator Cover(SceneTransitionStyle style)
    {
        if (style == SceneTransitionStyle.Fade)
        {
            ShowSolid(0f);
            yield return _solid.DOFade(1f, _fadeCoverDuration).SetEase(Ease.InOutSine).SetUpdate(true).WaitForCompletion();
            yield break;
        }

        // 원형: 화면을 다 덮는 구멍에서 시작해 초점 주변으로 좁혀지고, 잠깐 머문 뒤 완전히 닫힘
        float fullRadius = PrepareIris(FindFocusPoint());
        SetHoleRadius(fullRadius);
        Sequence close = DOTween.Sequence().SetUpdate(true)
            .Append(DOTween.To(() => _holeRadius, SetHoleRadius, _irisFocusRadius, _irisCloseDuration).SetEase(Ease.InOutCubic))
            .AppendInterval(_irisFocusHold)
            .Append(DOTween.To(() => _holeRadius, SetHoleRadius, 0f, 0.16f).SetEase(Ease.InQuad));
        yield return close.WaitForCompletion();
        ShowSolid(1f);
    }

    private IEnumerator Reveal(SceneTransitionStyle style)
    {
        if (style == SceneTransitionStyle.Fade)
        {
            ShowSolid(1f);
            yield return _solid.DOFade(0f, _fadeRevealDuration).SetEase(Ease.OutSine).SetUpdate(true).WaitForCompletion();
            yield break;
        }

        // 원형: 새 초점에서 작게 톡 열렸다가(Back Out) 잠깐 머문 뒤 가속하며 화면 밖까지 열림(Cubic In)
        float fullRadius = PrepareIris(FindFocusPoint());
        SetHoleRadius(0f);
        Sequence open = DOTween.Sequence().SetUpdate(true)
            .Append(DOTween.To(() => _holeRadius, SetHoleRadius, _irisFocusRadius, 0.28f).SetEase(Ease.OutBack))
            .AppendInterval(_irisFocusHold)
            .Append(DOTween.To(() => _holeRadius, SetHoleRadius, fullRadius, _irisOpenDuration).SetEase(Ease.InCubic));
        yield return open.WaitForCompletion();
    }

    //--- Iris ---//
    /// <summary>
    /// 링을 초점 위치로 옮기고 크기를 잡는다. 반환값은 화면을 완전히 드러내는 구멍 반지름.
    /// </summary>
    private float PrepareIris(Vector2 focus)
    {
        Rect canvas = _canvasRect.rect;
        float fullRadius = 0f;
        foreach (var corner in new[] { canvas.min, canvas.max, new Vector2(canvas.xMin, canvas.yMax), new Vector2(canvas.xMax, canvas.yMin) })
        {
            fullRadius = Mathf.Max(fullRadius, Vector2.Distance(focus, corner));
        }
        fullRadius += 8f;

        // 바깥 원은 구멍이 가장 클 때도 화면 모서리를 덮어야 한다
        float outerRadius = fullRadius + 8f;
        RectTransform irisRect = _iris.rectTransform;
        irisRect.anchoredPosition = focus;
        irisRect.sizeDelta = Vector2.one * outerRadius * 2f;

        _solid.enabled = false;
        _iris.enabled = true;
        return fullRadius;
    }

    private void SetHoleRadius(float radius)
    {
        _holeRadius = radius;
        float outerRadius = _iris.rectTransform.sizeDelta.x * 0.5f;
        // ProceduralImage는 테두리 0을 '꽉 찬 도형'으로 그리므로, 구멍이 사라지면 0으로 두어 원판이 되게 한다
        _iris.BorderWidth = radius <= 0.5f ? 0f : Mathf.Clamp(outerRadius - radius, 0f, outerRadius);
    }

    /// <summary>
    /// 원형 전환의 중심. 플레이어가 화면 안에 있으면 플레이어, 아니면 화면 중앙. (캔버스 중심 기준 좌표)
    /// </summary>
    private Vector2 FindFocusPoint()
    {
        Camera camera = Camera.main;
        PlayerController player = camera != null ? FindAnyObjectByType<PlayerController>() : null;
        if (player == null || !player.isActiveAndEnabled)
        {
            return Vector2.zero;
        }

        Vector3 screenPoint = camera.WorldToScreenPoint(player.transform.position + _playerFocusOffset);
        if (screenPoint.z <= 0f
            || !RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPoint, null, out Vector2 local)
            || !_canvasRect.rect.Contains(local))
        {
            return Vector2.zero;
        }
        return local;
    }

    //--- Card ---//
    private void ShowCard(SceneTransitionOptions options)
    {
        _card.DOKill();
        _cardTitle.DOKill();
        _card.gameObject.SetActive(true);
        _card.alpha = 0f;
        _cardTitle.text = options.CardTitle;
        _cardSubtitle.text = options.CardSubtitle ?? string.Empty;
        _cardSubtitle.gameObject.SetActive(!string.IsNullOrEmpty(options.CardSubtitle));

        // 카운트다운 "준비하세요"와 같은 결: 넓은 자간이 좁혀지며 떠오름
        RectTransform titleRect = _cardTitle.rectTransform;
        titleRect.DOKill();
        titleRect.localScale = Vector3.one * 1.04f;
        _cardTitle.characterSpacing = 40f;
        _card.DOFade(1f, 0.3f).SetEase(Ease.OutQuad).SetUpdate(true);
        DOTween.To(() => _cardTitle.characterSpacing, value => _cardTitle.characterSpacing = value, 4f, 0.7f).SetEase(Ease.OutCubic).SetUpdate(true).SetTarget(_cardTitle);
        titleRect.DOScale(1f, 0.7f).SetEase(Ease.OutCubic).SetUpdate(true);

        // 로딩 중 표시: 점이 천천히 숨쉬듯 깜빡임
        _dotsTween?.Kill();
        _loadingDots.alpha = 0.25f;
        _dotsTween = _loadingDots.DOFade(0.9f, 0.5f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
    }

    private IEnumerator HideCard()
    {
        _dotsTween?.Kill();
        yield return _card.DOFade(0f, 0.2f).SetEase(Ease.InQuad).SetUpdate(true).WaitForCompletion();
        _card.gameObject.SetActive(false);
    }

    //--- State ---//
    private void ShowSolid(float alpha)
    {
        _iris.enabled = false;
        _solid.enabled = true;
        Color color = _solid.color;
        color.a = alpha;
        _solid.color = color;
    }

    private void SetHiddenImmediate()
    {
        _isCovered = false;
        _rootGroup.blocksRaycasts = false;
        _solid.enabled = false;
        _iris.enabled = false;
        _card.gameObject.SetActive(false);
    }

    private IEnumerator WaitForStableFrames()
    {
        int stableFrames = 0;
        float waited = 0f;
        while (stableFrames < STABLE_FRAME_COUNT && waited < MAX_STABLE_WAIT)
        {
            yield return null;
            waited += Time.unscaledDeltaTime;
            stableFrames = Time.unscaledDeltaTime < STABLE_FRAME_DELTA ? stableFrames + 1 : 0;
        }
    }
}
