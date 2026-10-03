using SimpleErp.Core.Models;
using SimpleErp.Core.Tests.Fakes;
using SimpleErp.Core.ViewModels;

namespace SimpleErp.Core.Tests;

public class EmployeeEditorViewModelTests
{
    private static EmployeeEditorViewModel Editor(Employee? employee = null, bool isNew = false) =>
        new(employee ?? TestData.Employee(), isNew, TestData.Today);

    [Fact]
    public void Loaded_editor_is_clean_and_shows_the_values()
    {
        var editor = Editor();

        Assert.False(editor.IsDirty);
        Assert.False(editor.HasErrors);
        Assert.Equal("Ada Lovelace", editor.FullName);
        Assert.Equal("AL", editor.Initials);
        Assert.Equal("age 35", editor.AgeText);
        Assert.Equal("6 years here", editor.TenureText);
        Assert.Equal(70, editor.Score);
    }

    [Fact]
    public void Editing_marks_dirty_and_revert_restores()
    {
        var editor = Editor();

        editor.LastName = "Byron";
        Assert.True(editor.IsDirty);

        editor.Revert();
        Assert.False(editor.IsDirty);
        Assert.Equal("Lovelace", editor.LastName);
    }

    [Fact]
    public void Kpi_changes_update_score_and_chart_live()
    {
        var editor = Editor();
        var changed = new List<string?>();
        editor.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        editor.CodeEfficiency = 100;

        Assert.Equal(80.5, editor.Score); // 70 + 30 × 35%
        Assert.Equal(100, editor.KpiChart[(int)KpiIndicator.CodeEfficiency].Value);
        Assert.Contains(nameof(EmployeeEditorViewModel.Score), changed);
        Assert.Contains(nameof(EmployeeEditorViewModel.KpiChart), changed);
    }

    [Fact]
    public void Changing_position_changes_the_weighting()
    {
        var employee = TestData.Employee();
        employee.Kpi.Leadership = 100;
        var editor = Editor(employee);
        var asDeveloper = editor.Score;

        editor.Position = Position.ProjectManager;

        Assert.True(editor.Score > asDeveloper);
        Assert.Equal(35, editor.Weights[(int)KpiIndicator.Leadership].Value);
    }

    [Fact]
    public void Errors_appear_only_for_edited_fields_until_validate_all()
    {
        var editor = Editor(new Employee(), isNew: true);

        Assert.False(editor.HasErrors);

        editor.FirstName = "Grace";
        editor.FirstName = string.Empty;
        Assert.Single(editor.GetErrors(nameof(EmployeeEditorViewModel.FirstName)).Cast<string>());
        Assert.Empty(editor.GetErrors(nameof(EmployeeEditorViewModel.LastName)).Cast<string>());

        Assert.False(editor.ValidateAll());
        Assert.NotEmpty(editor.GetErrors(nameof(EmployeeEditorViewModel.LastName)).Cast<string>());
        Assert.Equal("Pick the birth date.", editor.GetErrors(nameof(EmployeeEditorViewModel.BirthDate)).Cast<string>().Single());
        Assert.StartsWith("Please fix the", editor.ErrorSummary);
    }

    [Fact]
    public void Errors_changed_is_raised_when_a_field_becomes_invalid_and_valid_again()
    {
        var editor = Editor();
        var raised = new List<string?>();
        editor.ErrorsChanged += (_, e) => raised.Add(e.PropertyName);

        editor.MonthlySalary = "lots";
        Assert.Equal("Enter a number, e.g. 5200.", editor.GetErrors(nameof(EmployeeEditorViewModel.MonthlySalary)).Cast<string>().Single());

        editor.MonthlySalary = "9100";
        Assert.False(editor.HasErrors);
        Assert.Equal([nameof(EmployeeEditorViewModel.MonthlySalary), nameof(EmployeeEditorViewModel.MonthlySalary)], raised);
    }

