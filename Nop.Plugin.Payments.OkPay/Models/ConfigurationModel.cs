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
        public bool WalletIdOverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.OkPay.Fields.IpnUrl")]
        public string IpnUrl { get; set; }
        public bool IpnUrlOverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.OkPay.Fields.SuccessUrl")]
        public string SuccessUrl { get; set; }
        public bool SuccessUrlOverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.OkPay.Fields.FailUrl")]
        public string FailUrl { get; set; }
        public bool FailUrlOverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.OkPay.Fields.OrderDescription")]
        public string OrderDescription { get; set; }
        public bool OrderDescriptionOverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.OkPay.Fields.PassProductNamesAndTotals")]
        public bool PassProductNamesAndTotals { get; set; }
        public bool PassProductNamesAndTotalsOverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.OkPay.Fields.Fees")]
        public int Fees { get; set; }
        public bool FeesOverrideForStore { get; set; }

        public IList<SelectListItem> AvailableFees { get; set; }

        public ConfigurationModel()
        {
            AvailableFees = new List<SelectListItem>();
        }
    }
}