using SimpleErp.Core.Models;
using SimpleErp.Core.Services;
using SimpleErp.Core.Tests.Fakes;

namespace SimpleErp.Core.Tests;

public class ErpStoreTests
{
    [Fact]
    public async Task First_start_creates_and_saves_demo_data()
    {
        var repository = new InMemoryRepository();
        var store = new ErpStore(repository, TestData.Time);

        var seeded = await store.LoadAsync();

        Assert.True(seeded);
        Assert.NotEmpty(store.Employees);
        Assert.NotEmpty(store.Projects);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task Existing_data_is_loaded_as_is()
    {
        var employee = TestData.Employee();
        var (store, repository) = await TestData.LoadedStoreAsync(new ErpData { Employees = [employee] });

        Assert.Equal(employee.Id, Assert.Single(store.Employees).Id);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task Saving_an_existing_employee_replaces_it_instead_of_adding_a_duplicate()
    {
        var employee = TestData.Employee();
        var (store, _) = await TestData.LoadedStoreAsync(new ErpData { Employees = [employee] });

        var edited = employee.Clone();
        edited.LastName = "Byron";
        await store.SaveEmployeeAsync(edited);

        Assert.Equal("Byron", Assert.Single(store.Employees).LastName);
    }

    [Fact]
    public async Task Saving_a_new_employee_adds_it_and_raises_changed()
    {
        var (store, repository) = await TestData.LoadedStoreAsync(new ErpData());
        var changes = 0;
        store.Changed += (_, _) => changes++;

        await store.SaveEmployeeAsync(TestData.Employee());

        Assert.Single(store.Employees);
        Assert.Single(repository.Stored!.Employees);
        Assert.Equal(1, changes);
    }

    [Fact]
    public async Task Store_keeps_its_own_copy_of_saved_items()
    {
        var (store, _) = await TestData.LoadedStoreAsync(new ErpData());
        var employee = TestData.Employee();

        await store.SaveEmployeeAsync(employee);
        employee.FirstName = "Mutated afterwards";

        Assert.Equal("Ada", store.Employees[0].FirstName);
    }

    [Fact]
    public async Task Deleting_an_employee_removes_them_from_project_teams()
    {
        var a = TestData.Employee("Ann", "A");
        var b = TestData.Employee("Bob", "B");
        var (store, repository) = await TestData.LoadedStoreAsync(new ErpData
        {
            Employees = [a, b],
            Projects = [TestData.Project("P1", a.Id, b.Id), TestData.Project("P2", a.Id)],
        });

        await store.DeleteEmployeeAsync(a.Id);

        Assert.Equal(b.Id, Assert.Single(store.Employees).Id);
        Assert.All(store.Projects, project => Assert.DoesNotContain(a.Id, project.TeamIds));
        Assert.Equal([b.Id], repository.Stored!.Projects[0].TeamIds);
        Assert.Empty(store.ProjectsOf(a.Id));
    }

    [Fact]
    public async Task Saving_a_project_drops_unknown_and_duplicate_team_members()
    {
        var a = TestData.Employee();
        var (store, _) = await TestData.LoadedStoreAsync(new ErpData { Employees = [a] });

        await store.SaveProjectAsync(TestData.Project("P", a.Id, a.Id, Guid.NewGuid()));

        Assert.Equal([a.Id], Assert.Single(store.Projects).TeamIds);
    }

    [Fact]
    public async Task Failed_write_leaves_the_data_unchanged()
    {
        var employee = TestData.Employee();
        var (store, repository) = await TestData.LoadedStoreAsync(new ErpData { Employees = [employee] });
        var changes = 0;
        store.Changed += (_, _) => changes++;
        repository.FailSaves = true;

        await Assert.ThrowsAsync<IOException>(() => store.DeleteEmployeeAsync(employee.Id));
        await Assert.ThrowsAsync<IOException>(() => store.SaveProjectAsync(TestData.Project()));

        Assert.Single(store.Employees);
        Assert.Empty(store.Projects);
        Assert.Equal(0, changes);
    }

    [Fact]
    public async Task Projects_of_an_employee_are_found()
    {
        var a = TestData.Employee();
        var (store, _) = await TestData.LoadedStoreAsync(new ErpData
        {
            Employees = [a],
            Projects = [TestData.Project("Mine", a.Id), TestData.Project("Other")],
        });

        Assert.Equal("Mine", Assert.Single(store.ProjectsOf(a.Id)).Name);
    }

    [Fact]
    public async Task Deleting_a_project_removes_only_that_project()
    {
        var keep = TestData.Project("Keep");
        var remove = TestData.Project("Remove");
        var (store, _) = await TestData.LoadedStoreAsync(new ErpData { Projects = [keep, remove] });

        await store.DeleteProjectAsync(remove.Id);

        Assert.Equal("Keep", Assert.Single(store.Projects).Name);
        Assert.Null(store.FindProject(remove.Id));
    }

    [Fact]
    public void Today_comes_from_the_time_provider() =>
        Assert.Equal(TestData.Today, new ErpStore(new InMemoryRepository(), TestData.Time).Today);
}
