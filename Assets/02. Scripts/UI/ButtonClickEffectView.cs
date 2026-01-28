using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Image))]
public class ButtonClickEffectView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    //--- Settings ---//
    [Header("버튼 스프라이트 설정")]
    [SerializeField] private Sprite _buttonNormal;
    [SerializeField] private Sprite _buttonClicked;

    //--- fields ---//
    private Image _buttonImage;

    //--- Unity Methods ---//
    private void Awake()
    {
        _buttonImage = GetComponent<Image>();
    }

    private void Start()
    {
        SetButtonNormal();
    }

    //--- Public Methods ---//
    public void OnPointerDown(PointerEventData eventData)
    {
        SetButtonClicked();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SetButtonNormal();
    }

    //--- Private Methods ---//
    private void SetButtonNormal()
    {
        if (_buttonNormal != null)
            _buttonImage.sprite = _buttonNormal;
    }

    private void SetButtonClicked()
    {
        if (_buttonClicked != null)
            _buttonImage.sprite = _buttonClicked;
    }
}