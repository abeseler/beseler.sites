using BeselerNet.Shared.Contracts.Budget;
using Dapper;
using Npgsql;

namespace BeselerNet.Api.Budget;

internal sealed class BudgetPeriodRow
{
    public int BudgetPeriodId { get; init; }
    public int AccountId { get; init; }
    public short Year { get; init; }
    public short Month { get; init; }
    public decimal? StartingBalance { get; init; }
}

internal sealed class BudgetLineRow
{
    public int BudgetLineId { get; init; }
    public int BudgetPeriodId { get; init; }
    public required string Name { get; init; }
    public required string Section { get; init; }
    public decimal? Amount { get; init; }
    public DateOnly? OnDate { get; init; }
    public bool Committed { get; init; }
    public int? BudgetRecurringTemplateId { get; init; }
}

internal sealed class BudgetTemplateRow
{
    public int BudgetRecurringTemplateId { get; init; }
    public int AccountId { get; init; }
    public required string Name { get; init; }
    public required string Section { get; init; }
    public decimal? Amount { get; init; }
    public required string ScheduleType { get; init; }
    public short? DayOfMonth { get; init; }
    public short? IntervalDays { get; init; }
    public DateOnly? AnchorDate { get; init; }
}

internal sealed class StampDateRow
{
    public int Year { get; init; }
    public int Month { get; init; }
    public DateOnly Date { get; init; }
}

internal sealed class BudgetNameHintRow
{
    public required string Section { get; init; }
    public required string Name { get; init; }
    public DateOnly? OnDate { get; init; }
    public decimal? Amount { get; init; }
}

internal sealed class BudgetDataSource(NpgsqlDataSource dataSource)
{
    private readonly NpgsqlDataSource _dataSource = dataSource;

    public async Task<IReadOnlyList<int>> ListYears(int accountId, CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var years = await connection.QueryAsync<int>(
            "SELECT DISTINCT year FROM budget_period WHERE account_id = @accountId ORDER BY year DESC",
            new { accountId });
        return years.AsList();
    }

