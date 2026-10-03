using SimpleErp.Core.Models;

namespace SimpleErp.Core.Services;

/// <summary>
/// The single in-memory copy of the data, shared by all pages. Every change is written through the
/// <see cref="IErpRepository"/> immediately; if writing fails, the change is rolled back and the exception rethrown.
/// </summary>
public sealed class ErpStore(IErpRepository repository, TimeProvider time)
{
    private ErpData _data = new();

    /// <summary>Raised after the data has changed (load, save or delete).</summary>
    public event EventHandler? Changed;

    public IReadOnlyList<Employee> Employees => _data.Employees;

    public IReadOnlyList<Project> Projects => _data.Projects;

    public string Location => repository.Location;

    public DateOnly Today => DateOnly.FromDateTime(time.GetLocalNow().Date);

    /// <summary>Loads the data file. On the very first start, creates and saves demo data instead.</summary>
    /// <returns><see langword="true"/> if demo data was created.</returns>
    public async Task<bool> LoadAsync(CancellationToken cancellationToken = default)
    {
        var data = await repository.LoadAsync(cancellationToken).ConfigureAwait(true);
        var seeded = data is null;

        if (data is null)
        {
            data = DemoData.Create(Today);
            await repository.SaveAsync(data, cancellationToken).ConfigureAwait(true);
        }

        _data = data;
        Changed?.Invoke(this, EventArgs.Empty);
        return seeded;
    }

    /// <summary>Starts with no data, e.g. after the data file could not be read.</summary>
    public void Reset()
    {
        _data = new ErpData();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public Employee? FindEmployee(Guid id) => _data.Employees.Find(employee => employee.Id == id);

    public Project? FindProject(Guid id) => _data.Projects.Find(project => project.Id == id);

    /// <summary>Projects that <paramref name="employeeId"/> is assigned to.</summary>
    public IReadOnlyList<Project> ProjectsOf(Guid employeeId) =>
        _data.Projects.Where(project => project.TeamIds.Contains(employeeId)).ToList();

    /// <summary>Adds the employee, or replaces the stored one with the same id. The store keeps its own copy.</summary>
    public Task SaveEmployeeAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(employee);

        return CommitAsync(data => Upsert(data.Employees, employee.Clone(), e => e.Id == employee.Id), cancellationToken);
    }

    /// <summary>Deletes the employee and removes them from every project team.</summary>
    public Task DeleteEmployeeAsync(Guid id, CancellationToken cancellationToken = default) =>
        CommitAsync(data =>
        {
            data.Employees.RemoveAll(employee => employee.Id == id);
            foreach (var project in data.Projects)
            {
                project.TeamIds.Remove(id);
            }
        }, cancellationToken);

    /// <summary>Adds the project, or replaces the stored one with the same id. Unknown team members are dropped.</summary>
    public Task SaveProjectAsync(Project project, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);

        return CommitAsync(data =>
        {
            var copy = project.Clone();
            var known = data.Employees.Select(employee => employee.Id).ToHashSet();
            copy.TeamIds = copy.TeamIds.Where(known.Contains).Distinct().ToList();
            Upsert(data.Projects, copy, p => p.Id == project.Id);
        }, cancellationToken);
    }

    public Task DeleteProjectAsync(Guid id, CancellationToken cancellationToken = default) =>
        CommitAsync(data => data.Projects.RemoveAll(project => project.Id == id), cancellationToken);

    private async Task CommitAsync(Action<ErpData> change, CancellationToken cancellationToken)
    {
        var updated = _data.Clone();
        change(updated);

        // Only replace the live data once it is safely on disk.
        await repository.SaveAsync(updated, cancellationToken).ConfigureAwait(true);
        _data = updated;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private static void Upsert<T>(List<T> items, T item, Predicate<T> sameId)
    {
        var index = items.FindIndex(sameId);
        if (index >= 0)
        {
            items[index] = item;
        }
        else
        {
            items.Add(item);
        }
    }
}
