using SimpleErp.Core.Models;

namespace SimpleErp.Core.Services;

/// <summary>
/// Realistic sample data for the first start, so the dashboard and charts have something to show.
/// The same <c>today</c> always gives the same data; dates are relative to it.
/// </summary>
public static class DemoData
{
    private const int Seed = 2018;

    private static readonly (string First, string Last, Position Position)[] People =
    [
        ("Olivia", "Bennett", Position.ProjectManager),
        ("Marcus", "Reed", Position.TeamLead),
        ("Sophia", "Turner", Position.Developer),
        ("Ethan", "Brooks", Position.Developer),
        ("Ava", "Coleman", Position.Designer),
        ("Liam", "Foster", Position.Developer),
        ("Isabella", "Hayes", Position.BusinessAnalyst),
        ("Noah", "Patterson", Position.QaEngineer),
        ("Mia", "Russell", Position.Developer),
        ("James", "Griffin", Position.TeamLead),
        ("Charlotte", "Sanders", Position.Designer),
        ("Benjamin", "Price", Position.Developer),
        ("Amelia", "Ward", Position.QaEngineer),
        ("Lucas", "Barnes", Position.Developer),
        ("Harper", "Ross", Position.ProjectManager),
        ("Henry", "Jenkins", Position.Developer),
        ("Evelyn", "Perry", Position.BusinessAnalyst),
        ("Alexander", "Powell", Position.Developer),
        ("Abigail", "Long", Position.Designer),
        ("Daniel", "Hughes", Position.QaEngineer),
        ("Emily", "Flores", Position.Developer),
        ("Matthew", "Butler", Position.TeamLead),
        ("Ella", "Simmons", Position.Developer),
        ("Jack", "Myers", Position.Developer),
    ];

    private static readonly string[] Cities =
    [
        "Austin", "Denver", "Seattle", "Portland", "Chicago", "Boston", "San Diego", "Raleigh", "Minneapolis", "Remote",
    ];

    /// <summary>Typical monthly salary range (USD) per position.</summary>
    private static readonly Dictionary<Position, (int Min, int Max)> SalaryBands = new()
    {
        [Position.Developer] = (6_200, 10_500),
        [Position.Designer] = (5_400, 8_600),
        [Position.QaEngineer] = (4_800, 7_400),
        [Position.BusinessAnalyst] = (5_600, 8_400),
        [Position.TeamLead] = (9_800, 12_800),
        [Position.ProjectManager] = (8_600, 11_900),
    };

    /// <summary>Typical score per indicator (Teamwork, Code, Design, Leadership, Success) for each position.</summary>
    private static readonly Dictionary<Position, int[]> TypicalKpi = new()
    {
        [Position.Developer] = [74, 80, 48, 52, 76],
        [Position.Designer] = [78, 42, 84, 50, 74],
        [Position.QaEngineer] = [80, 66, 46, 48, 80],
        [Position.BusinessAnalyst] = [82, 36, 60, 66, 78],
        [Position.TeamLead] = [80, 78, 50, 82, 80],
        [Position.ProjectManager] = [86, 30, 52, 86, 82],
    };

    public static ErpData Create(DateOnly today)
    {
        var random = new Random(Seed);
        var employees = People.Select((person, index) => CreateEmployee(person, index, today, random)).ToList();
        var projects = CreateProjects(today, employees);

        return new ErpData { Employees = employees, Projects = projects };
    }

