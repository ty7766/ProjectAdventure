using UnityEngine;
using System.Collections.Generic;

public class UI_OptionPresenter
{
    private UI_OptionView _view;
    private string _currentTab;
    private List<UI_OptionTabButtonView> _tabButtons;

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
        foreach (var tabButton in _tabButtons)
        {
            string tabName = tabButton.TabName;
            tabButton.AddButtonListener(() => OnTabSelected(tabName));
        }
        _currentTab = _tabButtons[0].TabName; // Default to the first tab
        UpdateTabButtonVisuals();
    }

    private void Initialize()
    {

    }

    private void OnTabSelected(string tabName)
    {
        _currentTab = tabName;
        UpdateTabButtonVisuals();
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
                CustomDebug.Log($"{tabButton.name}, {_unselectedTabOpacity}");
            }
        }
    }


}
