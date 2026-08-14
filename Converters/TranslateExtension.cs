using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using IpScopePro.Services;

namespace IpScopePro.Converters;

public class TranslateExtension : MarkupExtension
{
    public string Key { get; set; } = string.Empty;

    public TranslateExtension() { }

    public TranslateExtension(string key) => Key = key;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding($"[{Key}]")
        {
            Source = LocalizationService.Instance,
            Mode = BindingMode.OneWay
        };
        return binding.ProvideValue(serviceProvider);
    }
}
