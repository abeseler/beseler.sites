namespace BeselerNet.Web;

internal static class Routes
{
    public const string Home = "/";
    public const string Login = "/account/login";
    public const string SignUp = "/account/signup";
    public const string ConfirmEmail = "/account/confirm-email";
    public const string VerifyEmail = "/account/verify-email";
    public const string ForgotPassword = "/account/forgot-password";
    public const string ResetPassword = "/account/reset-password";
    public const string EstablishSession = "/account/session";
    public const string SignOut = "/account/sign-out";
    public const string Dashboard = "/dashboard";
    public const string Budget = "/budget";
    public const string BudgetTemplates = "/budget/templates";

    public static string BudgetYear(int year) => $"/budget/{year}";
    public static string BudgetMonth(int year, int month) => $"/budget/{year}/{month}";
    public const string Roles = "/roles";
    public const string Account = "/account";
    public const string Accounts = "/accounts";
    public const string Invite = "/accounts/invite";
    public const string AccountDetailTemplate = "/accounts/{Id:int}";
    public const string Settings = "/settings";
    public const string AcceptInvite = "/account/accept-invite";

    public static string AccountDetail(int accountId) => $"/accounts/{accountId}";
}