    private static Employee CreateEmployee((string First, string Last, Position Position) person, int index, DateOnly today, Random random)
    {
        var age = random.Next(23, 52);
        var birthDate = today.AddYears(-age).AddDays(-random.Next(0, 365));
        var yearsInCompany = Math.Min(random.Next(0, 8), age - 22);
        var hireDate = today.AddYears(-yearsInCompany).AddDays(-random.Next(20, 330));

        var band = SalaryBands[person.Position];
        var salary = Math.Round((decimal)(band.Min + (random.NextDouble() * (band.Max - band.Min))) / 50m) * 50m;

        // A per-person "form" shifts all indicators a little, so the leaderboard has a clear spread.
        var form = random.Next(-14, 13);
        var kpi = new KpiScores();
        foreach (var indicator in KpiScores.Indicators)
        {
            var typical = TypicalKpi[person.Position][(int)indicator];
            kpi.Set(indicator, Math.Clamp(typical + form + random.Next(-9, 10), 12, 99));
        }

        return new Employee
        {
            Id = DeterministicId(index),
            FirstName = person.First,
            LastName = person.Last,
            Email = $"{person.First}.{person.Last}@simple-erp.example".ToLowerInvariant(),
            City = Cities[random.Next(Cities.Length)],
            Position = person.Position,
            BirthDate = birthDate,
            HireDate = hireDate,
            MonthlySalary = salary,
            Kpi = kpi,
        };
    }

    private static List<Project> CreateProjects(DateOnly today, List<Employee> employees)
    {
        Guid[] Team(params int[] indexes) => indexes.Select(i => employees[i].Id).ToArray();

        Project Make(int id, string name, string client, string description, ProjectStatus status,
            int startOffset, int endOffset, decimal budget, decimal spent, int progress, Guid[] team) => new()
        {
            Id = DeterministicId(100 + id),
            Name = name,
            Client = client,
            Description = description,
            Status = status,
            StartDate = today.AddDays(startOffset),
            EndDate = today.AddDays(endOffset),
            Budget = budget,
            Spent = spent,
            Progress = progress,
            TeamIds = [.. team],
        };

        return
        [
            Make(1, "Warehouse Management Suite", "Northwind Traders",
                "Barcode receiving, stock moves and pick lists for three regional warehouses.",
                ProjectStatus.Active, -150, 75, 420_000m, 251_000m, 64, Team(0, 2, 3, 7, 10)),
            Make(2, "Customer Portal Redesign", "Contoso Retail",
                "New self-service portal: order history, invoices and support tickets.",
                ProjectStatus.Active, -60, 90, 185_000m, 61_500m, 38, Team(14, 4, 8, 12)),
            Make(3, "Mobile Field Service App", "Fabrikam Inc.",
                "Offline-first app for technicians: work orders, photos and signatures.",
                ProjectStatus.Active, -120, 30, 260_000m, 214_000m, 52, Team(1, 5, 13, 18, 19)),
            Make(4, "Payroll Integration", "Adventure Works",
                "Two-way sync between the HR system and the payroll provider.",
                ProjectStatus.Active, -95, 40, 120_000m, 131_800m, 81, Team(9, 11, 16)),
            Make(5, "E-commerce Checkout", "Proseware",
                "One-page checkout with saved cards and address autocomplete.",
                ProjectStatus.Active, -110, -6, 98_000m, 90_200m, 91, Team(21, 15, 20, 22)),
            Make(6, "Sales Analytics Dashboard", "Tailspin Toys",
                "Self-service BI over the sales warehouse with daily refresh.",
                ProjectStatus.Planned, 21, 160, 150_000m, 0m, 0, Team(6, 23)),
            Make(7, "Inventory Forecasting", "Litware",
                "Demand forecasting model feeding automatic purchase suggestions.",
                ProjectStatus.OnHold, -80, 100, 210_000m, 48_000m, 22, Team(17)),
            Make(8, "Legacy ERP Migration", "Wide World Importers",
                "Moved 12 years of orders and customers from the old ERP to the cloud.",
                ProjectStatus.Completed, -330, -45, 340_000m, 326_500m, 100, Team(0, 1, 3, 6, 9)),
        ];
    }

    /// <summary>Stable ids keep the demo data identical between runs, which also keeps screenshots and tests reproducible.</summary>
    private static Guid DeterministicId(int number) => new($"5e4d0000-0000-4000-8000-{number:D12}");
}
