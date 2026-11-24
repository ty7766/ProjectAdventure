using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UI_ConsentModalView : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private TMPro.TMP_Text _intro;
    [SerializeField] private GameObject _adItem;
    [SerializeField] private CanvasGroup _root;
    [SerializeField] private Button _acceptButton;
    [SerializeField] private Button _declineButton;
    [SerializeField] private Button _policyButton;
    [Header("문구")]
    [SerializeField, TextArea] private string _introWithAds = "클라우드 저장, 고객 문의, 광고 제공을 위해 아래 정보를 처리합니다.";
    [SerializeField, TextArea] private string _introWithoutAds = "클라우드 저장과 고객 문의를 위해 아래 정보를 처리합니다.";

    private Tween _fade;
    private Action _accept;
    private Action _decline;

    public bool IsShown { get; private set; }

    private void Awake()
    {
        _acceptButton.onClick.AddListener(Accept);
        _declineButton.onClick.AddListener(Decline);
        _policyButton.onClick.AddListener(LandLinkerServices.OpenPrivacyPolicy);
    }

    public void Show(Action onAccept, Action onDecline, bool showAdDisclosure)
    {
        if (_adItem != null) _adItem.SetActive(showAdDisclosure);
        if (_intro != null) _intro.text = showAdDisclosure ? _introWithAds : _introWithoutAds;
        gameObject.SetActive(true);
        _accept = onAccept;
        _decline = onDecline;
        IsShown = true;
        _root.blocksRaycasts = true;
        _root.interactable = true;
        _fade?.Kill();
        _fade = _root.DOFade(1f, 0.196f).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    public void Hide()
    {
        IsShown = false;
        _root.blocksRaycasts = false;
        _root.interactable = false;
        _fade?.Kill();
        _fade = _root.DOFade(0f, 0.18f).SetEase(Ease.InQuad).SetUpdate(true).OnComplete(() => gameObject.SetActive(false));
    }

    private void Accept()
    {
        Hide();
        _accept?.Invoke();
    }

    private void Decline()
    {
        Hide();
        _decline?.Invoke();
    }

    private void OnDestroy()
    {
        _fade?.Kill();
    }
}
