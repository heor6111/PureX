using System.Windows;

namespace PureX.Views;

public partial class ToolboxWindow : Window
{
    public ToolboxWindow()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
