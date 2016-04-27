using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.OkPay.Infrastructure;
using Nop.Plugin.Payments.OkPay.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Payments.OkPay.Controllers
{
    public class PaymentOkPayController : BasePaymentController
    {
        #region Fields
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly PaymentSettings _paymentSettings;
        private readonly ILocalizationService _localizationService;
        private readonly IWebHelper _webHelper;
        #endregion

        #region Ctor
        public PaymentOkPayController(IWorkContext workContext,
            IStoreService storeService,
            ISettingService settingService,
            IPaymentService paymentService,
            IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            ILogger logger,
            PaymentSettings paymentSettings,
            ILocalizationService localizationService, IWebHelper webHelper)
        {
            this._workContext = workContext;
            this._storeService = storeService;
            this._settingService = settingService;
            this._paymentService = paymentService;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
            this._paymentSettings = paymentSettings;
            this._localizationService = localizationService;
            this._webHelper = webHelper;
        }

        #endregion

        #region Utilites

        private OkPayPaymentProcessor GetPaymentProcessor()
        {
            var processor =
                _paymentService.LoadPaymentMethodBySystemName("Payments.OkPay") as OkPayPaymentProcessor;
            if (processor == null ||
                !processor.IsPaymentMethodActive(_paymentSettings) || !processor.PluginDescriptor.Installed)
                throw new NopException("OkPay module cannot be loaded");
            return processor;
        }

        private string GetValue(string key, FormCollection form)
        {
            return (form.AllKeys.Contains(key) ? form[key] : _webHelper.QueryString<string>(key)) ?? String.Empty;
        }

        #endregion

        #region Methods

        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var okPayPaymentSettings = _settingService.LoadSetting<OkPayPaymentSettings>(storeScope);

            var model = new ConfigurationModel
            {
                WalletId = okPayPaymentSettings.WalletId,
                OrderDescription = !string.IsNullOrEmpty(okPayPaymentSettings.OrderDescription) ? okPayPaymentSettings.OrderDescription : Constants.ORDER_DESCRIPTION,
                ActiveStoreScopeConfiguration = storeScope,
                //Currently OkPay does not support a separate parameter discounts and gift cards.
                //Therefore, the code commented out. OkPay developers promise to include support for gift cards in the near future.
                //TODO: Uncomment next time
                //PassProductNamesAndTotals = okPayPaymentSettings.PassProductNamesAndTotals,
                Fees = okPayPaymentSettings.Fees,
                ReturnFromOkPayWithoutPaymentRedirectsToOrderDetailsPage = okPayPaymentSettings.ReturnFromOkPayWithoutPaymentRedirectsToOrderDetailsPage,
            };

            model.AvailableFees.Add(new SelectListItem
            {
                Text = _localizationService.GetResource("Plugins.Payments.OKPAY.Fields.Fees.Item.Merchant", _workContext.WorkingLanguage.Id, defaultValue: "Merchant"),
                Selected = okPayPaymentSettings.Fees == 0,
                Value = "0"
            });

            model.AvailableFees.Add(new SelectListItem
            {
                Text = _localizationService.GetResource("Plugins.Payments.OKPAY.Fields.Fees.Item.Buyer", _workContext.WorkingLanguage.Id, defaultValue: "Buyer"),
                Selected = okPayPaymentSettings.Fees == 1,
                Value = "1"
            });

            if (storeScope > 0)
            {
                model.WalletId_OverrideForStore = _settingService.SettingExists(okPayPaymentSettings, x => x.WalletId, storeScope);
                model.OrderDescription_OverrideForStore = _settingService.SettingExists(okPayPaymentSettings,
                    x => x.OrderDescription, storeScope);
                //Currently OkPay does not support a separate parameter discounts and gift cards.
                //Therefore, the code commented out. OkPay developers promise to include support for gift cards in the near future.
                //TODO: Uncomment next time
                //model.PassProductNamesAndTotals_OverrideForStore = _settingService.SettingExists(okPayPaymentSettings,
                //    x => x.PassProductNamesAndTotals, storeScope);
                model.Fees_OverrideForStore = _settingService.SettingExists(okPayPaymentSettings, x => x.Fees, storeScope);
                model.ReturnFromOkPayWithoutPaymentRedirectsToOrderDetailsPage_OverrideForStore =
                    _settingService.SettingExists(okPayPaymentSettings,
                        x => x.ReturnFromOkPayWithoutPaymentRedirectsToOrderDetailsPage, storeScope);
            }

            return View("~/Plugins/Payments.OkPay/Views/PaymentOkPay/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var okPayPaymentSettings = _settingService.LoadSetting<OkPayPaymentSettings>(storeScope);

            //save settings
            okPayPaymentSettings.WalletId = model.WalletId;
            okPayPaymentSettings.OrderDescription = model.OrderDescription;
            //Currently OkPay does not support a separate parameter discounts and gift cards.
            //Therefore, the code commented out. OkPay developers promise to include support for gift cards in the near future.
            //TODO: Uncomment next time
            //okPayPaymentSettings.PassProductNamesAndTotals = model.PassProductNamesAndTotals;
            okPayPaymentSettings.Fees = model.Fees;
            okPayPaymentSettings.ReturnFromOkPayWithoutPaymentRedirectsToOrderDetailsPage =
                model.ReturnFromOkPayWithoutPaymentRedirectsToOrderDetailsPage;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            if (model.WalletId_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(okPayPaymentSettings, x => x.WalletId, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(okPayPaymentSettings, x => x.WalletId, storeScope);
            if (model.OrderDescription_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(okPayPaymentSettings, x => x.OrderDescription, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(okPayPaymentSettings, x => x.OrderDescription, storeScope);
            //Currently OkPay does not support a separate parameter discounts and gift cards.
            //Therefore, the code commented out. OkPay developers promise to include support for gift cards in the near future.
            //TODO: Uncomment next time
            //if (model.PassProductNamesAndTotals_OverrideForStore || storeScope == 0)
            //    _settingService.SaveSetting(okPayPaymentSettings, x => x.PassProductNamesAndTotals, storeScope, false);
            //else if (storeScope > 0)
            //    _settingService.DeleteSetting(okPayPaymentSettings, x => x.PassProductNamesAndTotals, storeScope);
            if (model.Fees_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(okPayPaymentSettings, x => x.Fees, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(okPayPaymentSettings, x => x.Fees, storeScope);
            if (model.ReturnFromOkPayWithoutPaymentRedirectsToOrderDetailsPage_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(okPayPaymentSettings, x => x.ReturnFromOkPayWithoutPaymentRedirectsToOrderDetailsPage, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(okPayPaymentSettings, x => x.ReturnFromOkPayWithoutPaymentRedirectsToOrderDetailsPage, storeScope);

            //now clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        [ChildActionOnly]
        public ActionResult PaymentInfo()
        {
            return View("~/Plugins/Payments.OkPay/Views/PaymentOkPay/PaymentInfo.cshtml");
        }


        [NonAction]
        public override IList<string> ValidatePaymentForm(FormCollection form)
        {
            return new List<string>();
        }

        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            return new ProcessPaymentRequest();
        }

        [ValidateInput(false)]
        public ActionResult IPNHandler(FormCollection form)
        {
            var processor = GetPaymentProcessor();
            TransactionStatus txnStatus;
            if (processor.VerifyIpn(form, out txnStatus))
            {
                var val = GetValue(Constants.OK_INVOICE_KEY, form);
                var orderId = 0;
                if (!String.IsNullOrEmpty(val) && Int32.TryParse(val, out orderId))
                {
                    var order = _orderService.GetOrderById(orderId);
                    if (_orderProcessingService.CanMarkOrderAsPaid(order) && txnStatus == TransactionStatus.Completed)
                        _orderProcessingService.MarkOrderAsPaid(order);
                    else if ((order.PaymentStatus == PaymentStatus.Paid ||
                              order.PaymentStatus == PaymentStatus.Authorized) &&
                             _orderProcessingService.CanCancelOrder(order) && txnStatus != TransactionStatus.Completed)
                        _orderProcessingService.CancelOrder(order, true);
                }
            }

            return Content("");
        }

        public ActionResult Fail(FormCollection form)
        {
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var okPayPaymentSettings = _settingService.LoadSetting<OkPayPaymentSettings>(storeScope);
            if (okPayPaymentSettings.ReturnFromOkPayWithoutPaymentRedirectsToOrderDetailsPage)
            {
                var val = GetValue(Constants.OK_INVOICE_KEY, form);
                var orderId = 0;
                if (!String.IsNullOrEmpty(val) && Int32.TryParse(val, out orderId))
                {
                    var order = _orderService.GetOrderById(orderId);
                    if (order != null)
                    {
                        return RedirectToRoute("OrderDetails", new { orderId = order.Id });
                    }
                }
            }
            return RedirectToRoute("HomePage");
        }

        public ActionResult Success(FormCollection form)
        {
            var val = GetValue(Constants.OK_INVOICE_KEY, form);
            var orderId = 0;
            if (!String.IsNullOrEmpty(val) && Int32.TryParse(val, out orderId))
            {
                var order = _orderService.GetOrderById(orderId);
                if (order != null)
                {
                    return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
                }
            }
            return RedirectToRoute("HomePage");
        }

        #endregion
    }
}