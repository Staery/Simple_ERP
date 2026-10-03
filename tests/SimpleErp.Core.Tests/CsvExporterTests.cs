using System.Globalization;
using System.Text;
using SimpleErp.Core.Models;
using SimpleErp.Core.Services;
using SimpleErp.Core.Tests.Fakes;

namespace SimpleErp.Core.Tests;

public class CsvExporterTests
{
    [Theory]
    [InlineData("plain", "plain")]
    [InlineData("with, comma", "\"with, comma\"")]
    [InlineData("say \"hi\"", "\"say \"\"hi\"\"\"")]
    [InlineData("two\nlines", "\"two\nlines\"")]
    [InlineData("=HYPERLINK(\"x\")", "\"'=HYPERLINK(\"\"x\"\")\"")]
    [InlineData("-42", "-42")]
    [InlineData("", "")]
    public void Fields_are_escaped(string value, string expected) => Assert.Equal(expected, CsvExporter.Escape(value));

    [Fact]
    public void Employees_are_exported_in_rank_order_with_invariant_numbers()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("ru-RU");
        try
        {
            var csv = CsvExporter.Employees(
                [TestData.Employee("Low", "Score", kpi: 40, salary: 5_000.5m), TestData.Employee("High", "Score", kpi: 90)],
                TestData.Today);

            var lines = csv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(3, lines.Length);
            Assert.StartsWith("Rank,First name,Last name,Position,Email,City,Age,Hire date,Monthly salary,Teamwork,", lines[0]);
            Assert.EndsWith("KPI score,Grade", lines[0]);
            Assert.StartsWith("1,High,Score,Developer,", lines[1]);
            Assert.Contains(",35,2020-02-01,7000.00,90,90,90,90,90,90.0,Outstanding", lines[1]);
            Assert.Contains(",5000.50,", lines[2]);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Projects_list_team_names_health_and_cost()
    {
        var a = TestData.Employee("Ann", "Lee", salary: 6_000m);
        var b = TestData.Employee("Bo", "Kim", salary: 4_000m);
        var project = TestData.Project("Portal, phase 2", a.Id, b.Id);

        var csv = CsvExporter.Projects([project], [a, b], TestData.Today);
        var row = csv.Split("\r\n")[1];

        Assert.StartsWith("\"Portal, phase 2\",Contoso,Active,On track,2026-01-25,2026-05-05,50,100000.00,40000.00,40,2,Ann Lee; Bo Kim,10000.00", row);
    }

    [Fact]
    public async Task Saved_file_is_utf8_with_bom()
    {
        var path = Path.Combine(Path.GetTempPath(), $"simple-erp-{Guid.NewGuid():N}.csv");
        try
        {
            await CsvExporter.SaveAsync(path, "Имя,Город\r\n");

            var bytes = await File.ReadAllBytesAsync(path);
            Assert.Equal([0xEF, 0xBB, 0xBF], bytes.Take(3));
            Assert.Equal("Имя,Город\r\n", Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
