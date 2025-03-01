using BeselerNet.Api.Accounts.OAuth;
using BeselerNet.Api.Communications;
using BeselerNet.Api.Core;
using BeselerNet.Shared.Contracts;
using BeselerNet.Shared.Contracts.Users;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Channels;

namespace BeselerNet.Api.Accounts.Users;

internal sealed class ForgotPasswordHandler
{
    public static IResult Handle(ForgotPasswordRequest request)
    {
        if (request.HasValidationErrors(out var errors))
        {
            return TypedResults.ValidationProblem(errors);
        }

        var requestSubmitted = ForgotPasswordService.RequestChannel.Writer.TryWrite(request);
        return requestSubmitted
            ? TypedResults.Accepted((string?)null, new GenericMessageResponse { Message = "If an account with that email exists, a password reset link has been sent." })
            : TypedResults.Problem(Problems.TooManyRequests);
    }
}

internal sealed class ForgotPasswordService(IServiceProvider services, JwtGenerator tokens, ILogger<ForgotPasswordService> logger) : BackgroundService
{
    private readonly IServiceProvider _services = services;
    private readonly JwtGenerator _tokens = tokens;
    private readonly ILogger<ForgotPasswordService> _logger = logger;

    public static readonly Channel<ForgotPasswordRequest> RequestChannel = Channel.CreateBounded<ForgotPasswordRequest>(new BoundedChannelOptions(100)
    {
        FullMode = BoundedChannelFullMode.DropWrite,
        SingleReader = true,
        SingleWriter = false,
        AllowSynchronousContinuations = false
    });

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Forgot password service started.");

        while (await RequestChannel.Reader.WaitToReadAsync(stoppingToken))
        {
            while (RequestChannel.Reader.TryRead(out var request))
            {
                await ProcessRequest(request, stoppingToken);
            }
        }

        _logger.LogInformation("Forgot password service stopped.");
    }

    private async Task ProcessRequest(ForgotPasswordRequest request, CancellationToken stoppingToken)
    {
        using var activity = Telemetry.Source.StartActivity("ForgotPasswordService.ProcessRequest", ActivityKind.Consumer, request.TraceId);

        if (request.Email is null)
        {
            _logger.LogWarning("Password reset request for {Email} failed: email is null", request.Email);
            return;
        }

        _logger.LogInformation("Processing password reset request for {Email}", request.Email);

        try
        {
            using var scope = _services.CreateAsyncScope();
            var accounts = scope.ServiceProvider.GetRequiredService<AccountDataSource>();
            var account = await accounts.WithEmail(request.Email, stoppingToken);
            if (account is null or { IsDisabled: true })
            {
                _logger.LogWarning("Password reset request for {Email} failed: account {Status}", request.Email, account is null ? "not found" : "disabled");
                return;
            }

            activity?.SetTag_AccountId(account.AccountId);
            var emailer = scope.ServiceProvider.GetRequiredService<EmailerProvider>().GetEmailer();
            var subjectClaim = new Claim(JwtRegisteredClaimNames.Sub, account.AccountId.ToString(), ClaimValueTypes.Integer);
            var token = _tokens.Generate(subjectClaim, TimeSpan.FromMinutes(20)).AccessToken;

            var sendResult = account.IsLocked
                ? await emailer.SendAccountLocked(account.AccountId, request.Email, account.Name, stoppingToken)
                : await emailer.SendPasswordReset(account.AccountId, request.Email, account.Name, token, stoppingToken);

            if (sendResult.Failed(out var exception))
            {
                throw exception;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing password reset request for {Email}", request.Email);
        }
    }
}
