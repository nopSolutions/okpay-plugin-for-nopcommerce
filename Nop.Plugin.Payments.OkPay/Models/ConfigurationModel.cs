using System.Collections.Generic;
using System.Web.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Payments.OkPay.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Payments.OkPay.Fields.WalletId")]
        public string WalletId { get; set; }
        public bool WalletId_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.OkPay.Fields.OrderDescription")]
        public string OrderDescription { get; set; }
        public bool OrderDescription_OverrideForStore { get; set; }

        //Currently OkPay does not support a separate parameter discounts and gift cards.
        //Therefore, the code commented out. OkPay developers promise to include support for gift cards in the near future.
        //TODO: Uncomment next time

        //[NopResourceDisplayName("Plugins.Payments.OkPay.Fields.PassProductNamesAndTotals")]
        //public bool PassProductNamesAndTotals { get; set; }
        //public bool PassProductNamesAndTotals_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.OkPay.Fields.Fees")]
        public int Fees { get; set; }
        public bool Fees_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.OkPay.Fields.ReturnFromOkPayWithoutPaymentRedirectsToOrderDetailsPage")]
        public bool ReturnFromOkPayWithoutPaymentRedirectsToOrderDetailsPage { get; set; }
        public bool ReturnFromOkPayWithoutPaymentRedirectsToOrderDetailsPage_OverrideForStore { get; set; }
        public IList<SelectListItem> AvailableFees { get; set; }

        public ConfigurationModel()
        {
            AvailableFees = new List<SelectListItem>();
        }
    }
}