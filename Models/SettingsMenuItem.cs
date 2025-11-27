using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows11Settings.Resources.Localization;

namespace Windows11Settings.Models
{
    public class SettingsMenuItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private bool _hasUnsavedChanges;
        private bool _isVisible = true;
        private readonly LocalizationManager _localization;

        public SettingsMenuItem()
        {
            _localization = LocalizationManager.Instance;
            _localization.PropertyChanged += (s, e) => OnPropertyChanged(nameof(Title));
        }

        public string Icon { get; set; } = string.Empty;
        public string PageKey { get; set; } = string.Empty;

        public string Title => _localization[PageKey];

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set
            {
                if (_hasUnsavedChanges != value)
                {
                    _hasUnsavedChanges = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}