namespace SimpleErp.Core.Models;

/// <summary>An employee's scores for the <see cref="KpiIndicator"/> values, each in the range 0–100.</summary>
public sealed class KpiScores
{
    public const int MinScore = 0;
    public const int MaxScore = 100;

    public int Teamwork { get; set; }

    public int CodeEfficiency { get; set; }

    public int DesignSkills { get; set; }

    public int Leadership { get; set; }

    /// <summary>Share of the employee's projects that were delivered successfully, in percent.</summary>
    public int ProjectSuccess { get; set; }

    public static IReadOnlyList<KpiIndicator> Indicators { get; } = Enum.GetValues<KpiIndicator>();

    public int Get(KpiIndicator indicator) => indicator switch
    {
        KpiIndicator.Teamwork => Teamwork,
        KpiIndicator.CodeEfficiency => CodeEfficiency,
        KpiIndicator.DesignSkills => DesignSkills,
        KpiIndicator.Leadership => Leadership,
        KpiIndicator.ProjectSuccess => ProjectSuccess,
        _ => throw new ArgumentOutOfRangeException(nameof(indicator), indicator, null),
    };

    public void Set(KpiIndicator indicator, int value)
    {
        switch (indicator)
        {
            case KpiIndicator.Teamwork: Teamwork = value; break;
            case KpiIndicator.CodeEfficiency: CodeEfficiency = value; break;
            case KpiIndicator.DesignSkills: DesignSkills = value; break;
            case KpiIndicator.Leadership: Leadership = value; break;
            case KpiIndicator.ProjectSuccess: ProjectSuccess = value; break;
            default: throw new ArgumentOutOfRangeException(nameof(indicator), indicator, null);
        }
    }

    public KpiScores Clone() => (KpiScores)MemberwiseClone();
}
