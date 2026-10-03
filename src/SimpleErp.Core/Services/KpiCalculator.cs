using SimpleErp.Core.Models;

namespace SimpleErp.Core.Services;

/// <summary>An employee's place in the KPI leaderboard.</summary>
/// <param name="Rank">1 for the best score. Equal scores share a rank (1, 2, 2, 4).</param>
public sealed record EmployeeRanking(Employee Employee, double Score, int Rank)
{
    public string Grade => KpiCalculator.Grade(Score);
}

/// <summary>
/// Turns the five indicator scores into one overall KPI score. Indicators are weighted by position:
/// code efficiency matters most for a developer, leadership for a project manager.
/// </summary>
public static class KpiCalculator
{
    // Weights in percent, in the order of KpiIndicator: Teamwork, CodeEfficiency, DesignSkills, Leadership, ProjectSuccess.
    private static readonly Dictionary<Position, int[]> Weights = new()
    {
        [Position.Developer] = [20, 35, 10, 10, 25],
        [Position.Designer] = [20, 10, 35, 10, 25],
        [Position.QaEngineer] = [25, 25, 10, 10, 30],
        [Position.BusinessAnalyst] = [25, 5, 15, 20, 35],
        [Position.TeamLead] = [20, 20, 5, 30, 25],
        [Position.ProjectManager] = [25, 0, 5, 35, 35],
    };

    /// <summary>Weight of <paramref name="indicator"/> for <paramref name="position"/>, in percent. The weights of a position add up to 100.</summary>
    public static int WeightOf(Position position, KpiIndicator indicator) =>
        Weights.TryGetValue(position, out var weights) ? weights[(int)indicator] : 20;

    /// <summary>Weighted overall score from 0 to 100, rounded to one decimal place.</summary>
    public static double Score(KpiScores scores, Position position)
    {
        ArgumentNullException.ThrowIfNull(scores);

        var total = 0.0;
        foreach (var indicator in KpiScores.Indicators)
        {
            total += Math.Clamp(scores.Get(indicator), KpiScores.MinScore, KpiScores.MaxScore) * WeightOf(position, indicator);
        }

        return Math.Round(total / 100.0, 1, MidpointRounding.AwayFromZero);
    }

    public static double Score(Employee employee)
    {
        ArgumentNullException.ThrowIfNull(employee);
        return Score(employee.Kpi, employee.Position);
    }

    public static string Grade(double score) => score switch
    {
        >= 85 => "Outstanding",
        >= 70 => "Strong",
        >= 55 => "Solid",
        >= 40 => "Developing",
        _ => "Needs attention",
    };

    /// <summary>Ranks employees by overall score, best first. Ties share a rank and are listed by name.</summary>
    public static IReadOnlyList<EmployeeRanking> Rank(IEnumerable<Employee> employees)
    {
        ArgumentNullException.ThrowIfNull(employees);

        var scored = employees
            .Select(employee => (Employee: employee, Score: Score(employee)))
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Employee.LastName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => item.Employee.FirstName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var result = new List<EmployeeRanking>(scored.Count);
        for (var i = 0; i < scored.Count; i++)
        {
            // Standard competition ranking: the rank is one more than the number of strictly better scores.
            var rank = i > 0 && scored[i].Score == scored[i - 1].Score ? result[i - 1].Rank : i + 1;
            result.Add(new EmployeeRanking(scored[i].Employee, scored[i].Score, rank));
        }

        return result;
    }

    /// <summary>Average score of each indicator across <paramref name="employees"/>; zeros when there is nobody.</summary>
    public static IReadOnlyList<ChartPoint> TeamAverages(IReadOnlyCollection<Employee> employees)
    {
        ArgumentNullException.ThrowIfNull(employees);

        return KpiScores.Indicators
            .Select(indicator => new ChartPoint(
                DisplayNames.ShortFor(indicator),
                employees.Count == 0 ? 0 : Math.Round(employees.Average(employee => employee.Kpi.Get(indicator)), 1)))
            .ToList();
    }

    /// <summary>The indicator scores of one employee as chart points, in <see cref="KpiIndicator"/> order.</summary>
    public static IReadOnlyList<ChartPoint> ToChart(KpiScores scores)
    {
        ArgumentNullException.ThrowIfNull(scores);
        return KpiScores.Indicators.Select(indicator => new ChartPoint(DisplayNames.ShortFor(indicator), scores.Get(indicator))).ToList();
    }
}
