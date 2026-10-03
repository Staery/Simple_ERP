using CommunityToolkit.Mvvm.ComponentModel;

namespace SimpleErp.Core.ViewModels;

/// <summary>The message in the status bar, shared by all pages.</summary>
public sealed partial class AppStatus : ObservableObject
{
    [ObservableProperty]
    private string _message = "Ready";

    public void Show(string message) => Message = message;
}
