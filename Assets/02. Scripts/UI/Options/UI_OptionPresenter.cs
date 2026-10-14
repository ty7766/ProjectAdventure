using UnityEngine;
using System.Collections.Generic;

public class UI_OptionPresenter
{
    private const string KeyBindTabName = "KeyBind";

    private UI_OptionView _view;
    private string _currentTab;
    private List<UI_OptionTabButtonView> _tabButtons;
    private List<UI_OptionTabButtonView> _visibleTabButtons = new List<UI_OptionTabButtonView>();

    private float _unselectedTabOpacity;

    public UI_OptionPresenter(UI_OptionView view, List<UI_OptionTabButtonView> tabButtons, float unselectedTabOpacity)
    {
        _view = view;
        _tabButtons = tabButtons;
        _unselectedTabOpacity = unselectedTabOpacity;
        Initialize();
    }

    public void Start()
    {
        SetupVisibleTabs();
        foreach (var tabButton in _visibleTabButtons)
        {
            string tabName = tabButton.TabName;
            tabButton.AddButtonListener(() => OnTabSelected(tabName));
        }
        _currentTab = _visibleTabButtons[0].TabName;
        RefreshView();
    }

    public void RefreshView()
    {
        UpdateTabView();
        UpdateTabButtonVisuals();
    }

    public void Cleanup()
    {
        foreach (var tabButton in _tabButtons)
        {
            tabButton.RemoveButtonListener();
        }
    }

    //--- Button Handlers ---//
    public void OnBackButtonClicked()
    {
        _view.FlowController.GoToTitle();
    }

    private void Initialize()
    {

    }

    /// <summary>
    /// 터치 UI 플랫폼(모바일)에서는 키바인딩 탭을 노출하지 않는다.
    /// 모바일은 화면 터치 컨트롤을 사용하므로 키 재설정 기능이 불필요하다.
    /// </summary>
    private void SetupVisibleTabs()
    {
        bool hideKeyBindTab = PlatformCapability.UseTouchUI;

        foreach (var tabButton in _tabButtons)
        {
            bool hideButton = hideKeyBindTab && tabButton.TabName == KeyBindTabName;
            if (hideButton)
            {
                tabButton.gameObject.SetActive(false);
                continue;
            }
            _visibleTabButtons.Add(tabButton);
        }

        if (_visibleTabButtons.Count == 0)
        {
            Debug.LogError("[Options] No visible tab buttons after platform filtering!");
            _visibleTabButtons.AddRange(_tabButtons);
        }
    }

    private void OnTabSelected(string tabName)
    {
        _currentTab = tabName;
        UpdateTabButtonVisuals();
        UpdateTabView();
    }

    private void UpdateTabButtonVisuals()
    {
        foreach(var tabButton in _tabButtons)
        {
            if(tabButton.TabName == _currentTab)
            {
                tabButton.SetGraphicOpacity(1f);
            }
            else
            {
                tabButton.SetGraphicOpacity(_unselectedTabOpacity);
            }
        }
    }

    private void UpdateTabView()
    {
        switch (_currentTab)
        {
            case "Graphic":
                _view.ShowGraphicTab();
                break;
            case "Sound":
                _view.ShowAudioTab();
                break;
            case "KeyBind":
                _view.ShowKeyBindTab();
                break;
            default:
                Debug.LogError($"Unknown tab name: {_currentTab}");
                break;
        }
    }


}
