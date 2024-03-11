using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Tirax.SshManager.Components;

public partial class AppBar : UserControl
{
    public static readonly StyledProperty<string> MainIconProperty = AvaloniaProperty.Register<AppBar, string>(nameof(MainIcon), defaultValue: "Menu");
    public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<AppBar, string>(nameof(Title), defaultValue: "Title");
    
    public AppBar() {
        InitializeComponent();
    }

    public string MainIcon {
        get => GetValue(MainIconProperty);
        set => SetValue(MainIconProperty, value);
    }

    public string Title {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
}