    [Fact]
    public void To_employee_trims_text_and_keeps_the_id()
    {
        var original = TestData.Employee();
        var editor = Editor(original);
        editor.FirstName = "  Augusta ";
        editor.MonthlySalary = "8100";
        editor.HireDate = new DateTime(2021, 5, 4);

        var result = editor.ToEmployee();

        Assert.Equal(original.Id, result.Id);
        Assert.Equal("Augusta", result.FirstName);
        Assert.Equal(8100m, result.MonthlySalary);
        Assert.Equal(new DateOnly(2021, 5, 4), result.HireDate);
    }

    [Fact]
    public void Accept_saved_makes_the_values_the_new_baseline()
    {
        var editor = Editor(isNew: true);
        editor.City = "Paris";

        editor.AcceptSaved(editor.ToEmployee());

        Assert.False(editor.IsNew);
        Assert.False(editor.IsDirty);
        editor.Revert();
        Assert.Equal("Paris", editor.City);
    }
}

public class ProjectEditorViewModelTests
{
    private static readonly Employee Ann = TestData.Employee("Ann", "Lee", salary: 6_000m);
    private static readonly Employee Bob = TestData.Employee("Bob", "Kim", salary: 4_000m);

    private static ProjectEditorViewModel Editor(Project? project = null, bool isNew = false) =>
        new(project ?? TestData.Project("Portal", Ann.Id), isNew, [Ann, Bob], TestData.Today);

    [Fact]
    public void Team_options_list_everyone_with_members_selected()
    {
        var editor = Editor();

        // Current members first, then everyone else by last name.
        Assert.Equal(["Ann Lee", "Bob Kim"], editor.TeamOptions.Select(o => o.FullName));
        Assert.Equal(1, editor.TeamSize);
        Assert.Equal(6_000m, editor.TeamMonthlyCost);
        Assert.False(editor.IsDirty);
    }

    [Fact]
    public void Toggling_a_team_member_marks_dirty_and_updates_cost()
    {
        var editor = Editor();

        editor.TeamOptions.Single(o => o.Id == Bob.Id).IsSelected = true;

        Assert.True(editor.IsDirty);
        Assert.Equal(10_000m, editor.TeamMonthlyCost);
        Assert.Equal(2, editor.ToProject().TeamIds.Count);
    }

    [Fact]
    public void Insight_reflects_the_values_in_the_form()
    {
        var editor = Editor();
        Assert.Equal(Services.ProjectHealth.OnTrack, editor.Insight!.Health);

        editor.Spent = "120000";

        Assert.Equal(Services.ProjectHealth.OverBudget, editor.Insight!.Health);
    }

    [Fact]
    public void Insight_is_null_without_dates()
    {
        var editor = Editor();

        editor.StartDate = null;

        Assert.Null(editor.Insight);
        Assert.Equal("Pick the start date.", editor.GetErrors(nameof(ProjectEditorViewModel.StartDate)).Cast<string>().Single());
    }

    [Fact]
    public void Completing_a_project_sets_progress_to_100()
    {
        var editor = Editor();

        editor.Status = ProjectStatus.Completed;

        Assert.Equal(100, editor.Progress);
        Assert.True(editor.ValidateAll());
    }

    [Fact]
    public void End_before_start_is_reported_on_the_end_date()
    {
        var editor = Editor();

        editor.EndDate = editor.StartDate!.Value.AddDays(-1);

        Assert.Contains("on or after", editor.GetErrors(nameof(ProjectEditorViewModel.EndDate)).Cast<string>().Single());
    }

    [Fact]
    public void Budget_is_required_but_spent_may_be_left_empty()
    {
        var editor = Editor();

        editor.Budget = "";
        editor.Spent = "";

        Assert.False(editor.ValidateAll());
        Assert.NotEmpty(editor.GetErrors(nameof(ProjectEditorViewModel.Budget)).Cast<string>());
        Assert.Empty(editor.GetErrors(nameof(ProjectEditorViewModel.Spent)).Cast<string>());
        Assert.Equal(0, editor.ToProject().Spent);
    }
}
