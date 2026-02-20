using System.Windows;
using PureX.ViewModels;
using PureX.Core.Models;

namespace PureX.Views;

public partial class AdvancedSettingsWindow : Window
{
    private readonly AdvancedSettingsViewModel _viewModel;

    public AdvancedSettingsWindow(ConversionOptions options, string mediaType)
    {
        InitializeComponent();
        _viewModel = new AdvancedSettingsViewModel(options, mediaType);
        DataContext = _viewModel;
    }

    public ConversionOptions Result { get; private set; } = new();

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        Result = _viewModel.GetOptions();
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
