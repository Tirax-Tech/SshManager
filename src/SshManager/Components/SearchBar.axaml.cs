using Avalonia.Controls;

namespace Tirax.SshManager.Components;

public partial class SearchBar : UserControl
{
    public SearchBar() {
        InitializeComponent();
    }

    public string? Text {
        get => input.Text;
        set => input.Text = value;
    }
}