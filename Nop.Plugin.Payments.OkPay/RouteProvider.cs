using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Payments.OkPay
{
    public class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            //IPN handler
            routes.MapRoute("Plugin.Payments.OkPay.IPNHandler",
                 "plugins/okpay/ipnhandler",
                 new { controller = "PaymentOkPay", action = "IPNHandler" },
                 new[] { "Nop.Plugin.Payments.OkPay.Controllers" }
            );
            //fail
            routes.MapRoute("Plugin.Payments.OkPay.Fail",
                 "plugins/okpay/fail",
                 new { controller = "PaymentOkPay", action = "Fail" },
                 new[] { "Nop.Plugin.Payments.OkPay.Controllers" }
            );
            //success
            routes.MapRoute("Plugin.Payments.OkPay.Success",
                 "plugins/okpay/success",
                 new { controller = "PaymentOkPay", action = "Success" },
                 new[] { "Nop.Plugin.Payments.OkPay.Controllers" }
            );
        }
        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}