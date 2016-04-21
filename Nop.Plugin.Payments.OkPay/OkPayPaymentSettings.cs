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
        /// <summary>
        /// Gets or sets a value indicating whether to "additional fee" is specified as percentage. true - percentage, false - fixed value.
        /// </summary>
        public bool AdditionalFeePercentage { get; set; }
        /// <summary>
        /// Additional fee
        /// </summary>
        public decimal AdditionalFee { get; set; }
    }
}