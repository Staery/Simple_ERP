using System.Text.Json;
using System.Text.Json.Serialization;
using SimpleErp.Core.Models;

namespace SimpleErp.Core.Services;

/// <summary>Stores the data as one human-readable JSON file, written atomically.</summary>
public sealed class JsonErpRepository(string filePath) : IErpRepository
{
    internal static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public string Location { get; } = Path.GetFullPath(filePath);

    /// <summary>The default data file under the current user's application data folder.</summary>
    public static JsonErpRepository CreateDefault()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SimpleERP");
        return new JsonErpRepository(Path.Combine(folder, "data.json"));
    }

    public async Task<ErpData?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(Location))
        {
            return null;
        }

        ErpData? data;
        try
        {
            await using var stream = File.OpenRead(Location);
            data = await JsonSerializer.DeserializeAsync<ErpData>(stream, SerializerOptions, cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            // Keep the damaged file for manual recovery instead of overwriting it on the next save.
            var backupPath = $"{Location}.corrupt-{DateTime.Now:yyyyMMdd-HHmmss}";
            File.Move(Location, backupPath, overwrite: true);

            throw new InvalidDataException(
                $"The data file could not be read ({ex.Message}). It has been moved to '{backupPath}'.", ex);
        }

        return Normalize(data ?? new ErpData());
    }

    public async Task SaveAsync(ErpData data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        Directory.CreateDirectory(Path.GetDirectoryName(Location)!);

        // Write a temporary file first and swap it in, so a crash never leaves a truncated file behind.
        var tempPath = Location + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, data, SerializerOptions, cancellationToken).ConfigureAwait(false);
        }

        File.Move(tempPath, Location, overwrite: true);
    }

    /// <summary>Repairs what a hand-edited or older file may contain: nulls, duplicates and dangling references.</summary>
    internal static ErpData Normalize(ErpData data)
    {
        data.Employees = (data.Employees ?? []).Where(employee => employee is not null).DistinctBy(employee => employee.Id).ToList();
        data.Projects = (data.Projects ?? []).Where(project => project is not null).DistinctBy(project => project.Id).ToList();

        foreach (var employee in data.Employees)
        {
            employee.FirstName ??= string.Empty;
            employee.LastName ??= string.Empty;
            employee.Email ??= string.Empty;
            employee.City ??= string.Empty;
            employee.Kpi ??= new KpiScores();
            foreach (var indicator in KpiScores.Indicators)
            {
                employee.Kpi.Set(indicator, Math.Clamp(employee.Kpi.Get(indicator), KpiScores.MinScore, KpiScores.MaxScore));
            }
        }

        var employeeIds = data.Employees.Select(employee => employee.Id).ToHashSet();
        foreach (var project in data.Projects)
        {
            project.Name ??= string.Empty;
            project.Client ??= string.Empty;
            project.Description ??= string.Empty;
            project.Progress = Math.Clamp(project.Progress, 0, 100);
            project.TeamIds = (project.TeamIds ?? []).Where(employeeIds.Contains).Distinct().ToList();
        }

        data.SchemaVersion = ErpData.CurrentSchemaVersion;
        return data;
    }
}
