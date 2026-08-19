using BeselerNet.Shared;
using BeselerNet.Shared.Contracts.Budget;
using Microsoft.IdentityModel.JsonWebTokens;
using Npgsql;
using System.Security.Claims;

namespace BeselerNet.Api.Budget;

internal static class BudgetHandlers
{
    public static async Task<IResult> ListYears(ClaimsPrincipal user, BudgetDataSource budget, CancellationToken cancellationToken)
    {
        if (AccountId(user) is not { } accountId)
            return TypedResults.Unauthorized();
        if (BudgetProblems.Forbid(user, accountId, Actions.Read) is { } denied)
            return denied;

        var years = await budget.ListYears(accountId, cancellationToken);
        return TypedResults.Ok(new BudgetYearsResponse { Years = years });
    }

    public static async Task<IResult> StartYear(
        int year,
        StartBudgetYearRequest request,
        ClaimsPrincipal user,
        BudgetDataSource budget,
        TimeProvider time,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        if (AccountId(user) is not { } accountId)
            return TypedResults.Unauthorized();
        if (BudgetProblems.Forbid(user, accountId, Actions.Update) is { } denied)
            return denied;
        if (request.IsInvalid(out var errors))
            return TypedResults.ValidationProblem(errors);
        if (Today(time, http) is not { } today)
            return TypedResults.Problem(BudgetProblems.UnknownTimeZone);
        if (year < today.Year)
            return TypedResults.Problem(BudgetProblems.PastYear);
        if (await budget.YearExists(accountId, year, cancellationToken))
            return TypedResults.Problem(BudgetProblems.YearExists);

        try
        {
            await budget.CreateYear(accountId, year, request.StartingBalance!.Value, cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return TypedResults.Problem(BudgetProblems.YearExists);
        }

        var periods = await budget.PeriodsForYear(accountId, year, cancellationToken);
        foreach (var template in await budget.ListTemplates(accountId, cancellationToken))
            await budget.Stamp(template, periods, today, skip: new HashSet<(int, int, DateOnly)>(), cancellationToken);

        return TypedResults.Created($"/v1/budget/years/{year}", await LoadYear(budget, accountId, year, cancellationToken));
    }

    public static async Task<IResult> GetYear(int year, ClaimsPrincipal user, BudgetDataSource budget, CancellationToken cancellationToken)
    {
        if (AccountId(user) is not { } accountId)
            return TypedResults.Unauthorized();
        if (BudgetProblems.Forbid(user, accountId, Actions.Read) is { } denied)
            return denied;

        var response = await LoadYear(budget, accountId, year, cancellationToken);
        return response is null ? TypedResults.Problem(BudgetProblems.YearNotFound) : TypedResults.Ok(response);
    }

    public static async Task<IResult> SetStartingBalance(
        int year,
        SetStartingBalanceRequest request,
        ClaimsPrincipal user,
        BudgetDataSource budget,
        CancellationToken cancellationToken)
    {
        if (AccountId(user) is not { } accountId)
            return TypedResults.Unauthorized();
        if (BudgetProblems.Forbid(user, accountId, Actions.Update) is { } denied)
            return denied;
        if (request.IsInvalid(out var errors))
            return TypedResults.ValidationProblem(errors);
        if (!await budget.YearExists(accountId, year, cancellationToken))
            return TypedResults.Problem(BudgetProblems.YearNotFound);

        await budget.SetJanuaryStartingBalance(accountId, year, request.StartingBalance!.Value, cancellationToken);
        return TypedResults.Ok(await LoadYear(budget, accountId, year, cancellationToken));
    }

    public static async Task<IResult> DeleteYear(int year, ClaimsPrincipal user, BudgetDataSource budget, TimeProvider time, HttpContext http, CancellationToken cancellationToken)
    {
        if (AccountId(user) is not { } accountId)
            return TypedResults.Unauthorized();
        if (BudgetProblems.Forbid(user, accountId, Actions.Update) is { } denied)
            return denied;
        if (Today(time, http) is not { } today)
            return TypedResults.Problem(BudgetProblems.UnknownTimeZone);
        if (year < today.Year)
            return TypedResults.Problem(BudgetProblems.PastYear);
        if (!await budget.YearExists(accountId, year, cancellationToken))
            return TypedResults.Problem(BudgetProblems.YearNotFound);

        await budget.DeleteYear(accountId, year, cancellationToken);
        return TypedResults.NoContent();
    }

    public static async Task<IResult> GetMonth(int year, int month, ClaimsPrincipal user, BudgetDataSource budget, CancellationToken cancellationToken)
    {
        if (AccountId(user) is not { } accountId)
            return TypedResults.Unauthorized();
        if (BudgetProblems.Forbid(user, accountId, Actions.Read) is { } denied)
            return denied;
        if (month is < 1 or > 12)
            return TypedResults.Problem(BudgetProblems.MonthNotFound);

        var yearResponse = await LoadYear(budget, accountId, year, cancellationToken);
        if (yearResponse is null)
            return TypedResults.Problem(BudgetProblems.YearNotFound);

        var summary = yearResponse.Months.Single(item => item.Month == month);
        var periods = await budget.PeriodsForYear(accountId, year, cancellationToken);
        var period = periods.Single(item => item.Month == month);
        var monthLines = (await budget.LinesForYear(accountId, year, cancellationToken))
            .Where(line => line.BudgetPeriodId == period.BudgetPeriodId)
            .ToArray();

        return TypedResults.Ok(new BudgetMonthResponse
        {
            Year = year,
            Month = month,
            StartingBalance = summary.StartingBalance,
            Income = summary.Income,
            Expenses = summary.Expenses,
            Savings = summary.Savings,
            CashFlow = summary.CashFlow,
            EndingBalance = summary.EndingBalance,
            Lines = monthLines.Select(MapLine).ToArray(),
            Days = BudgetCalculator.Days(year, month, summary.StartingBalance, monthLines)
        });
    }

    public static async Task<IResult> CreateLine(
        int year,
        int month,
        UpsertBudgetLineRequest request,
        ClaimsPrincipal user,
        BudgetDataSource budget,
        CancellationToken cancellationToken)
    {
        if (AccountId(user) is not { } accountId)
            return TypedResults.Unauthorized();
        if (BudgetProblems.Forbid(user, accountId, Actions.Update) is { } denied)
            return denied;
        if (month is < 1 or > 12)
            return TypedResults.Problem(BudgetProblems.MonthNotFound);
        if (request.IsInvalid(year, month, requireDate: true, out var errors))
            return TypedResults.ValidationProblem(errors);

        var periods = await budget.PeriodsForYear(accountId, year, cancellationToken);
        var period = periods.FirstOrDefault(item => item.Month == month);
        if (period is null)
            return TypedResults.Problem(BudgetProblems.YearNotFound);

        var line = await budget.InsertLine(
            period.BudgetPeriodId,
            request.Name.Trim(),
            BudgetSections.Normalize(request.Section),
            request.Amount,
            request.OnDate,
            committed: true,
            templateId: null,
            cancellationToken);
        return TypedResults.Created($"/v1/budget/lines/{line.BudgetLineId}", MapLine(line));
    }

    public static async Task<IResult> UpdateLine(
        int lineId,
        UpsertBudgetLineRequest request,
        ClaimsPrincipal user,
        BudgetDataSource budget,
        CancellationToken cancellationToken)
    {
        if (AccountId(user) is not { } accountId)
            return TypedResults.Unauthorized();
        if (BudgetProblems.Forbid(user, accountId, Actions.Update) is { } denied)
            return denied;

        var existing = await budget.LineWithId(lineId, cancellationToken);
        if (existing is null)
            return TypedResults.Problem(BudgetProblems.LineNotFound);
        var period = await budget.PeriodWithId(existing.BudgetPeriodId, cancellationToken);
        if (period is null || period.AccountId != accountId)
            return TypedResults.Problem(BudgetProblems.LineNotFound);
        if (request.IsInvalid(period.Year, period.Month, requireDate: true, out var errors))
            return TypedResults.ValidationProblem(errors);

        var line = await budget.UpdateLine(
            lineId,
            request.Name.Trim(),
            BudgetSections.Normalize(request.Section),
            request.Amount,
            request.OnDate,
            request.Committed ?? existing.Committed,
            cancellationToken);
        return TypedResults.Ok(MapLine(line));
    }

    public static async Task<IResult> DeleteLine(int lineId, ClaimsPrincipal user, BudgetDataSource budget, CancellationToken cancellationToken)
    {
        if (AccountId(user) is not { } accountId)
            return TypedResults.Unauthorized();
        if (BudgetProblems.Forbid(user, accountId, Actions.Update) is { } denied)
            return denied;

        var existing = await budget.LineWithId(lineId, cancellationToken);
        if (existing is null)
            return TypedResults.Problem(BudgetProblems.LineNotFound);
        var period = await budget.PeriodWithId(existing.BudgetPeriodId, cancellationToken);
        if (period is null || period.AccountId != accountId)
            return TypedResults.Problem(BudgetProblems.LineNotFound);

        await budget.DeleteLine(lineId, cancellationToken);
        return TypedResults.NoContent();
    }

    public static async Task<IResult> ListTemplates(ClaimsPrincipal user, BudgetDataSource budget, CancellationToken cancellationToken)
    {
        if (AccountId(user) is not { } accountId)
            return TypedResults.Unauthorized();
        if (BudgetProblems.Forbid(user, accountId, Actions.Read) is { } denied)
            return denied;

        var templates = await budget.ListTemplates(accountId, cancellationToken);
        return TypedResults.Ok(templates.Select(MapTemplate).ToArray());
    }

    public static async Task<IResult> GetTemplate(int templateId, ClaimsPrincipal user, BudgetDataSource budget, CancellationToken cancellationToken)
    {
        if (AccountId(user) is not { } accountId)
            return TypedResults.Unauthorized();
        if (BudgetProblems.Forbid(user, accountId, Actions.Read) is { } denied)
            return denied;

        var template = await budget.TemplateWithId(templateId, cancellationToken);
        if (template is null || template.AccountId != accountId)
            return TypedResults.Problem(BudgetProblems.TemplateNotFound);
        return TypedResults.Ok(MapTemplate(template));
    }

    public static async Task<IResult> CreateTemplate(
        UpsertBudgetTemplateRequest request,
        ClaimsPrincipal user,
        BudgetDataSource budget,
        TimeProvider time,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        if (AccountId(user) is not { } accountId)
            return TypedResults.Unauthorized();
        if (BudgetProblems.Forbid(user, accountId, Actions.Update) is { } denied)
            return denied;
        if (request.IsInvalid(out var errors))
            return TypedResults.ValidationProblem(errors);
        if (Today(time, http) is not { } today)
            return TypedResults.Problem(BudgetProblems.UnknownTimeZone);

        var schedule = BudgetSchedules.Normalize(request.ScheduleType);
        var template = await budget.InsertTemplate(
            accountId,
            request.Name.Trim(),
            BudgetSections.Normalize(request.Section),
            request.Amount,
            schedule,
            schedule == BudgetSchedules.Monthly ? request.DayOfMonth : null,
            schedule == BudgetSchedules.Monthly ? null : request.AnchorDate,
            BudgetSchedules.StepDays(schedule, request.IntervalDays),
            cancellationToken);

        await StampFromToday(budget, accountId, template, today, skipCommitted: false, cancellationToken);
        return TypedResults.Created($"/v1/budget/templates/{template.BudgetRecurringTemplateId}", MapTemplate(template));
    }

    public static async Task<IResult> UpdateTemplate(
        int templateId,
        UpsertBudgetTemplateRequest request,
        ClaimsPrincipal user,
        BudgetDataSource budget,
        TimeProvider time,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        if (AccountId(user) is not { } accountId)
            return TypedResults.Unauthorized();
        if (BudgetProblems.Forbid(user, accountId, Actions.Update) is { } denied)
            return denied;
        if (request.IsInvalid(out var errors))
            return TypedResults.ValidationProblem(errors);
        if (Today(time, http) is not { } today)
            return TypedResults.Problem(BudgetProblems.UnknownTimeZone);

        var existing = await budget.TemplateWithId(templateId, cancellationToken);
        if (existing is null || existing.AccountId != accountId)
            return TypedResults.Problem(BudgetProblems.TemplateNotFound);

        var skip = await budget.CommittedStampDates(templateId, today, cancellationToken);
        await budget.DeleteUncommittedFrom(templateId, today, cancellationToken);

        var schedule = BudgetSchedules.Normalize(request.ScheduleType);
        var template = await budget.UpdateTemplate(
            templateId,
            request.Name.Trim(),
            BudgetSections.Normalize(request.Section),
            request.Amount,
            schedule,
            schedule == BudgetSchedules.Monthly ? request.DayOfMonth : null,
            schedule == BudgetSchedules.Monthly ? null : request.AnchorDate,
            BudgetSchedules.StepDays(schedule, request.IntervalDays),
            cancellationToken);

        await StampFromToday(budget, accountId, template, today, skipCommitted: true, cancellationToken, skip);
        return TypedResults.Ok(MapTemplate(template));
    }

    public static async Task<IResult> DeleteTemplate(
        int templateId,
        ClaimsPrincipal user,
        BudgetDataSource budget,
        TimeProvider time,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        if (AccountId(user) is not { } accountId)
            return TypedResults.Unauthorized();
        if (BudgetProblems.Forbid(user, accountId, Actions.Update) is { } denied)
            return denied;
        if (Today(time, http) is not { } today)
            return TypedResults.Problem(BudgetProblems.UnknownTimeZone);

        var existing = await budget.TemplateWithId(templateId, cancellationToken);
        if (existing is null || existing.AccountId != accountId)
            return TypedResults.Problem(BudgetProblems.TemplateNotFound);

        await budget.DeleteStampsFrom(templateId, today, cancellationToken);
        await budget.DeleteTemplate(templateId, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task StampFromToday(
        BudgetDataSource budget,
        int accountId,
        BudgetTemplateRow template,
        DateOnly today,
        bool skipCommitted,
        CancellationToken cancellationToken,
        IReadOnlySet<(int Year, int Month, DateOnly Date)>? skip = null)
    {
        var years = (await budget.ListYears(accountId, cancellationToken)).Where(year => year >= today.Year);
        var periods = new List<BudgetPeriodRow>();
        foreach (var year in years)
            periods.AddRange(await budget.PeriodsForYear(accountId, year, cancellationToken));

        skip ??= skipCommitted
            ? await budget.CommittedStampDates(template.BudgetRecurringTemplateId, today, cancellationToken)
            : new HashSet<(int, int, DateOnly)>();
        await budget.Stamp(template, periods, today, skip, cancellationToken);
    }

    private static async Task<BudgetYearResponse?> LoadYear(BudgetDataSource budget, int accountId, int year, CancellationToken cancellationToken)
    {
        var periods = await budget.PeriodsForYear(accountId, year, cancellationToken);
        if (periods.Count == 0)
            return null;

        var lines = await budget.LinesForYear(accountId, year, cancellationToken);
        var months = BudgetCalculator.Summarize(periods, lines);
        return new BudgetYearResponse
        {
            Year = year,
            StartingBalance = months[0].StartingBalance,
            Months = months
        };
    }

    private static BudgetLineResponse MapLine(BudgetLineRow line) => new()
    {
        LineId = line.BudgetLineId,
        Name = line.Name,
        Section = line.Section,
        Amount = line.Amount,
        OnDate = line.OnDate,
        Committed = line.Committed,
        TemplateId = line.BudgetRecurringTemplateId
    };

    private static BudgetTemplateResponse MapTemplate(BudgetTemplateRow template) => new()
    {
        TemplateId = template.BudgetRecurringTemplateId,
        Name = template.Name,
        Section = template.Section,
        Amount = template.Amount,
        ScheduleType = template.ScheduleType,
        DayOfMonth = template.DayOfMonth,
        IntervalDays = template.IntervalDays,
        AnchorDate = template.AnchorDate
    };

    private static int? AccountId(ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var accountId) ? accountId : null;

    private static DateOnly? Today(TimeProvider time, HttpContext http)
    {
        var zone = http.Request.Headers[BudgetCalculator.TimeZoneHeader].FirstOrDefault();
        return BudgetCalculator.TryToday(time, zone, out var today, out _) ? today : null;
    }
}
