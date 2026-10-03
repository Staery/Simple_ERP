using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SimpleErp.Core.ViewModels;

/// <summary>A page of the main window. The WPF project maps each page type to a view with a DataTemplate.</summary>
public abstract class PageViewModel : ObservableObject
{
    public abstract string Title { get; }

    public abstract string Subtitle { get; }

    /// <summary>Called every time the page is shown, so it can pick up changes made on other pages.</summary>
    public virtual void OnActivated()
    {
    }

    /// <summary>Called before another page is shown. Returns <see langword="false"/> to stay on this page.</summary>
    public virtual Task<bool> CanLeaveAsync() => Task.FromResult(true);
}

/// <summary>A page with a list and an editor; the window maps Ctrl+N and Ctrl+S to these commands.</summary>
public interface IEditorPage
{
    IAsyncRelayCommand NewCommand { get; }

    IAsyncRelayCommand SaveCommand { get; }
}
