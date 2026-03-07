using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialArrowRenderer : MonoBehaviour
{
    //--- Serialized Fields ---//
    [Header("화살표 구성")]
    [SerializeField]
    private RectTransform _arrowContainer;
    [SerializeField]
    private Image _dotPrefab;
    [SerializeField]
    private Image _arrowHead;

    [Header("애니메이션")]
    [SerializeField]
    private float _dotSpacing = 20f;
    [SerializeField]
    private float _dotRevealInterval = 0.03f;

    //--- Fields ---//
    private readonly List<Image> _dots = new List<Image>();
    private Coroutine _animationCoroutine;
    private WaitForSecondsRealtime _waitDotReveal;

    //--- Unity Methods ---//
    private void Awake()
    {
        _waitDotReveal = new WaitForSecondsRealtime(_dotRevealInterval);
    }

    //--- Public Methods ---//
    /// <summary>
    /// from에서 to까지 점선 화살표를 애니메이션합니다. 완료 시 onComplete가 호출됩니다.
    /// </summary>
    public void Animate(Vector2 from, Vector2 to, Action onComplete = null)
    {
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
        }
        _animationCoroutine = StartCoroutine(AnimateRoutine(from, to, onComplete));
    }

    /// <summary>
    /// 화살표 애니메이션을 즉시 중단하고 점들을 제거합니다.
    /// </summary>
    public void Clear()
    {
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
            _animationCoroutine = null;
        }
        ClearDots();
        _arrowHead.gameObject.SetActive(false);
    }

    /// <summary>
    /// 중심(center)과 반크기(halfSize)를 가진 사각형에서 direction 방향 가장자리 점을 반환합니다.
    /// </summary>
    public static Vector2 GetBoxEdgePoint(Vector2 center, Vector2 halfSize, Vector2 direction)
    {
        if (direction == Vector2.zero)
        {
            return center;
        }

        float tx = direction.x != 0f ? halfSize.x / Mathf.Abs(direction.x) : float.MaxValue;
        float ty = direction.y != 0f ? halfSize.y / Mathf.Abs(direction.y) : float.MaxValue;
        return center + direction * Mathf.Min(tx, ty);
    }

    //--- Private Methods ---//
    private IEnumerator AnimateRoutine(Vector2 from, Vector2 to, Action onComplete)
    {
        ClearDots();
        _arrowHead.gameObject.SetActive(false);

        Vector2 direction = (to - from).normalized;
        float totalDistance = Vector2.Distance(from, to);
        int dotCount = Mathf.FloorToInt(totalDistance / _dotSpacing);

        for (int i = 0; i < dotCount; i++)
        {
            Vector2 dotScreenPos = from + direction * (_dotSpacing * i);
            Image dot = Instantiate(_dotPrefab, _arrowContainer);
            dot.rectTransform.anchoredPosition = ScreenToCanvasPosition(dotScreenPos);
            dot.gameObject.SetActive(true);
            _dots.Add(dot);
            yield return _waitDotReveal;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        _arrowHead.rectTransform.anchoredPosition = ScreenToCanvasPosition(to);
        _arrowHead.rectTransform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        _arrowHead.gameObject.SetActive(true);

        _animationCoroutine = null;
        onComplete?.Invoke();
    }

    private void ClearDots()
    {
        foreach (Image dot in _dots)
        {
            if (dot != null)
            {
                Destroy(dot.gameObject);
            }
        }
        _dots.Clear();
    }

    private Vector2 ScreenToCanvasPosition(Vector2 screenPosition)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _arrowContainer,
            screenPosition,
            null,
            out Vector2 localPosition);
        return localPosition;
    }
}
