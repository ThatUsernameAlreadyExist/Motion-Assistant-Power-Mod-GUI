using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows11Settings.Resources.Localization;

/// <summary>
/// Model for page visibility settings
/// </summary>
public class PageVisibilityModel : INotifyPropertyChanged, IDisposable
{
    private string _pageName;
    private bool _isVisible;
    private readonly LocalizationManager _localization;

    public PageVisibilityModel(LocalizationManager localization)
    {
        _localization = localization;
        // Subscribe to localization changes
        _localization.PropertyChanged += OnLocalizationChanged;
    }

    private void OnLocalizationChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LocalizationManager.CurrentLanguage))
        {
            // Language changed, notify that DisplayName has changed
            OnPropertyChanged(nameof(DisplayName));
        }
    }

    public void Dispose()
    {
        _localization.PropertyChanged -= OnLocalizationChanged;
    }

    public string PageName
    {
        get => _pageName;
        set => SetProperty(ref _pageName, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    public string DisplayName => _localization[_pageName];

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}