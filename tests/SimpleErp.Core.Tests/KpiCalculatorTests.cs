using SimpleErp.Core.Models;
using SimpleErp.Core.Services;
using SimpleErp.Core.Tests.Fakes;

namespace SimpleErp.Core.Tests;

public class KpiCalculatorTests
{
    [Theory]
    [MemberData(nameof(AllPositions))]
    public void Weights_of_every_position_add_up_to_100(Position position)
    {
        var total = KpiScores.Indicators.Sum(indicator => KpiCalculator.WeightOf(position, indicator));

        Assert.Equal(100, total);
    }

    [Fact]
    public void Score_of_equal_indicators_is_that_value_for_any_position()
    {
        var scores = new KpiScores { Teamwork = 64, CodeEfficiency = 64, DesignSkills = 64, Leadership = 64, ProjectSuccess = 64 };

        Assert.All(Enum.GetValues<Position>(), position => Assert.Equal(64, KpiCalculator.Score(scores, position)));
    }

    [Fact]
    public void Score_weights_indicators_by_position()
    {
        var coder = new KpiScores { Teamwork = 50, CodeEfficiency = 100, DesignSkills = 50, Leadership = 50, ProjectSuccess = 50 };

        // Developer: 50·20% + 100·35% + 50·10% + 50·10% + 50·25% = 67.5; project manager weighs code at 0%.
        Assert.Equal(67.5, KpiCalculator.Score(coder, Position.Developer));
        Assert.Equal(50, KpiCalculator.Score(coder, Position.ProjectManager));
    }

    [Fact]
    public void Score_clamps_out_of_range_values()
    {
        var scores = new KpiScores { Teamwork = 250, CodeEfficiency = 250, DesignSkills = 250, Leadership = 250, ProjectSuccess = -40 };

        var score = KpiCalculator.Score(scores, Position.Designer);

        Assert.InRange(score, 0, 100);
        Assert.Equal(75, score);
    }

    [Theory]
    [InlineData(92.0, "Outstanding")]
    [InlineData(85.0, "Outstanding")]
    [InlineData(84.9, "Strong")]
    [InlineData(70.0, "Strong")]
    [InlineData(55.0, "Solid")]
    [InlineData(40.0, "Developing")]
    [InlineData(39.9, "Needs attention")]
    public void Grade_follows_thresholds(double score, string grade) => Assert.Equal(grade, KpiCalculator.Grade(score));

    [Fact]
    public void Rank_uses_competition_ranking_for_ties()
    {
        var employees = new[]
        {
            TestData.Employee("Cara", "Young", kpi: 60),
            TestData.Employee("Ann", "Best", kpi: 90),
            TestData.Employee("Bob", "Twin", kpi: 75),
            TestData.Employee("Ben", "Alpha", kpi: 75),
        };

        var ranking = KpiCalculator.Rank(employees);

        Assert.Equal(["Ann Best", "Ben Alpha", "Bob Twin", "Cara Young"], ranking.Select(r => r.Employee.FullName));
        Assert.Equal([1, 2, 2, 4], ranking.Select(r => r.Rank));
    }

    [Fact]
    public void Rank_of_nobody_is_empty() => Assert.Empty(KpiCalculator.Rank([]));

    [Fact]
    public void Team_averages_cover_every_indicator()
    {
        var averages = KpiCalculator.TeamAverages([TestData.Employee(kpi: 40), TestData.Employee(kpi: 81)]);

        Assert.Equal(KpiScores.Indicators.Count, averages.Count);
        Assert.All(averages, point => Assert.Equal(60.5, point.Value));
        Assert.Equal("Teamwork", averages[0].Label);
    }

    [Fact]
    public void Team_averages_of_nobody_are_zero() =>
        Assert.All(KpiCalculator.TeamAverages([]), point => Assert.Equal(0, point.Value));

    [Fact]
    public void Kpi_scores_get_and_set_every_indicator()
    {
        var scores = new KpiScores();
        foreach (var (indicator, i) in KpiScores.Indicators.Select((indicator, i) => (indicator, i)))
        {
            scores.Set(indicator, 10 + i);
        }

        Assert.Equal([10, 11, 12, 13, 14], KpiScores.Indicators.Select(scores.Get));
        Assert.Equal(12, scores.DesignSkills);
    }

    public static TheoryData<Position> AllPositions() => new(Enum.GetValues<Position>());
}
