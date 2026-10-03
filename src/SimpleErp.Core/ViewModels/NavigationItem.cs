using CommunityToolkit.Mvvm.ComponentModel;

namespace SimpleErp.Core.ViewModels;

/// <summary>An entry of the sidebar.</summary>
/// <param name="iconKey">Key of a <c>Geometry</c> resource in the WPF project's <c>Themes/Icons.xaml</c>.</param>
public sealed partial class NavigationItem(string title, string iconKey, PageViewModel page) : ObservableObject
{
    public string Title { get; } = title;

    public string IconKey { get; } = iconKey;

    public PageViewModel Page { get; } = page;

    /// <summary>Number shown next to the title, or <see langword="null"/> for none.</summary>
    [ObservableProperty]
    private int? _badge;
}
