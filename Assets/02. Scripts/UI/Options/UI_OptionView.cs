using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class UI_OptionView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TitleFlowController _flowController;

    [Header("Components")]
    [SerializeField] private List<UI_OptionTabButtonView> _tabButtons;
    [SerializeField] private Button _backButton;

    [Header("Pages")]
    [SerializeField] private CanvasGroup _graphicTab;
    [SerializeField] private CanvasGroup _audioTab;
    [SerializeField] private CanvasGroup _keyBindTab;
    [SerializeField] private UI_KeyBindTabView _keyBindTabView;

    [Header("Settings")]
    [SerializeField] private float _unselectedTabOpacity = 0.8f;
    [SerializeField] private float _transitionDuration = 0.5f;

    private UI_OptionPresenter _presenter;

    public TitleFlowController FlowController => _flowController;

    private void Awake()
    {
        InitializePresenter();
    }

    private void Start()
    {
        _presenter.Start();
        if(_backButton)
        {
            _backButton.onClick.AddListener(_presenter.OnBackButtonClicked);
        }
    }

    private void OnEnable()
    {
        _presenter.RefreshView();
    }

    private void OnDisable()
    {
        _presenter.Cleanup();
    }

    private void OnDestroy()
    {
        if(_backButton)
        {
            _backButton.onClick.RemoveListener(_presenter.OnBackButtonClicked);
        }
    }

    private void InitializePresenter()
    {
        Assert.IsNotNull(_tabButtons, "Tab buttons list reference is null!");
        Assert.IsTrue(_tabButtons.Count > 0, "Tab buttons list is empty in the inspector!");
        _presenter = new UI_OptionPresenter(this, _tabButtons, _unselectedTabOpacity);
    }

    public void ShowGraphicTab()
    {
        Assert.IsNotNull(_graphicTab, "Graphic Tab reference is not assigned in the inspector!");

        HideAllTab();
        StartCoroutine(DoTransition(1f, _graphicTab, _transitionDuration));
    }

    public void ShowAudioTab()
    {
        Assert.IsNotNull(_audioTab, "Audio Tab reference is not assigned in the inspector!");

        HideAllTab();
        StartCoroutine(DoTransition(1f, _audioTab, _transitionDuration));
    }

    public void ShowKeyBindTab()
    {
        Assert.IsNotNull(_keyBindTab, "Key Bind Tab reference is not assigned in the inspector!");

        if (_keyBindTabView != null)
        {
            _keyBindTabView.RefreshKeyBindData();
        }
        else
        {
            Debug.LogError("[Options] ShowKeyBindTab: _keyBindTabView is not assigned in inspector!");
        }

        HideAllTab();
        StartCoroutine(DoTransition(1f, _keyBindTab, _transitionDuration));
    }

    private void HideAllTab()
    {
        Assert.IsNotNull(_graphicTab, "Graphic Tab reference is not assigned in the inspector!");
        Assert.IsNotNull(_audioTab, "Audio Tab reference is not assigned in the inspector!");
        Assert.IsNotNull(_keyBindTab, "Key Bind Tab reference is not assigned in the inspector!");

        _graphicTab.blocksRaycasts = false;
        _audioTab.blocksRaycasts = false;
        _keyBindTab.blocksRaycasts = false;
        StartCoroutine(DoTransition(0f, _graphicTab, _transitionDuration));
        StartCoroutine(DoTransition(0f, _audioTab, _transitionDuration));
        StartCoroutine(DoTransition(0f, _keyBindTab, _transitionDuration));
    }

    private IEnumerator DoTransition(float targetAlpha, CanvasGroup canvasGroup, float duration)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration);
            yield return null;
        }
        canvasGroup.alpha = targetAlpha;
        canvasGroup.blocksRaycasts = targetAlpha > 0.05f;
        canvasGroup.interactable = targetAlpha > 0.05f;
    }
}