    public async Task<bool> YearExists(int accountId, int year, CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM budget_period WHERE account_id = @accountId AND year = @year)",
            new { accountId, year });
    }

    public async Task<IReadOnlyList<BudgetPeriodRow>> PeriodsForYear(int accountId, int year, CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<BudgetPeriodRow>(
            """
            SELECT budget_period_id, account_id, year, month, starting_balance
            FROM budget_period
            WHERE account_id = @accountId AND year = @year
            ORDER BY month
            """,
            new { accountId, year });
        return rows.AsList();
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<BudgetNameHint>>> SuggestedNames(
        int accountId,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<BudgetNameHintRow>(
            """
            SELECT l.section, l.name, l.on_date, l.amount
            FROM budget_line l
            INNER JOIN budget_period p ON p.budget_period_id = l.budget_period_id
            WHERE p.account_id = @accountId
            ORDER BY l.on_date DESC NULLS LAST
            """,
            new { accountId });

        var inMonth = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            if (row.OnDate is not { } date || date.Year != year || date.Month != month)
                continue;
            inMonth.Add($"{BudgetSections.Normalize(row.Section)}\n{row.Name.Trim()}");
        }

        var hints = new Dictionary<string, IReadOnlyList<BudgetNameHint>>(StringComparer.OrdinalIgnoreCase);
        foreach (var section in new[] { BudgetSections.Income, BudgetSections.Expense, BudgetSections.Savings })
        {
            var names = new List<BudgetNameHint>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                if (!string.Equals(row.Section, section, StringComparison.OrdinalIgnoreCase))
                    continue;
                var name = row.Name.Trim();
                if (name.Length == 0 || !seen.Add(name))
                    continue;
                if (inMonth.Contains($"{section}\n{name}"))
                    continue;
                names.Add(new BudgetNameHint
                {
                    Name = name,
                    Amount = row.Amount,
                    Day = row.OnDate?.Day
                });
                if (names.Count == 3)
                    break;
            }

            hints[section] = names;
        }

        return hints;
    }

    public async Task<IReadOnlyList<BudgetLineRow>> LinesForYear(int accountId, int year, CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<BudgetLineRow>(
            """
            SELECT l.budget_line_id, l.budget_period_id, l.name, l.section, l.amount, l.on_date,
                   l.committed, l.budget_recurring_template_id
            FROM budget_line l
            INNER JOIN budget_period p ON p.budget_period_id = l.budget_period_id
            WHERE p.account_id = @accountId AND p.year = @year
            ORDER BY l.on_date NULLS LAST, l.name
            """,
            new { accountId, year });
        return rows.AsList();
    }

    public async Task CreateYear(int accountId, int year, decimal startingBalance, CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        for (var month = 1; month <= 12; month++)
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO budget_period (account_id, year, month, starting_balance)
                VALUES (@accountId, @year, @month, @startingBalance)
                """,
                new { accountId, year, month, startingBalance = month == 1 ? startingBalance : (decimal?)null },
                transaction);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task SetJanuaryStartingBalance(int accountId, int year, decimal startingBalance, CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            UPDATE budget_period
            SET starting_balance = @startingBalance, updated_at = NOW()
            WHERE account_id = @accountId AND year = @year AND month = 1
            """,
            new { accountId, year, startingBalance });
    }

    public async Task DeleteYear(int accountId, int year, CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            "DELETE FROM budget_period WHERE account_id = @accountId AND year = @year",
            new { accountId, year });
    }

    public async Task<BudgetLineRow?> LineWithId(int lineId, CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<BudgetLineRow>(
            """
            SELECT budget_line_id, budget_period_id, name, section, amount, on_date,
                   committed, budget_recurring_template_id
            FROM budget_line
            WHERE budget_line_id = @lineId
            """,
            new { lineId });
    }

    public async Task<BudgetPeriodRow?> PeriodWithId(int periodId, CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<BudgetPeriodRow>(
            """
            SELECT budget_period_id, account_id, year, month, starting_balance
            FROM budget_period
            WHERE budget_period_id = @periodId
            """,
            new { periodId });
    }

    public async Task<BudgetLineRow> InsertLine(
        int periodId,
        string name,
        string section,
        decimal? amount,
        DateOnly? onDate,
        bool committed,
        int? templateId,
        CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<BudgetLineRow>(
            """
            INSERT INTO budget_line (
                budget_period_id, name, section, amount, on_date, committed, budget_recurring_template_id)
            VALUES (
                @periodId, @name, @section, @amount, @onDate, @committed, @templateId)
            RETURNING budget_line_id, budget_period_id, name, section, amount, on_date,
                      committed, budget_recurring_template_id
            """,
            new { periodId, name, section, amount, onDate, committed, templateId });
    }

    public async Task<BudgetLineRow> UpdateLine(
        int lineId,
        string name,
        string section,
        decimal? amount,
        DateOnly? onDate,
        bool committed,
        CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<BudgetLineRow>(
            """
            UPDATE budget_line
            SET name = @name, section = @section, amount = @amount, on_date = @onDate,
                committed = @committed, updated_at = NOW()
            WHERE budget_line_id = @lineId
            RETURNING budget_line_id, budget_period_id, name, section, amount, on_date,
                      committed, budget_recurring_template_id
            """,
            new { lineId, name, section, amount, onDate, committed });
    }

    public async Task DeleteLine(int lineId, CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync("DELETE FROM budget_line WHERE budget_line_id = @lineId", new { lineId });
    }

    public async Task<IReadOnlyList<BudgetTemplateRow>> ListTemplates(int accountId, CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<BudgetTemplateRow>(
            """
            SELECT budget_recurring_template_id, account_id, name, section, amount, schedule_type,
                   day_of_month, interval_days, anchor_date
            FROM budget_recurring_template
            WHERE account_id = @accountId
            ORDER BY name
            """,
            new { accountId });
        return rows.AsList();
    }

    public async Task<BudgetTemplateRow?> TemplateWithId(int templateId, CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<BudgetTemplateRow>(
            """
            SELECT budget_recurring_template_id, account_id, name, section, amount, schedule_type,
                   day_of_month, interval_days, anchor_date
            FROM budget_recurring_template
            WHERE budget_recurring_template_id = @templateId
            """,
            new { templateId });
    }

    public async Task<BudgetTemplateRow> InsertTemplate(
        int accountId,
        string name,
        string section,
        decimal? amount,
        string scheduleType,
        int? dayOfMonth,
        DateOnly? anchorDate,
        int? intervalDays,
        CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<BudgetTemplateRow>(
            """
            INSERT INTO budget_recurring_template (
                account_id, name, section, amount, schedule_type, day_of_month, anchor_date, interval_days)
            VALUES (
                @accountId, @name, @section, @amount, @scheduleType, @dayOfMonth, @anchorDate, @intervalDays)
            RETURNING budget_recurring_template_id, account_id, name, section, amount, schedule_type,
                      day_of_month, interval_days, anchor_date
            """,
            new { accountId, name, section, amount, scheduleType, dayOfMonth, anchorDate, intervalDays });
    }

    public async Task<BudgetTemplateRow> UpdateTemplate(
        int templateId,
        string name,
        string section,
        decimal? amount,
        string scheduleType,
        int? dayOfMonth,
        DateOnly? anchorDate,
        int? intervalDays,
        CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<BudgetTemplateRow>(
            """
            UPDATE budget_recurring_template
            SET name = @name, section = @section, amount = @amount, schedule_type = @scheduleType,
                day_of_month = @dayOfMonth, weekday = NULL, week_of_month = NULL, anchor_date = @anchorDate,
                interval_days = @intervalDays, updated_at = NOW()
            WHERE budget_recurring_template_id = @templateId
            RETURNING budget_recurring_template_id, account_id, name, section, amount, schedule_type,
                      day_of_month, interval_days, anchor_date
            """,
            new { templateId, name, section, amount, scheduleType, dayOfMonth, anchorDate, intervalDays });
    }

    public async Task DeleteUncommittedFrom(int templateId, DateOnly fromDate, CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            DELETE FROM budget_line
            WHERE budget_recurring_template_id = @templateId
              AND committed = false
              AND on_date >= @fromDate
            """,
            new { templateId, fromDate });
    }

    public async Task DeleteStampsFrom(int templateId, DateOnly fromDate, CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            DELETE FROM budget_line
            WHERE budget_recurring_template_id = @templateId
              AND on_date >= @fromDate
            """,
            new { templateId, fromDate });
    }

    public async Task DeleteTemplate(int templateId, CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            "DELETE FROM budget_recurring_template WHERE budget_recurring_template_id = @templateId",
            new { templateId });
    }

    public async Task<IReadOnlySet<(int Year, int Month, DateOnly Date)>> CommittedStampDates(
        int templateId,
        DateOnly fromDate,
        CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<StampDateRow>(
            """
            SELECT p.year, p.month, l.on_date AS date
            FROM budget_line l
            INNER JOIN budget_period p ON p.budget_period_id = l.budget_period_id
            WHERE l.budget_recurring_template_id = @templateId
              AND l.committed = true
              AND l.on_date >= @fromDate
            """,
            new { templateId, fromDate });
        return rows.Select(row => (row.Year, row.Month, row.Date)).ToHashSet();
    }

    public async Task Stamp(
        BudgetTemplateRow template,
        IReadOnlyList<BudgetPeriodRow> periods,
        DateOnly fromDate,
        IReadOnlySet<(int Year, int Month, DateOnly Date)> skip,
        CancellationToken cancellationToken)
    {
        if (periods.Count == 0)
            return;

        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var byMonth = periods.ToDictionary(period => ((int)period.Year, (int)period.Month));

        foreach (var year in periods.Select(period => (int)period.Year).Distinct())
        {
            foreach (var date in BudgetCalculator.Occurrences(template, year))
            {
                if (date < fromDate)
                    continue;
                if (skip.Contains((date.Year, date.Month, date)))
                    continue;
                if (!byMonth.TryGetValue((date.Year, date.Month), out var period))
                    continue;

                await connection.ExecuteAsync(
                    """
                    INSERT INTO budget_line (
                        budget_period_id, name, section, amount, on_date, committed, budget_recurring_template_id)
                    VALUES (
                        @periodId, @name, @section, @amount, @onDate, false, @templateId)
                    """,
                    new
                    {
                        periodId = period.BudgetPeriodId,
                        name = template.Name,
                        section = template.Section,
                        amount = template.Amount,
                        onDate = date,
                        templateId = template.BudgetRecurringTemplateId
                    },
                    transaction);
            }
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task ImportYear(
        int accountId,
        int year,
        decimal startingBalance,
        IReadOnlyList<BudgetImportTemplate> newTemplates,
        IReadOnlyDictionary<string, int> existingKeys,
        IReadOnlyList<BudgetImportLine> lines,
        CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var keys = new Dictionary<string, int>(existingKeys, StringComparer.OrdinalIgnoreCase);

        foreach (var template in newTemplates)
        {
            var created = await connection.QuerySingleAsync<BudgetTemplateRow>(
                """
                INSERT INTO budget_recurring_template (
                    account_id, name, section, amount, schedule_type, day_of_month, anchor_date, interval_days)
                VALUES (
                    @accountId, @name, @section, @amount, @scheduleType, @dayOfMonth, @anchorDate, @intervalDays)
                RETURNING budget_recurring_template_id, account_id, name, section, amount, schedule_type,
                          day_of_month, interval_days, anchor_date
                """,
                new
                {
                    accountId,
                    name = template.Name,
                    section = template.Section,
                    amount = template.Amount,
                    scheduleType = template.ScheduleType,
                    dayOfMonth = template.DayOfMonth,
                    anchorDate = template.AnchorDate,
                    intervalDays = template.IntervalDays
                },
                transaction);
            keys[template.Key] = created.BudgetRecurringTemplateId;
        }

        var exists = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM budget_period WHERE account_id = @accountId AND year = @year)",
            new { accountId, year },
            transaction);

        if (!exists)
        {
            for (var month = 1; month <= 12; month++)
            {
                await connection.ExecuteAsync(
                    """
                    INSERT INTO budget_period (account_id, year, month, starting_balance)
                    VALUES (@accountId, @year, @month, @startingBalance)
                    """,
                    new { accountId, year, month, startingBalance = month == 1 ? startingBalance : (decimal?)null },
                    transaction);
            }
        }
        else
        {
            await connection.ExecuteAsync(
                """
                DELETE FROM budget_line l
                USING budget_period p
                WHERE l.budget_period_id = p.budget_period_id
                  AND p.account_id = @accountId AND p.year = @year
                """,
                new { accountId, year },
                transaction);
            await connection.ExecuteAsync(
                """
                UPDATE budget_period
                SET starting_balance = @startingBalance, updated_at = NOW()
                WHERE account_id = @accountId AND year = @year AND month = 1
                """,
                new { accountId, year, startingBalance },
                transaction);
        }

        var periods = (await connection.QueryAsync<BudgetPeriodRow>(
            """
            SELECT budget_period_id, account_id, year, month, starting_balance
            FROM budget_period
            WHERE account_id = @accountId AND year = @year
            """,
            new { accountId, year },
            transaction)).ToDictionary(period => (int)period.Month);

        foreach (var line in lines)
        {
            int? templateId = null;
            if (!string.IsNullOrWhiteSpace(line.TemplateKey) && keys.TryGetValue(line.TemplateKey, out var id))
                templateId = id;

            await connection.ExecuteAsync(
                """
                INSERT INTO budget_line (
                    budget_period_id, name, section, amount, on_date, committed, budget_recurring_template_id)
                VALUES (
                    @periodId, @name, @section, @amount, @onDate, @committed, @templateId)
                """,
                new
                {
                    periodId = periods[line.OnDate.Month].BudgetPeriodId,
                    name = line.Name,
                    section = line.Section,
                    amount = line.Amount,
                    onDate = line.OnDate,
                    committed = line.Committed,
                    templateId
                },
                transaction);
        }

        await transaction.CommitAsync(cancellationToken);
    }
}

internal sealed record BudgetImportTemplate(
    string Key,
    string Name,
    string Section,
    decimal? Amount,
    string ScheduleType,
    int? DayOfMonth,
    DateOnly? AnchorDate,
    int? IntervalDays);

internal sealed record BudgetImportLine(
    string Name,
    string Section,
    decimal? Amount,
    DateOnly OnDate,
    bool Committed,
    string? TemplateKey);
