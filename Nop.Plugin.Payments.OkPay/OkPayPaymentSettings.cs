using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.OkPay
{
    public class OkPayPaymentSettings : ISettings
    {
        public string WalletId { get; set; }
        public string OrderDescription { get; set; }
        public int Fees { get; set; }
        public bool ReturnFromOkPayWithoutPaymentRedirectsToOrderDetailsPage { get; set; }
        public bool PassBillingInfo { get; set; }
    }
}