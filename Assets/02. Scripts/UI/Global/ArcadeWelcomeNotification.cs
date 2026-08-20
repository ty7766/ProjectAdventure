using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Global notification presenter used by GlobalUI.
///
/// Calls made before GlobalUI exists are kept in a pending queue. Once the
/// presenter is available, pending notifications and later requests are
/// played in FIFO order.
/// </summary>
public sealed class ArcadeWelcomeNotification : MonoBehaviour
{
    private sealed class NotificationRequest
    {
        public readonly string Message;
        public readonly bool UseConfiguredStartDelay;

        public NotificationRequest(string message, bool useConfiguredStartDelay)
        {
            Message = message;
            UseConfiguredStartDelay = useConfiguredStartDelay;
        }
    }

    private static ArcadeWelcomeNotification _instance;
    private static readonly Queue<NotificationRequest> PendingNotifications =
        new Queue<NotificationRequest>();
    private static bool _arcadeLoginNotificationQueued;

    public static ArcadeWelcomeNotification Instance => _instance;
    public static bool HasInstance => _instance != null && _instance._isInitialized;

    [Header("UI References")]
    [SerializeField] private RectTransform _panelRect;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private CanvasGroup _messageCanvasGroup;
    [SerializeField] private TextMeshProUGUI _messageText;

    [Header("Animation")]
    [SerializeField] private Vector2 _hiddenPosition = new Vector2(0f, 120f);
    [SerializeField] private Vector2 _shownPosition = new Vector2(0f, -68f);
    [SerializeField, Min(0f)] private float _startDelay = 1f;
    [SerializeField] private Vector2 _collapsedSize = new Vector2(88f, 88f);
    [SerializeField] private Vector2 _expandedSize = new Vector2(512f, 88f);
    [SerializeField, Min(0f)] private float _slideInDuration = 0.45f;
    [SerializeField, Min(0f)] private float _expandDuration = 0.35f;
    [SerializeField, Min(0f)] private float _messageFadeDuration = 0.25f;
    [SerializeField, Min(0f)] private float _visibleDuration = 2.5f;
    [SerializeField, Min(0f)] private float _collapseDuration = 0.35f;
    [SerializeField, Min(0f)] private float _slideOutDuration = 0.35f;

    private readonly Queue<NotificationRequest> _notificationQueue =
        new Queue<NotificationRequest>();

    private Coroutine _notificationCoroutine;
    private bool _isInitialized;

    /// <summary>
    /// Queues a generic notification. It plays immediately when the presenter
    /// is available, or waits in the static pending queue otherwise.
    /// </summary>
    public static void Show(string message)
    {
        Enqueue(message, false);
    }

    /// <summary>Queues a two-line notification using the supplied rich text.</summary>
    public static void Show(string title, string detail)
    {
        Enqueue(string.Concat(title ?? string.Empty, "\n", detail ?? string.Empty), false);
    }

    /// <summary>
    /// Queues the notification shown after Arcade authentication becomes ready.
    /// The configured startup delay is applied only to this notification.
    /// </summary>
    public static void ShowArcadeLoginNotification(string displayName)
    {
        // Preserve the previous one-time welcome behavior even if the SDK
        // refreshes its credential and raises ready again later.
        if (_arcadeLoginNotificationQueued)
        {
            return;
        }

        _arcadeLoginNotificationQueued = true;
        string safeDisplayName = string.IsNullOrWhiteSpace(displayName)
            ? "Player"
            : displayName.Trim();

        Enqueue(
            string.Concat(
                safeDisplayName,
                "\uB2D8, \uC548\uB155\uD558\uC138\uC694.",
                "\n<size=80%>Arcade ID\uB85C \uB85C\uADF8\uC778 \uB418\uC5C8\uC2B5\uB2C8\uB2E4.</size>"),
            true);
    }

    /// <summary>Queues the standard cloud-save completion notification.</summary>
    public static void ShowCloudSaveCompleted()
    {
        Enqueue(
            "\uC790\uB3D9 \uC800\uC7A5\uD588\uC5B4\uC694\n<size=80%>\uD074\uB77C\uC6B0\uB4DC\uC5D0 \uC9C4\uD589\uC0C1\uD669\uC744 \uB3D9\uAE30\uD654\uD588\uC2B5\uB2C8\uB2E4.</size>",
            false);
    }

    private static void Enqueue(string message, bool useConfiguredStartDelay)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var request = new NotificationRequest(message, useConfiguredStartDelay);
        if (_instance != null && _instance._isInitialized)
        {
            _instance.EnqueueLocal(request);
        }
        else
        {
            PendingNotifications.Enqueue(request);
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        BindReferences();
        SetHiddenState();
        _isInitialized = true;

        DrainPendingNotifications();
        StartProcessingIfNeeded();
    }

    private void OnEnable()
    {
        StartProcessingIfNeeded();
    }

