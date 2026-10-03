using SimpleErp.Core.Abstractions;
using SimpleErp.Core.Models;
using SimpleErp.Core.Services;

namespace SimpleErp.Core.Tests.Fakes;

internal sealed class InMemoryRepository(ErpData? initial = null) : IErpRepository
{
    public ErpData? Stored { get; private set; } = initial;

    public int SaveCount { get; private set; }

    public bool FailSaves { get; set; }

    public Exception? LoadException { get; set; }

    public string Location => "/data/simple-erp/data.json";

    public Task<ErpData?> LoadAsync(CancellationToken cancellationToken = default) =>
        LoadException is { } ex ? Task.FromException<ErpData?>(ex) : Task.FromResult(Stored?.Clone());

    public Task SaveAsync(ErpData data, CancellationToken cancellationToken = default)
    {
        if (FailSaves)
        {
            throw new IOException("Disk is full.");
        }

        SaveCount++;
        Stored = data.Clone();
        return Task.CompletedTask;
    }
}

internal sealed class FakeDialogs : IDialogService
{
    public bool ConfirmResult { get; set; } = true;

    public UnsavedChangesDecision UnsavedChangesDecision { get; set; } = UnsavedChangesDecision.Cancel;

    public string? FileToSave { get; set; }

    public List<string> Errors { get; } = [];

    public int SavePrompts { get; private set; }

    public int Confirmations { get; private set; }

    public bool Confirm(string title, string message)
    {
        Confirmations++;
        return ConfirmResult;
    }

    public UnsavedChangesDecision AskToSaveChanges(string itemName)
    {
        SavePrompts++;
        return UnsavedChangesDecision;
    }

    public void ShowError(string title, string message) => Errors.Add(message);

    public string? PickSaveFile(string title, string defaultFileName, string filter) => FileToSave;
}

internal sealed class FakeShell : IShellService
{
    public List<string> Opened { get; } = [];

    public void Open(string path) => Opened.Add(path);
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now.ToUniversalTime();

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}

internal static class TestData
{
    public static readonly DateOnly Today = new(2026, 3, 16);

    public static readonly TimeProvider Time = new FixedTimeProvider(new DateTimeOffset(2026, 3, 16, 9, 30, 0, TimeSpan.Zero));

    public static Employee Employee(
        string first = "Ada",
        string last = "Lovelace",
        Position position = Position.Developer,
        int kpi = 70,
        decimal salary = 7_000m) => new()
    {
        FirstName = first,
        LastName = last,
        Email = $"{first}.{last}@example.com".ToLowerInvariant(),
        City = "London",
        Position = position,
        BirthDate = new DateOnly(1990, 12, 10),
        HireDate = new DateOnly(2020, 2, 1),
        MonthlySalary = salary,
        Kpi = new KpiScores { Teamwork = kpi, CodeEfficiency = kpi, DesignSkills = kpi, Leadership = kpi, ProjectSuccess = kpi },
    };

    public static Project Project(string name = "Portal", params Guid[] team) => new()
    {
        Name = name,
        Client = "Contoso",
        Status = ProjectStatus.Active,
        StartDate = Today.AddDays(-50),
        EndDate = Today.AddDays(50),
        Budget = 100_000m,
        Spent = 40_000m,
        Progress = 50,
        TeamIds = [.. team],
    };

    /// <summary>A store loaded with the given data (or demo data when <paramref name="data"/> is null).</summary>
    public static async Task<(ErpStore Store, InMemoryRepository Repository)> LoadedStoreAsync(ErpData? data = null)
    {
        var repository = new InMemoryRepository(data ?? DemoData.Create(Today));
        var store = new ErpStore(repository, Time);
        await store.LoadAsync();
        return (store, repository);
    }
}
