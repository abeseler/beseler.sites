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
            var periodLines = byPeriod[period.BudgetPeriodId].ToArray();
            foreach (var line in periodLines)
            {
                var amount = line.Amount ?? 0;
                if (string.Equals(line.Section, BudgetSections.Income, StringComparison.OrdinalIgnoreCase))
                    income += Math.Abs(amount);
                else if (string.Equals(line.Section, BudgetSections.Expense, StringComparison.OrdinalIgnoreCase))
                    expenses += Math.Abs(amount);
                else if (string.Equals(line.Section, BudgetSections.Savings, StringComparison.OrdinalIgnoreCase))
                    savings += amount;
            }

            var ending = start + income - expenses - savings;
            previousEnding = ending;
            summaries.Add(new BudgetMonthSummary
            {
                Month = period.Month,
                StartingBalance = start,
                Income = income,
                Expenses = expenses,
                Savings = savings,
                CashFlow = income - expenses,
                EndingBalance = ending
            });
        }

        return summaries;
    }

    public readonly record struct YearSections(decimal Income, decimal Expenses, decimal Savings);

    public static (YearSections SoFar, YearSections Ahead) SplitYearTotals(
        IEnumerable<BudgetLineRow> lines,
        DateOnly? today,
        int year)
    {
        decimal incomeSoFar = 0, expensesSoFar = 0, savingsSoFar = 0;
        decimal incomeAhead = 0, expensesAhead = 0, savingsAhead = 0;
        foreach (var line in lines)
        {
            var amount = line.Amount ?? 0;
            decimal income = 0, expenses = 0, savings = 0;
            if (string.Equals(line.Section, BudgetSections.Income, StringComparison.OrdinalIgnoreCase))
                income = Math.Abs(amount);
            else if (string.Equals(line.Section, BudgetSections.Expense, StringComparison.OrdinalIgnoreCase))
                expenses = Math.Abs(amount);
            else if (string.Equals(line.Section, BudgetSections.Savings, StringComparison.OrdinalIgnoreCase))
                savings = amount;

            var ahead = today is { } date
                && (year > date.Year || (year == date.Year && line.OnDate is { } on && on > date));
            if (ahead)
            {
                incomeAhead += income;
                expensesAhead += expenses;
                savingsAhead += savings;
            }
            else
            {
                incomeSoFar += income;
                expensesSoFar += expenses;
                savingsSoFar += savings;
            }
        }

        return (
            new YearSections(incomeSoFar, expensesSoFar, savingsSoFar),
            new YearSections(incomeAhead, expensesAhead, savingsAhead));
    }

    public static IReadOnlyList<BudgetYearRollup> Rollup(
        IReadOnlyList<BudgetPeriodRow> periods,
        IReadOnlyList<BudgetLineRow> lines,
        DateOnly? today)
    {
        var lineByPeriod = lines.ToLookup(line => line.BudgetPeriodId);
        var rollups = new List<BudgetYearRollup>();
        foreach (var group in periods.GroupBy(period => (int)period.Year).OrderByDescending(group => group.Key))
        {
            var yearPeriods = group.OrderBy(period => period.Month).ToArray();
            var yearLines = yearPeriods.SelectMany(period => lineByPeriod[period.BudgetPeriodId]).ToArray();
            var months = Summarize(yearPeriods, yearLines);
            if (months.Count == 0)
                continue;

            var counted = (today is { } date && group.Key == date.Year
                ? months.Where(item => item.Month <= date.Month)
                : months).ToArray();
            if (counted.Length == 0)
                counted = months.ToArray();

            rollups.Add(new BudgetYearRollup
            {
                Year = group.Key,
                StartingBalance = months[0].StartingBalance,
                Income = counted.Sum(item => item.Income),
                Expenses = counted.Sum(item => item.Expenses),
                Savings = counted.Sum(item => item.Savings),
                CashFlow = counted.Sum(item => item.CashFlow),
                EndingBalance = counted[^1].EndingBalance,
                LineCount = yearLines.Length
            });
        }

        return rollups;
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
                Apply(ref balance, line);
            }

            days.Add(new BudgetDayBalance { Day = day, Balance = balance });
        }

        return days;
    }

    public static BudgetTrough? Trough(int year, int month, decimal startingBalance, IEnumerable<BudgetLineRow> lines)
    {
        var days = Days(year, month, startingBalance, lines);
        if (days.Count == 0)
            return null;

        var low = days[0];
        foreach (var day in days)
        {
            if (day.Balance < low.Balance)
                low = day;
        }

        if (low.Balance >= 0)
            return null;

        return new BudgetTrough
        {
            Year = year,
            Month = month,
            Day = low.Day,
            Balance = low.Balance
        };
    }

    public static decimal BalanceOn(DateOnly date, decimal startingBalance, IEnumerable<BudgetLineRow> lines)
    {
        var balance = startingBalance;
        foreach (var line in lines)
        {
            if (line.OnDate is not { } on || on.Year != date.Year || on.Month != date.Month || on.Day > date.Day)
                continue;
            Apply(ref balance, line);
        }

        return balance;
    }

    private static void Apply(ref decimal balance, BudgetLineRow line)
    {
        var amount = line.Amount ?? 0;
        if (string.Equals(line.Section, BudgetSections.Income, StringComparison.OrdinalIgnoreCase))
            balance += Math.Abs(amount);
        else if (string.Equals(line.Section, BudgetSections.Expense, StringComparison.OrdinalIgnoreCase))
            balance -= Math.Abs(amount);
        else
            balance -= amount;
    }
}
