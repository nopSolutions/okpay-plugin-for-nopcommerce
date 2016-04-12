using System.Collections.Generic;
using System.Web.Mvc;
using Nop.Services.Payments;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Payments.OkPay.Controllers
{
    public class PaymentOkPayController : BasePaymentController
    {
        public override IList<string> ValidatePaymentForm(FormCollection form)
        {
            throw new System.NotImplementedException();
        }

        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            throw new System.NotImplementedException();
        }
    }
}