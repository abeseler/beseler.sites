using BeselerNet.Shared.Core;
using System.Diagnostics.CodeAnalysis;

namespace BeselerNet.Shared.Contracts.Budget;

public sealed record BudgetYearsResponse
{
    public required IReadOnlyList<int> Years { get; init; }
}

public sealed record StartBudgetYearRequest
{
    [JsonPropertyName("starting_balance")]
    public decimal? StartingBalance { get; init; }

    public bool IsInvalid([NotNullWhen(true)] out Dictionary<string, string[]>? validationErrors)
    {
        var errors = new ErrorCollector();
        if (StartingBalance is null)
            errors.Add("starting_balance", "Starting balance is required.");
        validationErrors = errors.Collection;
        return errors.Count > 0;
    }
}

public sealed record SetStartingBalanceRequest
{
    [JsonPropertyName("starting_balance")]
    public decimal? StartingBalance { get; init; }

    public bool IsInvalid([NotNullWhen(true)] out Dictionary<string, string[]>? validationErrors)
    {
        var errors = new ErrorCollector();
        if (StartingBalance is null)
            errors.Add("starting_balance", "Starting balance is required.");
        validationErrors = errors.Collection;
        return errors.Count > 0;
    }
}

public sealed record BudgetYearResponse
{
    public required int Year { get; init; }
    [JsonPropertyName("starting_balance")]
    public required decimal StartingBalance { get; init; }
    public required IReadOnlyList<BudgetMonthSummary> Months { get; init; }
    [JsonPropertyName("checking_now")]
    public decimal? CheckingNow { get; init; }
}

public sealed record BudgetMonthSummary
{
    public required int Month { get; init; }
    [JsonPropertyName("starting_balance")]
    public required decimal StartingBalance { get; init; }
    public required decimal Income { get; init; }
    public required decimal Expenses { get; init; }
    public required decimal Savings { get; init; }
    [JsonPropertyName("cash_flow")]
    public required decimal CashFlow { get; init; }
    [JsonPropertyName("ending_balance")]
    public required decimal EndingBalance { get; init; }
}

public sealed record BudgetMonthResponse
{
    public required int Year { get; init; }
    public required int Month { get; init; }
    [JsonPropertyName("starting_balance")]
    public required decimal StartingBalance { get; init; }
    public required decimal Income { get; init; }
    public required decimal Expenses { get; init; }
    public required decimal Savings { get; init; }
    [JsonPropertyName("cash_flow")]
    public required decimal CashFlow { get; init; }
    [JsonPropertyName("ending_balance")]
    public required decimal EndingBalance { get; init; }
    public required IReadOnlyList<BudgetLineResponse> Lines { get; init; }
    public required IReadOnlyList<BudgetDayBalance> Days { get; init; }
    [JsonPropertyName("suggested_names")]
    public IReadOnlyDictionary<string, IReadOnlyList<string>> SuggestedNames { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>();
}

public sealed record BudgetDayBalance
{
    public required int Day { get; init; }
    public required decimal Balance { get; init; }
}

public sealed record BudgetLineResponse
{
    [JsonPropertyName("line_id")]
    public required int LineId { get; init; }
    public required string Name { get; init; }
    public required string Section { get; init; }
    public decimal? Amount { get; init; }
    [JsonPropertyName("on_date")]
    public DateOnly? OnDate { get; init; }
    public required bool Committed { get; init; }
    [JsonPropertyName("template_id")]
    public int? TemplateId { get; init; }
}

public sealed record UpsertBudgetLineRequest
{
    public string Name { get; init; } = "";
    public string Section { get; init; } = "";
    public decimal? Amount { get; init; }
    [JsonPropertyName("on_date")]
    public DateOnly? OnDate { get; init; }
    public bool? Committed { get; init; }

