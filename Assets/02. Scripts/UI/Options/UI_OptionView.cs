using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class UI_OptionView : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private List<UI_OptionTabButtonView> _tabButtons;

    [Header("Settings")]
    [SerializeField] private float _unselectedTabOpacity = 0.8f;

    private UI_OptionPresenter _presenter;

    private void Awake()
    {
        InitializePresenter();
    }

    private void Start()
    {
        _presenter.Start();
    }

    private void InitializePresenter()
    {
        Assert.IsNotNull(_tabButtons, "Tab buttons list reference is null!");
        Assert.IsTrue(_tabButtons.Count > 0, "Tab buttons list is empty in the inspector!");
        _presenter = new UI_OptionPresenter(this, _tabButtons, _unselectedTabOpacity);
    }
}
