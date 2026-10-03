using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using SimpleErp.Core.ViewModels;

namespace SimpleErp;

/// <summary>The shell window. All behaviour lives in <see cref="ShellViewModel"/>; this class only handles the window lifetime.</summary>
public partial class MainWindow : Window
{
    private readonly ShellViewModel _viewModel;
    private bool _closeConfirmed;

    public MainWindow(ShellViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e) => await _viewModel.LoadCommand.ExecuteAsync(null);

    protected override async void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        if (_closeConfirmed || e.Cancel)
        {
            return;
        }

        // Closing must be decided synchronously, so cancel now and close again once unsaved work is handled.
        e.Cancel = true;

        if (await _viewModel.PrepareToCloseAsync())
        {
            _closeConfirmed = true;
            _ = Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(Close));
        }
    }
}