    private void OnDestroy()
    {
        if (_instance != this)
        {
            return;
        }

        while (_notificationQueue.Count > 0)
        {
            PendingNotifications.Enqueue(_notificationQueue.Dequeue());
        }

        _instance = null;
        _isInitialized = false;
        _notificationCoroutine = null;
    }

    private void BindReferences()
    {
        if (_panelRect == null)
        {
            _panelRect = transform as RectTransform;
        }

        if (_messageCanvasGroup == null && _messageText != null)
        {
            _messageCanvasGroup = _messageText.GetComponent<CanvasGroup>();
            if (_messageCanvasGroup == null)
            {
                _messageCanvasGroup = _messageText.gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    private void DrainPendingNotifications()
    {
        while (PendingNotifications.Count > 0)
        {
            _notificationQueue.Enqueue(PendingNotifications.Dequeue());
        }
    }

    private void EnqueueLocal(NotificationRequest request)
    {
        _notificationQueue.Enqueue(request);
        StartProcessingIfNeeded();
    }

    private void StartProcessingIfNeeded()
    {
        if (!_isInitialized
            || !isActiveAndEnabled
            || _notificationCoroutine != null
            || _notificationQueue.Count == 0)
        {
            return;
        }

        _notificationCoroutine = StartCoroutine(ProcessNotificationQueue());
    }

    private IEnumerator ProcessNotificationQueue()
    {
        while (_notificationQueue.Count > 0)
        {
            NotificationRequest request = _notificationQueue.Dequeue();
            yield return PlayNotification(request);
        }

        _notificationCoroutine = null;
    }

    private IEnumerator PlayNotification(NotificationRequest request)
    {
        if (request == null || _messageText == null)
        {
            yield break;
        }

        _messageText.text = request.Message;

        if (request.UseConfiguredStartDelay && _startDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(_startDelay);
        }

        SetHiddenState();
        SetPanelVisible();

        // Slide in as a compact badge, then reveal the message area.
        yield return AnimatePosition(_hiddenPosition, _shownPosition, _slideInDuration, Easing.EaseOut);
        yield return AnimateSize(_collapsedSize, _expandedSize, _expandDuration, Easing.EaseInOut);
        yield return AnimateAlpha(_messageCanvasGroup, 0f, 1f, _messageFadeDuration, Easing.EaseInOut);

        if (_visibleDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(_visibleDuration);
        }

        yield return AnimateAlpha(_messageCanvasGroup, 1f, 0f, _messageFadeDuration, Easing.EaseInOut);
        yield return AnimateSize(_expandedSize, _collapsedSize, _collapseDuration, Easing.EaseInOut);
        yield return AnimatePosition(_shownPosition, _hiddenPosition, _slideOutDuration, Easing.EaseIn);

        SetHiddenState();
    }

    private IEnumerator AnimatePosition(Vector2 startPosition, Vector2 endPosition, float duration, Easing easing)
    {
        yield return AnimateValue(duration, easing, t =>
        {
            if (_panelRect != null)
            {
                _panelRect.anchoredPosition = Vector2.LerpUnclamped(startPosition, endPosition, t);
            }
        });
    }

    private IEnumerator AnimateSize(Vector2 startSize, Vector2 endSize, float duration, Easing easing)
    {
        yield return AnimateValue(duration, easing, t =>
        {
            if (_panelRect != null)
            {
                _panelRect.sizeDelta = Vector2.LerpUnclamped(startSize, endSize, t);
            }
        });
    }

    private IEnumerator AnimateAlpha(CanvasGroup canvasGroup, float startAlpha, float endAlpha, float duration, Easing easing)
    {
        yield return AnimateValue(duration, easing, t =>
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            }
        });
    }

    private static IEnumerator AnimateValue(float duration, Easing easing, Action<float> apply)
    {
        duration = Mathf.Max(0f, duration);
        if (duration <= 0f)
        {
            apply(1f);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float linearT = Mathf.Clamp01(elapsed / duration);
            apply(EvaluateEasing(linearT, easing));
            yield return null;
        }

        apply(1f);
    }

    private static float EvaluateEasing(float t, Easing easing)
    {
        switch (easing)
        {
            case Easing.EaseIn:
                return t * t * t;
            case Easing.EaseOut:
                float inverse = 1f - t;
                return 1f - inverse * inverse * inverse;
            case Easing.EaseInOut:
                return t < 0.5f
                    ? 4f * t * t * t
                    : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
            default:
                return t;
        }
    }

    private void SetPanelVisible()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }
    }

    private void SetHiddenState()
    {
        if (_panelRect != null)
        {
            _panelRect.anchoredPosition = _hiddenPosition;
            _panelRect.sizeDelta = _collapsedSize;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        if (_messageCanvasGroup != null)
        {
            _messageCanvasGroup.alpha = 0f;
            _messageCanvasGroup.interactable = false;
            _messageCanvasGroup.blocksRaycasts = false;
        }
    }

    private enum Easing
    {
        Linear,
        EaseIn,
        EaseOut,
        EaseInOut,
    }
}
