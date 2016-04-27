using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.OkPay
{
    public class OkPayPaymentSettings : ISettings
    {
        public string WalletId { get; set; }
        public string OrderDescription { get; set; }
        //Currently OkPay does not support a separate parameter discounts and gift cards.
        //Therefore, the code commented out. OkPay developers promise to include support for gift cards in the near future.
        //TODO: Uncomment next time
        //public bool PassProductNamesAndTotals { get; set; }
        public int Fees { get; set; }
        public bool ReturnFromOkPayWithoutPaymentRedirectsToOrderDetailsPage { get; set; }
    }
}