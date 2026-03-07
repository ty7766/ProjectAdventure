using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialView : MonoBehaviour
{
    //--- Serialized Fields ---//
    [Header("오버레이")]
    [SerializeField]
    private Image _overlayImage;
    [SerializeField]
    private Material _spotlightMaterial;

    [Header("텍스트 패널")]
    [SerializeField]
    private CanvasGroup _textPanel;
    [SerializeField]
    private TMP_Text _descriptionText;
    [SerializeField]
    private float _textFadeDuration = 0.3f;

    //--- Fields ---//
    private Material _materialInstance;
    private RectTransform _textPanelRect;
    private Coroutine _fadeCoroutine;

    private static readonly int HoleCenterID = Shader.PropertyToID("_HoleCenter");
    private static readonly int HoleSizeID = Shader.PropertyToID("_HoleSize");

    //--- Unity Methods ---//
    private void Awake()
    {
        _materialInstance = Instantiate(_spotlightMaterial);
        _overlayImage.material = _materialInstance;
        _textPanelRect = _textPanel.GetComponent<RectTransform>();
    }

    private void OnDestroy()
    {
        if (_materialInstance != null)
        {
            Destroy(_materialInstance);
        }
    }

    //--- Public Methods ---//
    public void ShowStep(TutorialStepConfig config)
    {
        Vector2 spotlightScreenPos = GetScreenPosition(config) + config.SpotlightOffset;
        UpdateSpotlight(spotlightScreenPos, config.HoleSize);

        _descriptionText.text = config.DescriptionText;
        _textPanel.gameObject.SetActive(true);
        _textPanel.alpha = 0f;

        Vector2 tipScreenPos = RectTransformUtility.WorldToScreenPoint(null, config.ArrowTipTarget.position);
        PositionTextPanel(tipScreenPos);

        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }
        _fadeCoroutine = StartCoroutine(FadeInTextPanel());
    }

    /// <summary>
    /// 현재 스텝 UI를 즉시 숨깁니다. 카메라 이동 전 호출합니다.
    /// </summary>
    public void HideCurrentStep()
    {
        _materialInstance.SetVector(HoleCenterID, new Vector4(-9999f, -9999f, 0f, 0f));
        _materialInstance.SetVector(HoleSizeID, Vector4.zero);

        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }
        _textPanel.alpha = 0f;
        _textPanel.gameObject.SetActive(false);
    }

    /// <summary>
    /// 튜토리얼 UI 전체를 숨깁니다.
    /// </summary>
    public void Hide()
    {
        HideCurrentStep();
    }

    //--- Private Methods ---//
    private Vector2 GetScreenPosition(TutorialStepConfig config)
    {
        if (config.Target == TutorialStepConfig.TargetType.WorldObject)
        {
            if (config.WorldTarget == null)
            {
                CustomDebug.LogWarning("TutorialView: WorldTarget이 null입니다.");
                return Vector2.zero;
            }
            return Camera.main.WorldToScreenPoint(config.WorldTarget.position);
        }

        if (config.UiTarget == null)
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

    private void PositionTextPanel(Vector2 tipScreenPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _textPanelRect.parent as RectTransform,
            tipScreenPos,
            null,
            out Vector2 localPos
        );
        _textPanelRect.anchoredPosition = localPos;
    }

    private IEnumerator FadeInTextPanel()
    {
        float elapsed = 0f;
        while (elapsed < _textFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _textPanel.alpha = Mathf.Clamp01(elapsed / _textFadeDuration);
            yield return null;
        }
        _textPanel.alpha = 1f;
        _fadeCoroutine = null;
    }
}
