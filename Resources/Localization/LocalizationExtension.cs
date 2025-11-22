using System;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Windows11Settings.Resources.Localization;

namespace Windows11Settings.Helpers
{
    public class LocalizeExtension : MarkupExtension
    {
        public string Key { get; set; }

        public LocalizeExtension(string key)
        {
            Key = key;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var localizationManager = LocalizationManager.Instance;
            return localizationManager[Key];
        }
    }
}