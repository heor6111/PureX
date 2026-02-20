using System.IO;
using PureX.ViewModels;
using PureX.Views;

namespace PureX;

public partial class MainWindow : System.Windows.Window
{
    private readonly MainViewModel _viewModel;
    private ToolboxWindow? _toolboxWindow;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
    }

    private void ToolboxButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_toolboxWindow == null || !_toolboxWindow.IsLoaded)
        {
            _toolboxWindow = new ToolboxWindow
            {
                Owner = this
            };
            _toolboxWindow.Show();
        }
        else
        {
            _toolboxWindow.Activate();
        }
    }

    private void Window_Drop(object sender, System.Windows.DragEventArgs e)
    {
        DropZone.Style = FindResource("DropZoneStyle") as System.Windows.Style;
        
        if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
            var validFiles = files.Where(File.Exists).ToArray();
            
            if (validFiles.Length > 0)
            {
                _viewModel.AddDroppedFiles(validFiles);
            }
            else
            {
                System.Windows.MessageBox.Show("没有找到有效的文件", "提示", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }
    }

    private void Window_DragOver(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            e.Effects = System.Windows.DragDropEffects.Copy;
            DropZone.Style = FindResource("DropZoneActiveStyle") as System.Windows.Style;
        }
        else
        {
            e.Effects = System.Windows.DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void Window_DragLeave(object sender, System.Windows.DragEventArgs e)
    {
        DropZone.Style = FindResource("DropZoneStyle") as System.Windows.Style;
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_viewModel.IsConverting)
        {
            var result = System.Windows.MessageBox.Show("正在转换文件，确定要退出吗？", "确认退出", 
                System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
            
            if (result == System.Windows.MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }
    }
}
