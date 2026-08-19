using BeselerNet.Shared.Core;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace BeselerNet.Shared.Contracts.Budget;

public static class BudgetPackFormat
{
    public const string Name = "beseler.budget";
    public const int Version = 1;
}

public sealed record BudgetPack
{
    public string Format { get; init; } = "";
    public int Version { get; init; }
    [JsonPropertyName("exported_at")]
    public DateTimeOffset? ExportedAt { get; init; }
    public IReadOnlyList<BudgetPackTemplate> Templates { get; init; } = [];
    public BudgetPackYear? Year { get; init; }

    public bool IsInvalid(int year, [NotNullWhen(true)] out Dictionary<string, string[]>? validationErrors)
    {
        var errors = new ErrorCollector();
        if (!string.Equals(Format.Trim(), BudgetPackFormat.Name, StringComparison.OrdinalIgnoreCase))
            errors.Add("format", $"Format must be {BudgetPackFormat.Name}.");
        if (Version != BudgetPackFormat.Version)
            errors.Add("version", $"Version must be {BudgetPackFormat.Version}.");
        if (Year is null)
            errors.Add("year", "Year is required.");
        else
        {
            if (Year.Year != year)
                errors.Add("year.year", $"File year must be {year}.");
            if (Year.StartingBalance is null)
                errors.Add("year.starting_balance", "Starting balance is required.");

            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var signatures = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < Templates.Count; i++)
            {
                var template = Templates[i];
                if (string.IsNullOrWhiteSpace(template.Key))
                    errors.Add("templates", i, "key", "Template key is required.");
                else if (!keys.Add(template.Key.Trim()))
                    errors.Add("templates", i, "key", "Template keys must be unique.");

                var request = template.ToUpsert();
                if (request.IsInvalid(out var templateErrors) && templateErrors is not null)
                {
                    foreach (var pair in templateErrors)
                    {
                        foreach (var message in pair.Value)
                            errors.Add("templates", i, pair.Key, message);
                    }
                }

                if (!string.IsNullOrWhiteSpace(template.Name) && BudgetSections.IsKnown(template.Section) && BudgetSchedules.IsKnown(template.ScheduleType))
                {
                    var signature = BudgetTemplateIdentity.MatchKey(
                        template.Name, template.Section, template.ScheduleType,
                        template.DayOfMonth, template.IntervalDays, template.AnchorDate);
                    if (!signatures.TryAdd(signature, i))
                        errors.Add("templates", i, "key", $"Same schedule as templates[{signatures[signature]}]. Rename or change one.");
                }
            }

            var lineItems = Year.Lines ?? [];
            for (var i = 0; i < lineItems.Count; i++)
            {
                var line = lineItems[i];
                if (line.OnDate is not { } date)
                    errors.Add("year.lines", i, "on_date", "Date is required.");
                else if (date.Year != year)
                    errors.Add("year.lines", i, "on_date", "Date must fall in that year.");

                var month = line.OnDate?.Month ?? 1;
                var request = new UpsertBudgetLineRequest
                {
                    Name = line.Name,
                    Section = line.Section,
                    Amount = line.Amount,
                    OnDate = line.OnDate,
                    Committed = line.Committed
                };
                if (request.IsInvalid(year, month, requireDate: true, out var lineErrors) && lineErrors is not null)
                {
                    foreach (var pair in lineErrors)
                    {
                        foreach (var message in pair.Value)
                            errors.Add("year.lines", i, pair.Key, message);
                    }
                }

                if (!string.IsNullOrWhiteSpace(line.TemplateKey) && !keys.Contains(line.TemplateKey.Trim()))
                    errors.Add("year.lines", i, "template_key", "Template key is not in this file.");
            }
        }

        validationErrors = errors.Collection;
        return errors.Count > 0;
    }
}

public sealed record BudgetPackYear
{
    public int Year { get; init; }
    [JsonPropertyName("starting_balance")]
    public decimal? StartingBalance { get; init; }
    public IReadOnlyList<BudgetPackLine> Lines { get; init; } = [];
}

public sealed record BudgetPackLine
{
    public string Name { get; init; } = "";
    public string Section { get; init; } = "";
    public decimal? Amount { get; init; }
    [JsonPropertyName("on_date")]
    public DateOnly? OnDate { get; init; }
    public bool Committed { get; init; }
    [JsonPropertyName("template_key")]
    public string? TemplateKey { get; init; }
}

public sealed record BudgetPackTemplate
{
    public string Key { get; init; } = "";
    public string Name { get; init; } = "";
    public string Section { get; init; } = "";
    public decimal? Amount { get; init; }
    [JsonPropertyName("schedule_type")]
    public string ScheduleType { get; init; } = "";
    [JsonPropertyName("day_of_month")]
    public int? DayOfMonth { get; init; }
    [JsonPropertyName("interval_days")]
    public int? IntervalDays { get; init; }
    [JsonPropertyName("anchor_date")]
    public DateOnly? AnchorDate { get; init; }

    public UpsertBudgetTemplateRequest ToUpsert() => new()
    {
        Name = Name,
        Section = Section,
        Amount = Amount,
        ScheduleType = ScheduleType,
        DayOfMonth = DayOfMonth,
        IntervalDays = IntervalDays,
        AnchorDate = AnchorDate
    };
}

public static class BudgetTemplateIdentity
{
    public static string MatchKey(
        string name,
        string section,
        string scheduleType,
        int? dayOfMonth,
        int? intervalDays,
        DateOnly? anchorDate)
    {
        var schedule = BudgetSchedules.Normalize(scheduleType);
        var identity = schedule == BudgetSchedules.Monthly
            ? $"monthly:{dayOfMonth}"
            : $"{schedule}:{anchorDate:yyyy-MM-dd}:{BudgetSchedules.StepDays(schedule, intervalDays)}";
        return $"{name.Trim().ToLowerInvariant()}|{BudgetSections.Normalize(section)}|{identity}";
    }

    public static string FileKey(string name, ISet<string> used)
    {
        var slug = Slug(name);
        var key = slug;
        var n = 2;
        while (!used.Add(key))
        {
            key = $"{slug}-{n}";
            n++;
        }

        return key;
    }

    private static string Slug(string name)
    {
        var builder = new StringBuilder();
        var dash = false;
        foreach (var c in name.Trim().ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(c))
            {
                builder.Append(c);
                dash = false;
            }
            else if (builder.Length > 0 && !dash)
            {
                builder.Append('-');
                dash = true;
            }
        }

        if (builder.Length > 0 && builder[^1] == '-')
            builder.Length--;
        return builder.Length == 0 ? "template" : builder.ToString();
    }
}
