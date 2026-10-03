namespace SimpleErp.Core.Validation;

/// <summary>Validation messages keyed by the name of the property they belong to.</summary>
public sealed class ValidationErrors
{
    private readonly Dictionary<string, string> _errors = new(StringComparer.Ordinal);

    public bool IsValid => _errors.Count == 0;

    public int Count => _errors.Count;

    public IReadOnlyDictionary<string, string> ByProperty => _errors;

    /// <summary>The message for <paramref name="property"/>, or <see langword="null"/> when it is valid.</summary>
    public string? this[string property] => _errors.GetValueOrDefault(property);

    /// <summary>Records an error. The first error recorded for a property wins.</summary>
    public void Add(string property, string message) => _errors.TryAdd(property, message);

    public bool Contains(string property) => _errors.ContainsKey(property);
}
