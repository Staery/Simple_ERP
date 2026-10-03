using SimpleErp.Core.Models;

namespace SimpleErp.Core.Services;

/// <summary>Loads and saves all application data.</summary>
public interface IErpRepository
{
    /// <summary>Where the data lives, shown to the user.</summary>
    string Location { get; }

    /// <summary>Returns the stored data, or <see langword="null"/> when nothing has been saved yet.</summary>
    /// <exception cref="InvalidDataException">The stored data is damaged.</exception>
    Task<ErpData?> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(ErpData data, CancellationToken cancellationToken = default);
}
