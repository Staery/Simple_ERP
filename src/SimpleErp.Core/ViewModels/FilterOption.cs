namespace SimpleErp.Core.ViewModels;

/// <summary>An entry of a filter or sort drop-down.</summary>
public sealed record FilterOption<T>(string Label, T Value)
{
    public override string ToString() => Label;
}
