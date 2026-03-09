using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UI_OptionTabButtonView : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField, Tooltip("탭 선택을 나타내기 위한 그래픽 컴포넌트 연결")]
    private Graphic _graphicComponent;
    [SerializeField, Tooltip("현재 탭의 이름")]
    private string _tabName;

    private Button _button;
    public string TabName => _tabName;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    private void OnDestroy()
    {
        if(_button)
        {
            _button.onClick.RemoveAllListeners();
        }
    }

    public void AddButtonListener(UnityEngine.Events.UnityAction action)
    {
        if(_button)
        {
            _button.onClick.AddListener(action);
        }
    }

    public void SetGraphicOpacity(float opacity)
    {
        if(_graphicComponent)
        {
            Color color = _graphicComponent.color;
            color.a = opacity;
            _graphicComponent.color = color;
        }
    }
}
