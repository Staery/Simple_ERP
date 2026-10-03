using SimpleErp.Core.Models;
using SimpleErp.Core.Services;
using SimpleErp.Core.Tests.Fakes;

namespace SimpleErp.Core.Tests;

public sealed class JsonErpRepositoryTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "simple-erp-tests", Guid.NewGuid().ToString("N"));

    private string FilePath => Path.Combine(_folder, "data.json");

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public async Task Missing_file_loads_as_null() => Assert.Null(await new JsonErpRepository(FilePath).LoadAsync());

    [Fact]
    public async Task Data_survives_a_round_trip()
    {
        var repository = new JsonErpRepository(FilePath);
        var data = DemoData.Create(TestData.Today);

        await repository.SaveAsync(data);
        var loaded = await repository.LoadAsync();

        Assert.NotNull(loaded);
        Assert.Equal(data.Employees.Count, loaded.Employees.Count);
        Assert.Equal(data.Projects.Count, loaded.Projects.Count);

        var expected = data.Employees[3];
        var actual = loaded.Employees.Single(e => e.Id == expected.Id);
        Assert.Equal(expected.FullName, actual.FullName);
        Assert.Equal(expected.BirthDate, actual.BirthDate);
        Assert.Equal(expected.MonthlySalary, actual.MonthlySalary);
        Assert.Equal(expected.Position, actual.Position);
        Assert.Equal(expected.Kpi.CodeEfficiency, actual.Kpi.CodeEfficiency);
        Assert.Equal(data.Projects[0].TeamIds, loaded.Projects[0].TeamIds);
        Assert.False(File.Exists(FilePath + ".tmp"));
    }

    [Fact]
    public async Task File_is_readable_json_with_named_enums_and_iso_dates()
    {
        var repository = new JsonErpRepository(FilePath);
        var employee = TestData.Employee(position: Position.QaEngineer);
        await repository.SaveAsync(new ErpData { Employees = [employee] });

        var json = await File.ReadAllTextAsync(FilePath);

        Assert.Contains("\"position\": \"qaEngineer\"", json);
        Assert.Contains("\"birthDate\": \"1990-12-10\"", json);
        Assert.DoesNotContain("fullName", json);
    }

    [Fact]
    public async Task Corrupt_file_is_moved_aside_and_reported()
    {
        Directory.CreateDirectory(_folder);
        await File.WriteAllTextAsync(FilePath, "{ not json");

        var ex = await Assert.ThrowsAsync<InvalidDataException>(() => new JsonErpRepository(FilePath).LoadAsync());

        Assert.False(File.Exists(FilePath));
        Assert.Single(Directory.GetFiles(_folder, "data.json.corrupt-*"));
        Assert.Contains("moved", ex.Message);
    }

    [Fact]
    public async Task Hand_edited_file_is_normalized()
    {
        Directory.CreateDirectory(_folder);
        var id = Guid.NewGuid();
        await File.WriteAllTextAsync(FilePath, $$"""
            {
              "employees": [
                { "id": "{{id}}", "firstName": "Ann", "lastName": null, "kpi": { "teamwork": 140, "leadership": -3 } },
                { "id": "{{id}}", "firstName": "Duplicate" },
                null
              ],
              "projects": [
                { "name": "P", "progress": 120, "teamIds": ["{{id}}", "{{id}}", "{{Guid.NewGuid()}}"] }
              ]
            }
            """);

        var data = await new JsonErpRepository(FilePath).LoadAsync();

        Assert.NotNull(data);
        var employee = Assert.Single(data.Employees);
        Assert.Equal("Ann", employee.FirstName);
        Assert.Equal(string.Empty, employee.LastName);
        Assert.Equal(100, employee.Kpi.Teamwork);
        Assert.Equal(0, employee.Kpi.Leadership);

        var project = Assert.Single(data.Projects);
        Assert.Equal(100, project.Progress);
        Assert.Equal([id], project.TeamIds);
        Assert.Equal(string.Empty, project.Client);
    }

    [Fact]
    public void Default_location_is_in_application_data()
    {
        var location = JsonErpRepository.CreateDefault().Location;

        Assert.EndsWith(Path.Combine("SimpleERP", "data.json"), location);
    }
}
