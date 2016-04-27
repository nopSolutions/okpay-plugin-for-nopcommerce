namespace Nop.Plugin.Payments.OkPay.Infrastructure
{
    public enum TransactionStatus
    {
        Completed = 1,
        Pending = 2,
        Reversed = 3,
        Error = 4,
        Canceled = 5,
        Hold = 6
    }
}