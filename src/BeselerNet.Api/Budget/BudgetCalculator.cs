using BeselerNet.Shared.Contracts.Budget;

namespace BeselerNet.Api.Budget;

internal static class BudgetCalculator
{
    public const string TimeZoneHeader = "X-Time-Zone";

    public static bool TryToday(TimeProvider time, string? timeZone, out DateOnly today, out string? error)
    {
        today = default;
        error = null;
        var utc = time.GetUtcNow().UtcDateTime;
        if (string.IsNullOrWhiteSpace(timeZone))
        {
            today = DateOnly.FromDateTime(utc);
            return true;
        }

        try
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, zone));
            return true;
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            error = "Unknown time zone.";
            return false;
        }
    }

    public static IReadOnlyList<DateOnly> Occurrences(BudgetTemplateRow template, int year)
    {
        if (string.Equals(template.ScheduleType, BudgetSchedules.Monthly, StringComparison.OrdinalIgnoreCase))
        {
            var day = template.DayOfMonth ?? 1;
            return Enumerable.Range(1, 12)
                .Select(month => BudgetSchedules.ClampMonthDay(year, month, day))
                .ToArray();
        }

        if (template.AnchorDate is not { } anchor)
            return [];

        var step = BudgetSchedules.StepDays(template.ScheduleType, template.IntervalDays);
        if (step is null or < 1)
            return [];

        var start = new DateOnly(year, 1, 1);
        var end = new DateOnly(year, 12, 31);
        var date = anchor;
        if (date < start)
        {
            var steps = (start.DayNumber - date.DayNumber + step.Value - 1) / step.Value;
            date = date.AddDays(steps * step.Value);
        }

        var dates = new List<DateOnly>();
        while (date <= end)
        {
            dates.Add(date);
            date = date.AddDays(step.Value);
        }

        return dates;
    }

    public static IReadOnlyList<BudgetMonthSummary> Summarize(
        IReadOnlyList<BudgetPeriodRow> periods,
        IReadOnlyList<BudgetLineRow> lines)
    {
        var byPeriod = lines.ToLookup(line => line.BudgetPeriodId);
        decimal previousEnding = 0;
        var summaries = new List<BudgetMonthSummary>(periods.Count);

        foreach (var period in periods.OrderBy(period => period.Month))
        {
            var start = period.Month == 1 ? period.StartingBalance ?? 0 : previousEnding;
            decimal income = 0, expenses = 0, savings = 0;
            foreach (var line in byPeriod[period.BudgetPeriodId])
            {
                var amount = line.Amount ?? 0;
                if (string.Equals(line.Section, BudgetSections.Income, StringComparison.OrdinalIgnoreCase))
                    income += Math.Abs(amount);
                else if (string.Equals(line.Section, BudgetSections.Expense, StringComparison.OrdinalIgnoreCase))
                    expenses += Math.Abs(amount);
                else if (string.Equals(line.Section, BudgetSections.Savings, StringComparison.OrdinalIgnoreCase))
                    savings += amount;
            }

            var cashFlow = income - expenses;
            var ending = start + income - expenses - savings;
            previousEnding = ending;
            summaries.Add(new BudgetMonthSummary
            {
                Month = period.Month,
                StartingBalance = start,
                Income = income,
                Expenses = expenses,
                Savings = savings,
                CashFlow = cashFlow,
                EndingBalance = ending
            });
        }

        return summaries;
    }

    public static IReadOnlyList<BudgetDayBalance> Days(
        int year,
        int month,
        decimal startingBalance,
        IEnumerable<BudgetLineRow> lines)
    {
        var last = DateTime.DaysInMonth(year, month);
        var days = new List<BudgetDayBalance>(last);
        var monthLines = lines.Where(line => line.OnDate is { } date && date.Year == year && date.Month == month).ToArray();

        for (var day = 1; day <= last; day++)
        {
            var balance = startingBalance;
            foreach (var line in monthLines)
            {
                if (line.OnDate!.Value.Day > day)
                    continue;
                var amount = line.Amount ?? 0;
                if (string.Equals(line.Section, BudgetSections.Income, StringComparison.OrdinalIgnoreCase))
                    balance += Math.Abs(amount);
                else if (string.Equals(line.Section, BudgetSections.Expense, StringComparison.OrdinalIgnoreCase))
                    balance -= Math.Abs(amount);
                else
                    balance -= amount;
            }

            days.Add(new BudgetDayBalance { Day = day, Balance = balance });
        }

        return days;
    }
}
