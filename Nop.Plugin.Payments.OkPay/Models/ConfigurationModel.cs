using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

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
        
        [NopResourceDisplayName("Plugins.Payments.OkPay.Fields.PassBillingInfo")]
        public bool PassBillingInfo { get; set; }
        public bool PassBillingInfo_OverrideForStore { get; set; }

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