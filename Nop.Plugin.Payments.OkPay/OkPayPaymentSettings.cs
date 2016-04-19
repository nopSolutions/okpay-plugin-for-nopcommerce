using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.OkPay
{
    public class OkPayPaymentSettings : ISettings
    {
        public string WalletId { get; set; }
        public string IpnUrl { get; set; }
        public string SuccessUrl { get; set; }
        public string FailUrl { get; set; }
        public string OrderDescription { get; set; }
        public bool PassProductNamesAndTotals { get; set; }
        public int Fees { get; set; }
    }
}