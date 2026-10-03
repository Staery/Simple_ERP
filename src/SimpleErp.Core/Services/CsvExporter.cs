using System.Globalization;
using System.Text;
using SimpleErp.Core.Models;

namespace SimpleErp.Core.Services;

/// <summary>
/// Writes employees and projects as CSV (RFC 4180): comma-separated, invariant number and date formats,
/// UTF-8 with a byte order mark so that Excel detects the encoding.
/// </summary>
public static class CsvExporter
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public static string Employees(IReadOnlyCollection<Employee> employees, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(employees);

        var builder = new StringBuilder();
        AppendRow(builder,
        [
            "Rank", "First name", "Last name", "Position", "Email", "City", "Age", "Hire date", "Monthly salary",
            .. KpiScores.Indicators.Select(DisplayNames.For), "KPI score", "Grade",
        ]);

        foreach (var ranking in KpiCalculator.Rank(employees))
        {
            var employee = ranking.Employee;
            AppendRow(builder,
            [
                ranking.Rank.ToString(Invariant),
                employee.FirstName,
                employee.LastName,
                DisplayNames.For(employee.Position),
                employee.Email,
                employee.City,
                employee.AgeOn(today).ToString(Invariant),
                employee.HireDate.ToString("yyyy-MM-dd", Invariant),
                employee.MonthlySalary.ToString("0.00", Invariant),
                .. KpiScores.Indicators.Select(indicator => employee.Kpi.Get(indicator).ToString(Invariant)),
                ranking.Score.ToString("0.0", Invariant),
                ranking.Grade,
            ]);
        }

        return builder.ToString();
    }

    public static string Projects(IReadOnlyCollection<Project> projects, IReadOnlyCollection<Employee> employees, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(projects);
        ArgumentNullException.ThrowIfNull(employees);

        var names = employees.ToDictionary(employee => employee.Id, employee => employee.FullName);
        var builder = new StringBuilder();
        AppendRow(builder,
        [
            "Name", "Client", "Status", "Health", "Start", "End", "Progress %", "Budget", "Spent", "Budget used %",
            "Team size", "Team", "Monthly team cost",
        ]);

        foreach (var project in projects.OrderBy(project => project.StartDate).ThenBy(project => project.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            var insight = ProjectAnalyzer.Analyze(project, today);
            var team = project.TeamIds.Where(names.ContainsKey).Select(id => names[id]).ToList();

            AppendRow(builder,
            [
                project.Name,
                project.Client,
                DisplayNames.For(project.Status),
                insight.HealthText,
                project.StartDate.ToString("yyyy-MM-dd", Invariant),
                project.EndDate.ToString("yyyy-MM-dd", Invariant),
                project.Progress.ToString(Invariant),
                project.Budget.ToString("0.00", Invariant),
                project.Spent.ToString("0.00", Invariant),
                (insight.BudgetUsed * 100).ToString("0", Invariant),
                team.Count.ToString(Invariant),
                string.Join("; ", team),
                ProjectAnalyzer.MonthlyTeamCost(project, employees).ToString("0.00", Invariant),
            ]);
        }

        return builder.ToString();
    }

    /// <summary>Writes <paramref name="csv"/> to <paramref name="path"/> as UTF-8 with a byte order mark.</summary>
    public static Task SaveAsync(string path, string csv, CancellationToken cancellationToken = default) =>
        File.WriteAllTextAsync(path, csv, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), cancellationToken);

    /// <summary>Quotes a field when it contains a separator, a quote or a line break; quotes inside are doubled.</summary>
    internal static string Escape(string? value)
    {
        value ??= string.Empty;

        // A leading = + - @ would make spreadsheet apps evaluate the cell as a formula.
        if (value.Length > 0 && value[0] is '=' or '+' or '-' or '@' && !double.TryParse(value, NumberStyles.Float, Invariant, out _))
        {
            value = "'" + value;
        }

        return value.IndexOfAny([',', '"', '\r', '\n']) >= 0
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;
    }

    private static void AppendRow(StringBuilder builder, IEnumerable<string> fields) =>
        builder.Append(string.Join(',', fields.Select(Escape))).Append("\r\n");
}