    public bool IsInvalid(int year, int month, bool requireDate, [NotNullWhen(true)] out Dictionary<string, string[]>? validationErrors)
    {
        var errors = new ErrorCollector();
        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("name", "Name is required.");
        else if (Name.Trim().Length > 128)
            errors.Add("name", "Name must be 128 characters or fewer.");

        if (string.IsNullOrWhiteSpace(Section))
            errors.Add("section", "Section is required.");
        else if (!BudgetSections.IsKnown(Section))
            errors.Add("section", "Section must be income, expense, or savings.");

        if (requireDate && OnDate is null)
            errors.Add("on_date", "Date is required.");
        else if (OnDate is { } date && (date.Year != year || date.Month != month))
            errors.Add("on_date", "Date must fall in that month.");

        if (BudgetSections.IsKnown(Section) && BudgetSections.AmountError(Section, Amount) is { } amountError)
            errors.Add("amount", amountError);

        validationErrors = errors.Collection;
        return errors.Count > 0;
    }
}

public sealed record BudgetTemplateResponse
{
    [JsonPropertyName("template_id")]
    public required int TemplateId { get; init; }
    public required string Name { get; init; }
    public required string Section { get; init; }
    public decimal? Amount { get; init; }
    [JsonPropertyName("schedule_type")]
    public required string ScheduleType { get; init; }
    [JsonPropertyName("day_of_month")]
    public int? DayOfMonth { get; init; }
    [JsonPropertyName("interval_days")]
    public int? IntervalDays { get; init; }
    [JsonPropertyName("anchor_date")]
    public DateOnly? AnchorDate { get; init; }
}

public sealed record UpsertBudgetTemplateRequest
{
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

    public bool IsInvalid([NotNullWhen(true)] out Dictionary<string, string[]>? validationErrors)
    {
        var errors = new ErrorCollector();
        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("name", "Name is required.");
        else if (Name.Trim().Length > 128)
            errors.Add("name", "Name must be 128 characters or fewer.");

        if (string.IsNullOrWhiteSpace(Section))
            errors.Add("section", "Section is required.");
        else if (!BudgetSections.IsKnown(Section))
            errors.Add("section", "Section must be income, expense, or savings.");
        else if (BudgetSections.AmountError(Section, Amount) is { } amountError)
            errors.Add("amount", amountError);

        var schedule = BudgetSchedules.Normalize(ScheduleType);
        if (!BudgetSchedules.IsKnown(schedule))
            errors.Add("schedule_type", "Schedule must be monthly, weekly, every 2 weeks, or every N days.");
        else if (schedule == BudgetSchedules.Monthly)
        {
            if (DayOfMonth is null or < 1 or > 31)
                errors.Add("day_of_month", "Day of month is required (1-31). Months with fewer days use the last day of that month.");
        }
        else
        {
            if (AnchorDate is null)
                errors.Add("anchor_date", "A start date is required for this schedule.");
            if (schedule == BudgetSchedules.Interval && IntervalDays is null or < 1 or > 365)
                errors.Add("interval_days", "Interval must be between 1 and 365 days.");
        }

        validationErrors = errors.Collection;
        return errors.Count > 0;
    }
}

public static class BudgetSections
{
    public const string Income = "income";
    public const string Expense = "expense";
    public const string Savings = "savings";

    public static bool IsKnown(string value) =>
        value.Equals(Income, StringComparison.OrdinalIgnoreCase)
        || value.Equals(Expense, StringComparison.OrdinalIgnoreCase)
        || value.Equals(Savings, StringComparison.OrdinalIgnoreCase);

    public static string Normalize(string value) => value.Trim().ToLowerInvariant();

    public static string? AmountError(string section, decimal? amount)
    {
        if (amount is null or >= 0)
            return null;

        return Normalize(section) switch
        {
            Expense => "Expense must be zero or positive. Do not type a minus — it is already an outflow.",
            Income => "Income must be zero or positive.",
            _ => null
        };
    }

    public static string AmountHint(string section) =>
        Normalize(section) switch
        {
            Expense => "Enter a positive amount. No minus — it is already an outflow.",
            Savings => "Positive adds to savings (leaves checking). Negative pulls from savings into checking.",
            _ => "Enter a positive amount. Adds to checking."
        };

    public static readonly string[] SavingsNames = ["Set aside", "Draw"];
}

public static class BudgetSchedules
{
    public const string Monthly = "monthly";
    public const string Weekly = "weekly";
    public const string Biweekly = "biweekly";
    public const string Interval = "interval";

    public static bool IsKnown(string value) =>
        value.Equals(Monthly, StringComparison.OrdinalIgnoreCase)
        || value.Equals(Weekly, StringComparison.OrdinalIgnoreCase)
        || value.Equals(Biweekly, StringComparison.OrdinalIgnoreCase)
        || value.Equals(Interval, StringComparison.OrdinalIgnoreCase);

    public static string Normalize(string value) =>
        string.IsNullOrWhiteSpace(value) ? "" : value.Trim().ToLowerInvariant();

    public static int? StepDays(string scheduleType, int? intervalDays) =>
        Normalize(scheduleType) switch
        {
            Weekly => 7,
            Biweekly => 14,
            Interval => intervalDays,
            _ => null
        };

    public static DateOnly ClampMonthDay(int year, int month, int day) =>
        new(year, month, Math.Min(Math.Max(day, 1), DateTime.DaysInMonth(year, month)));
}
