using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.OkPay.Components
{
    [ViewComponent(Name = "PaymentOkPay")]
    public class PaymentOkPayViewComponent : NopViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View("~/Plugins/Payments.OkPay/Views/PaymentInfo.cshtml");
        }
    }
}
