using BeselerNet.Shared.Contracts.Budget;
using static Microsoft.AspNetCore.Http.StatusCodes;
using static System.Net.Mime.MediaTypeNames;

namespace BeselerNet.Api.Budget;

internal static class BudgetEndpoints
{
    public static void MapBudgetEndpoints(this IEndpointRouteBuilder builder)
    {
        var years = builder.MapGroup("/v1/budget/years")
            .WithTags("Budget")
            .RequireAuthorization();

        years.MapGet("/", BudgetHandlers.ListYears)
            .WithName("ListBudgetYears")
            .Produces<BudgetYearsResponse>(Status200OK, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json);

        years.MapPost("/{year:int}", BudgetHandlers.StartYear)
            .WithName("StartBudgetYear")
            .Accepts<StartBudgetYearRequest>(Application.Json)
            .Produces<BudgetYearResponse>(Status201Created, Application.Json)
            .ProducesValidationProblem(Status400BadRequest, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .ProducesProblem(Status409Conflict, Application.Json);

        years.MapGet("/{year:int}", BudgetHandlers.GetYear)
            .WithName("GetBudgetYear")
            .Produces<BudgetYearResponse>(Status200OK, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .ProducesProblem(Status404NotFound, Application.Json);

        years.MapPut("/{year:int}/starting-balance", BudgetHandlers.SetStartingBalance)
            .WithName("SetBudgetStartingBalance")
            .Accepts<SetStartingBalanceRequest>(Application.Json)
            .Produces<BudgetYearResponse>(Status200OK, Application.Json)
            .ProducesValidationProblem(Status400BadRequest, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .ProducesProblem(Status404NotFound, Application.Json);

        years.MapDelete("/{year:int}", BudgetHandlers.DeleteYear)
            .WithName("DeleteBudgetYear")
            .Produces(Status204NoContent)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status400BadRequest, Application.Json)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .ProducesProblem(Status404NotFound, Application.Json);

        years.MapGet("/{year:int}/months/{month:int}", BudgetHandlers.GetMonth)
            .WithName("GetBudgetMonth")
            .Produces<BudgetMonthResponse>(Status200OK, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .ProducesProblem(Status404NotFound, Application.Json);

        years.MapPost("/{year:int}/months/{month:int}/lines", BudgetHandlers.CreateLine)
            .WithName("CreateBudgetLine")
            .Accepts<UpsertBudgetLineRequest>(Application.Json)
            .Produces<BudgetLineResponse>(Status201Created, Application.Json)
            .ProducesValidationProblem(Status400BadRequest, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .ProducesProblem(Status404NotFound, Application.Json);

        var lines = builder.MapGroup("/v1/budget/lines")
            .WithTags("Budget")
            .RequireAuthorization();

        lines.MapPut("/{lineId:int}", BudgetHandlers.UpdateLine)
            .WithName("UpdateBudgetLine")
            .Accepts<UpsertBudgetLineRequest>(Application.Json)
            .Produces<BudgetLineResponse>(Status200OK, Application.Json)
            .ProducesValidationProblem(Status400BadRequest, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .ProducesProblem(Status404NotFound, Application.Json);

        lines.MapDelete("/{lineId:int}", BudgetHandlers.DeleteLine)
            .WithName("DeleteBudgetLine")
            .Produces(Status204NoContent)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .ProducesProblem(Status404NotFound, Application.Json);

        var templates = builder.MapGroup("/v1/budget/templates")
            .WithTags("Budget")
            .RequireAuthorization();

        templates.MapGet("/", BudgetHandlers.ListTemplates)
            .WithName("ListBudgetTemplates")
            .Produces<IReadOnlyList<BudgetTemplateResponse>>(Status200OK, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json);

        templates.MapPost("/", BudgetHandlers.CreateTemplate)
            .WithName("CreateBudgetTemplate")
            .Accepts<UpsertBudgetTemplateRequest>(Application.Json)
            .Produces<BudgetTemplateResponse>(Status201Created, Application.Json)
            .ProducesValidationProblem(Status400BadRequest, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json);

        templates.MapGet("/{templateId:int}", BudgetHandlers.GetTemplate)
            .WithName("GetBudgetTemplate")
            .Produces<BudgetTemplateResponse>(Status200OK, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .ProducesProblem(Status404NotFound, Application.Json);

        templates.MapPut("/{templateId:int}", BudgetHandlers.UpdateTemplate)
            .WithName("UpdateBudgetTemplate")
            .Accepts<UpsertBudgetTemplateRequest>(Application.Json)
            .Produces<BudgetTemplateResponse>(Status200OK, Application.Json)
            .ProducesValidationProblem(Status400BadRequest, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .ProducesProblem(Status404NotFound, Application.Json);

        templates.MapDelete("/{templateId:int}", BudgetHandlers.DeleteTemplate)
            .WithName("DeleteBudgetTemplate")
            .Produces(Status204NoContent)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .ProducesProblem(Status404NotFound, Application.Json);
    }
}
