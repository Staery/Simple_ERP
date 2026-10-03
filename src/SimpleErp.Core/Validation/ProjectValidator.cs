using SimpleErp.Core.Models;

namespace SimpleErp.Core.Validation;

public static class ProjectValidator
{
    public const int MaxNameLength = 80;
    public const decimal MaxBudget = 1_000_000_000m;

    public static ValidationErrors Validate(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);

        var errors = new ValidationErrors();

        CheckText(errors, nameof(Project.Name), project.Name, "Project name");
        CheckText(errors, nameof(Project.Client), project.Client, "Client");

        if (project.EndDate < project.StartDate)
        {
            errors.Add(nameof(Project.EndDate), "The end date must be on or after the start date.");
        }

        if (project.Budget < 0)
        {
            errors.Add(nameof(Project.Budget), "The budget cannot be negative.");
        }
        else if (project.Budget > MaxBudget)
        {
            errors.Add(nameof(Project.Budget), "The budget is unrealistically large.");
        }

        if (project.Spent < 0)
        {
            errors.Add(nameof(Project.Spent), "Spent cannot be negative.");
        }
        else if (project.Spent > MaxBudget)
        {
            errors.Add(nameof(Project.Spent), "The amount is unrealistically large.");
        }

        if (project.Progress is < 0 or > 100)
        {
            errors.Add(nameof(Project.Progress), "Progress must be between 0 and 100%.");
        }
        else if (project.Status == ProjectStatus.Completed && project.Progress != 100)
        {
            errors.Add(nameof(Project.Progress), "A completed project must be 100% done.");
        }

        return errors;
    }

    private static void CheckText(ValidationErrors errors, string property, string? value, string label)
    {
        var trimmed = value?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            errors.Add(property, $"{label} is required.");
        }
        else if (trimmed.Length > MaxNameLength)
        {
            errors.Add(property, $"{label} must be at most {MaxNameLength} characters.");
        }
    }
}
