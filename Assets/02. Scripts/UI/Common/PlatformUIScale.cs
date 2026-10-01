using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 터치 UI 플랫폼에서 UI 요소를 PC 기준보다 살짝 크게 그린다.
/// - 기준(PC) 배치/크기는 프리팹 그대로 두고, 터치 플랫폼에서만 localScale을 배율만큼 키운다.
/// - 균일 스케일이므로 텍스트 레이아웃(줄바꿈/잘림)은 PC와 동일하게 유지된다.
/// - 키운 결과가 캔버스(화면) 밖으로 나가면 배율을 화면에 들어오는 최대치로 줄인다. (PC 크기 미만으로는 줄이지 않음)
/// 등장 연출이 localScale을 건드리는 대상에는 붙이지 말고, 그 부모 컨테이너에 붙인다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class PlatformUIScale : MonoBehaviour
{
    //--- Settings ---//
    [SerializeField, Range(1f, 1.5f), Tooltip("터치 플랫폼에서의 배율 (PC = 1)")]
    private float _touchScale = 1.2f;
    [SerializeField, Tooltip("배율을 키워도 캔버스 밖으로 나가지 않도록 제한")]
    private bool _fitInsideCanvas = true;
    [SerializeField, Tooltip("캔버스 가장자리와 유지할 최소 여백 (캔버스 단위)")]
    private float _canvasMargin = 16f;

    //--- Fields ---//
    private bool _isApplied;

    //--- Properties ---//
    public float AppliedScale { get; private set; } = 1f;

    //--- Unity Methods ---//
    private void Awake()
    {
        Apply();
    }

    //--- Public Methods ---//
    /// <summary>
    /// 배율을 적용한다. 여러 번 호출해도 한 번만 적용된다.
    /// (비활성 오브젝트는 Awake가 늦으므로, 연출 쪽에서 홈 상태를 캡처하기 전에 ApplyInChildren으로 먼저 적용한다)
    /// </summary>
    public void Apply()
    {
        if (_isApplied)
        {
            return;
        }
        _isApplied = true;

        if (!PlatformCapability.UseTouchUI || _touchScale <= 1f)
        {
            return;
        }

        float scale = _fitInsideCanvas ? ClampToCanvas(_touchScale) : _touchScale;
        AppliedScale = scale;
        transform.localScale *= scale;
    }

    public static void ApplyInChildren(Component root)
    {
        foreach (var platformScale in root.GetComponentsInChildren<PlatformUIScale>(true))
        {
            platformScale.Apply();
        }
    }

    //--- Private Methods ---//
    /// <summary>
    /// 피벗을 중심으로 커졌을 때 자식까지 포함한 영역이 캔버스 안에 머무는 최대 배율을 구한다.
    /// 런타임 캔버스 갱신 시점과 무관하도록 앵커/사이즈 값으로 직접 계산한다. (중간 부모의 스케일/회전은 1/0으로 가정)
    /// </summary>
    private float ClampToCanvas(float desired)
    {
        var rect = (RectTransform)transform;
        Canvas canvas = GetComponentInParent<Canvas>(true);
        if (canvas == null)
        {
            return desired;
        }
        canvas = canvas.rootCanvas;

        Rect canvasRect = new Rect(Vector2.zero, GetCanvasSize(canvas));
        Rect area = new Rect(canvasRect.xMin + _canvasMargin, canvasRect.yMin + _canvasMargin,
            canvasRect.width - _canvasMargin * 2f, canvasRect.height - _canvasMargin * 2f);

        Rect parentRect = ComputeRectInCanvas((RectTransform)rect.parent, canvas.transform, canvasRect);
        Rect selfRect = ComputeRect(rect, parentRect);
        Vector2 pivot = selfRect.min + Vector2.Scale(selfRect.size, rect.pivot);
        Rect bounds = selfRect;
        EncapsulateChildren(rect, selfRect, ref bounds);

        // 현재(PC) 스케일 기준 피벗으로부터의 거리
        Vector2 baseScale = rect.localScale;
        float minX = (bounds.xMin - pivot.x) * baseScale.x;
        float maxX = (bounds.xMax - pivot.x) * baseScale.x;
        float minY = (bounds.yMin - pivot.y) * baseScale.y;
        float maxY = (bounds.yMax - pivot.y) * baseScale.y;

        float scale = desired;
        scale = Mathf.Min(scale, MaxScaleForEdge(minX, area.xMin - pivot.x));
        scale = Mathf.Min(scale, MaxScaleForEdge(maxX, area.xMax - pivot.x));
        scale = Mathf.Min(scale, MaxScaleForEdge(minY, area.yMin - pivot.y));
        scale = Mathf.Min(scale, MaxScaleForEdge(maxY, area.yMax - pivot.y));
        return Mathf.Max(1f, scale);
    }

    private static float MaxScaleForEdge(float extent, float allowed)
    {
        // 피벗에서 같은 방향으로 뻗어 있는 변만 제한 대상
        if (Mathf.Abs(extent) < 0.01f || Mathf.Sign(extent) != Mathf.Sign(allowed))
        {
            return float.MaxValue;
        }
        return allowed / extent;
    }

    private static Vector2 GetCanvasSize(Canvas canvas)
    {
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null
            && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize
            && scaler.screenMatchMode == CanvasScaler.ScreenMatchMode.MatchWidthOrHeight)
        {
            // CanvasScaler와 같은 공식 (캔버스가 아직 갱신되지 않았어도 정확한 값)
            Vector2 screen = new Vector2(Screen.width, Screen.height);
            Vector2 reference = scaler.referenceResolution;
            float logWidth = Mathf.Log(screen.x / reference.x, 2f);
            float logHeight = Mathf.Log(screen.y / reference.y, 2f);
            float scaleFactor = Mathf.Pow(2f, Mathf.Lerp(logWidth, logHeight, scaler.matchWidthOrHeight));
            return screen / scaleFactor;
        }

        return ((RectTransform)canvas.transform).rect.size;
    }

    private static Rect ComputeRectInCanvas(RectTransform target, Transform canvasTransform, Rect canvasRect)
    {
        if (target == null || target == canvasTransform)
        {
            return canvasRect;
        }
        Rect parentRect = ComputeRectInCanvas(target.parent as RectTransform, canvasTransform, canvasRect);
        return ComputeRect(target, parentRect);
    }

    private static Rect ComputeRect(RectTransform target, Rect parentRect)
    {
        Vector2 anchorMin = parentRect.min + Vector2.Scale(parentRect.size, target.anchorMin);
        Vector2 anchorMax = parentRect.min + Vector2.Scale(parentRect.size, target.anchorMax);
        Vector2 size = anchorMax - anchorMin + target.sizeDelta;
        Vector2 pivot = anchorMin + Vector2.Scale(anchorMax - anchorMin, target.pivot) + target.anchoredPosition;
        return new Rect(pivot - Vector2.Scale(size, target.pivot), size);
    }

    private static void EncapsulateChildren(RectTransform parent, Rect parentRect, ref Rect bounds)
    {
        foreach (Transform child in parent)
        {
            if (!child.gameObject.activeSelf || child is not RectTransform childRect)
            {
                continue;
            }

            Rect rect = ComputeRect(childRect, parentRect);
            if (rect.width > 0f && rect.height > 0f)
            {
                bounds = Rect.MinMaxRect(
                    Mathf.Min(bounds.xMin, rect.xMin), Mathf.Min(bounds.yMin, rect.yMin),
                    Mathf.Max(bounds.xMax, rect.xMax), Mathf.Max(bounds.yMax, rect.yMax));
            }
            EncapsulateChildren(childRect, rect, ref bounds);
        }
    }
}
