namespace BeselerNet.Api.Communications;

internal sealed class EmailerProvider(MailjetEmailService mailjet)
{
    private readonly MailjetEmailService _mailjet = mailjet;
    //private readonly SendGridEmailService _sendGrid = sendGrid;
    //private static readonly Lock @lock = new();
    //private static int _counter = -1;
    public IEmailer GetEmailer() => _mailjet;
    //{
    //    lock (@lock)
    //    {
    //        _counter = _counter == 2 ? 0 : _counter + 1;
    //        if (_counter is 0 or 2)
    //        {
    //            return _mailjet;
    //        }
    //        else
    //        {
    //            return _sendGrid;
    //        }
    //    }
    //}
}
