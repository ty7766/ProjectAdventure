using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupContext
{
    public string Content;
    public Sprite Icon;
    public Color BackgroundColor;

    public PopupContext(string content, Sprite icon, Color backgroundColor)
    {
        Content = content;
        Icon = icon;
        BackgroundColor = backgroundColor;
    }
}

[RequireComponent(typeof(NotificationView))]
public class NotificationPresenter : MonoBehaviour
{
    private static Queue<PopupContext> _popupQueue = new Queue<PopupContext>();
    private static bool _isShowingPopup = false;
    private static NotificationPresenter _instance;

    [Header("Position Settings")]
    [SerializeField]
    private Vector2 _hiddenPosition = new Vector2(0, 150);
    [SerializeField]
    private Vector2 _shownPosition = new Vector2(0, -50);
    [SerializeField, Tooltip("숨김 위치에서 화면 가장자리 밖으로 추가로 더 밀어낼 거리 (그림자/외곽선 여유)")]
    private float _offscreenMargin = 24f;

    [Header("Timing Settings")]
    [SerializeField]
    private float _showDuration = 2.0f;
    [SerializeField]
    private float _animDuration = 0.3f;

    private NotificationView _view;

    private void Awake()
    {
        _instance = this;
        _view = GetComponent<NotificationView>();
    }

    private void Start()
    {
        _view.HidePopup();
    }

    private void Update()
    {
        if (_popupQueue.Count > 0 && !_isShowingPopup)
        {
            StartCoroutine(ShowPopupRoutine());
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        _isShowingPopup = false;
        _view.HidePopup();
    }

    public static void Reset()
    {
        _popupQueue.Clear();
        if (_instance != null)
        {
            _instance.StopAllCoroutines();
            _instance._view?.HidePopup();
        }
        _isShowingPopup = false;
    }

    /// <summary>
    /// 플레이어 HUD에 표출할 알림을 등록합니다.
    /// </summary>
    /// <param name="context"></param>
    public static void AddPopup(PopupContext context)
    {
        if (context == null)
        {
            return;
        }
        _popupQueue.Enqueue(context);
    }

    /// <summary>
    /// 플레이어 HUD에 표출할 알림을 등록합니다.
    /// </summary>
    /// <param name="content">표출할 내용입니다.</param>
    /// <param name="icon">표출할 아이콘입니다.</param>
    /// <param name="backgroundColor">알림 팝업의 배경 색상입니다.</param>
    public static void AddPopup(string content, Sprite icon, Color backgroundColor)
    {
        var context = new PopupContext(content, icon, backgroundColor);
        AddPopup(context);
    }

    private IEnumerator ShowPopupRoutine()
    {
        _isShowingPopup = true;

        // 1단계: 등장 (Slide Down)
        PopupContext currentContext = _popupQueue.Dequeue();
        Vector2 hiddenPosition = ResolveHiddenPosition();
        _view.ShowPopupAt(hiddenPosition, currentContext);
        yield return StartCoroutine(_view.MovePopup(hiddenPosition, _shownPosition, _animDuration));

        // 2단계: 대기
        yield return new WaitForSecondsRealtime(_showDuration);

        // 3단계: 퇴장 (Slide Up)
        yield return StartCoroutine(_view.MovePopup(_shownPosition, hiddenPosition, _animDuration));
        _isShowingPopup = false; 
    }

    /// <summary>
    /// 인스펙터의 숨김 위치가 실제 크기(터치 배율 포함)로는 화면 밖으로 다 나가지 못할 때 보정한다.
    /// 화면 가장자리에 앵커된 상태에서, 피벗 반대편 변까지 가장자리 밖으로 나가는 위치를 최소값으로 쓴다.
    /// </summary>
    private Vector2 ResolveHiddenPosition()
    {
        var rect = (RectTransform)transform;
        Vector2 size = Vector2.Scale(rect.rect.size, rect.localScale);
        Vector2 slide = _hiddenPosition - _shownPosition;
        Vector2 hidden = _hiddenPosition;

        if (slide.x > 0f && rect.anchorMin.x >= 1f)
        {
            hidden.x = Mathf.Max(hidden.x, size.x * rect.pivot.x + _offscreenMargin);
        }
        else if (slide.x < 0f && rect.anchorMax.x <= 0f)
        {
            hidden.x = Mathf.Min(hidden.x, -size.x * (1f - rect.pivot.x) - _offscreenMargin);
        }

        if (slide.y > 0f && rect.anchorMin.y >= 1f)
        {
            hidden.y = Mathf.Max(hidden.y, size.y * rect.pivot.y + _offscreenMargin);
        }
        else if (slide.y < 0f && rect.anchorMax.y <= 0f)
        {
            hidden.y = Mathf.Min(hidden.y, -size.y * (1f - rect.pivot.y) - _offscreenMargin);
        }

        return hidden;
    }


}