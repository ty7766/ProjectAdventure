using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialView : MonoBehaviour
{
    [Header("오버레이")]
    [SerializeField]
    private Image _overlayImage;
    [SerializeField]
    private Material _spotlightMaterial;

    [Header("화살표")]
    [SerializeField]
    private RectTransform _arrowContainer;
    [SerializeField]
    private Image _dotPrefab;
    [SerializeField]
    private Image _arrowHead;
    [SerializeField]
    private float _dotSpacing = 20f;
    [SerializeField]
    private float _dotRevealInterval = 0.03f;

    [Header("텍스트 패널")]
    [SerializeField]
    private CanvasGroup _textPanel;
    [SerializeField]
    private TMP_Text _descriptionText;
    [SerializeField]
    private float _textFadeDuration = 0.3f;

    private Material _materialInstance;
    private readonly List<Image> _dots = new List<Image>();
    private Coroutine _animationCoroutine;
    private WaitForSecondsRealtime _waitDotReveal;

    private static readonly int HoleCenterID = Shader.PropertyToID("_HoleCenter");
    private static readonly int HoleSizeID = Shader.PropertyToID("_HoleSize");

    private void Awake()
    {
        _materialInstance = Instantiate(_spotlightMaterial);
        _overlayImage.material = _materialInstance;
        _waitDotReveal = new WaitForSecondsRealtime(_dotRevealInterval);
    }

    private void OnDestroy()
    {
        if(_materialInstance != null)
        {
            Destroy(_materialInstance);
        }
    }

    public void ShowStep(TutorialStepConfig config)
    {
        Vector2 screenPosition = GetScreenPosition(config);
        UpdateSpotlight(screenPosition, config.HoleSize);

        Vector2 arrowTipScreenPosition = RectTransformUtility.WorldToScreenPoint(null,config.ArrowTipTarget.position);
        
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
        }

        _animationCoroutine = StartCoroutine(AnimateArrow(screenPosition, arrowTipScreenPosition, config.DescriptionText));
    }

    public void Hide()
    {
        if(_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
            _animationCoroutine = null;
        }

        ClearDots();
        _arrowHead.gameObject.SetActive(false);
        _textPanel.alpha = 0f;
        _textPanel.gameObject.SetActive(false);
    }

    private Vector2 GetScreenPosition(TutorialStepConfig config)
    {
        if(config.Target == TutorialStepConfig.TargetType.WorldObject)
        {
            if(config.WorldTarget == null)
            {
                CustomDebug.LogWarning("TutorialView: WorldTarget이 null 입니다.");
                return Vector2.zero;
            }
            return Camera.main.WorldToScreenPoint(config.WorldTarget.transform.position);
        }

        if(config.UiTarget == null)
        {
            CustomDebug.LogWarning("TutorialView: UITarget이 null입니다.");
            return Vector2.zero;
        }

        return RectTransformUtility.WorldToScreenPoint(null, config.UiTarget.position);
    }

    private void UpdateSpotlight(Vector2 screenPosition, Vector2 holeSize)
    {
        _materialInstance.SetVector(HoleCenterID, new Vector4(screenPosition.x, screenPosition.y, 0f, 0f));
        _materialInstance.SetVector(HoleSizeID, new Vector4(holeSize.x, holeSize.y, 0f, 0f));
    }

    private IEnumerator AnimateArrow(Vector2 from, Vector2 to, string text)
    {
        ClearDots();
        _arrowHead.gameObject.SetActive(false);
        _textPanel.gameObject.SetActive(false);
        _textPanel.alpha = 0f;
        _descriptionText.text = text;

        Vector2 direction = (to - from).normalized;
        float totalDistance = Vector2.Distance(from, to);
        int dotCount = Mathf.FloorToInt(totalDistance / _dotSpacing);

        for (int i = 0; i < dotCount; i++)
        {
            Vector2 dotScreenPosition = from + direction * (_dotSpacing * i);
            Image dot = Instantiate(_dotPrefab, _arrowContainer);
            dot.rectTransform.anchoredPosition = ScreenToCanvasPosition(dotScreenPosition);
            dot.gameObject.SetActive(true);
            _dots.Add(dot);

            yield return _waitDotReveal;
        }

        //화살표 머리 방향 설정 후 표시
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        _arrowHead.rectTransform.anchoredPosition = ScreenToCanvasPosition(to);
        _arrowHead.rectTransform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        _arrowHead.gameObject.SetActive(true);

        //TextPanel Fadein
        _textPanel.gameObject.SetActive(true);
        yield return StartCoroutine(FadeInTextPanel());

        _animationCoroutine = null;
    }

    private IEnumerator FadeInTextPanel()
    {
        float elapsed = 0f;

        while(elapsed < _textFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _textPanel.alpha = Mathf.Clamp01(elapsed / _textFadeDuration);
            yield return null;
        }

        _textPanel.alpha = 1f;
    }

    private void ClearDots()
    {
        foreach(Image dot in _dots)
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
