using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Payments.OkPay
{
    public class RouteProvider : IRouteProvider
    {
        #region Methods

        public void RegisterRoutes(IRouteBuilder routeBuilder)
        {
            //IPN handler
            routeBuilder.MapRoute("Plugin.Payments.OkPay.IPNHandler",
                 "plugins/okpay/ipnhandler",
                 new { controller = "PaymentOkPay", action = "IPNHandler" });
            //fail
            routeBuilder.MapRoute("Plugin.Payments.OkPay.Fail",
                 "plugins/okpay/fail",
                 new { controller = "PaymentOkPay", action = "Fail" });
            //success
            routeBuilder.MapRoute("Plugin.Payments.OkPay.Success",
                 "plugins/okpay/success",
                 new { controller = "PaymentOkPay", action = "Success" });
        }

        #endregion

        #region Properties
        
        public int Priority
        {
            get
            {
                return 0;
            }
        }

        #endregion
    }
}