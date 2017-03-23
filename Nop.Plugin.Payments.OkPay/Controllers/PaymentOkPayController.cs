using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
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
        private readonly ILogger _logger;
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly ILocalizationService _localizationService;
        private readonly IWebHelper _webHelper;
        private readonly PaymentSettings _paymentSettings;

        #endregion

        #region Ctor
        public PaymentOkPayController(IWorkContext workContext,
            IStoreService storeService,
            ISettingService settingService,
            ILogger logger,
            IPaymentService paymentService,
            IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            ILocalizationService localizationService, 
            IWebHelper webHelper,
            PaymentSettings paymentSettings)
        {
            this._workContext = workContext;
            this._storeService = storeService;
            this._settingService = settingService;
            this._logger = logger;
            this._paymentService = paymentService;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
            this._localizationService = localizationService;
            this._webHelper = webHelper;
            this._paymentSettings = paymentSettings;
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

        private string PreparationOrderNote(FormCollection form)
        {
            var sb = new StringBuilder();
            foreach (var key in form.AllKeys)
            {
                sb.AppendLine(key + ": " + GetValue(key, form));
            }
            return sb.ToString();
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
                PassBillingInfo = okPayPaymentSettings.PassBillingInfo,
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
                model.OrderDescription_OverrideForStore = _settingService.SettingExists(okPayPaymentSettings, x => x.OrderDescription, storeScope);
                model.PassBillingInfo_OverrideForStore = _settingService.SettingExists(okPayPaymentSettings, x => x.PassBillingInfo, storeScope);
                model.Fees_OverrideForStore = _settingService.SettingExists(okPayPaymentSettings, x => x.Fees, storeScope);
                model.ReturnFromOkPayWithoutPaymentRedirectsToOrderDetailsPage_OverrideForStore = _settingService.SettingExists(okPayPaymentSettings, x => x.ReturnFromOkPayWithoutPaymentRedirectsToOrderDetailsPage, storeScope);
            }

            return View("~/Plugins/Payments.OkPay/Views/Configure.cshtml", model);
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
            okPayPaymentSettings.Fees = model.Fees;
            okPayPaymentSettings.ReturnFromOkPayWithoutPaymentRedirectsToOrderDetailsPage = model.ReturnFromOkPayWithoutPaymentRedirectsToOrderDetailsPage;
            okPayPaymentSettings.PassBillingInfo = model.PassBillingInfo;

            _settingService.SaveSettingOverridablePerStore(okPayPaymentSettings, x => x.WalletId, model.WalletId_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(okPayPaymentSettings, x => x.OrderDescription, model.OrderDescription_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(okPayPaymentSettings, x => x.PassBillingInfo, model.PassBillingInfo_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(okPayPaymentSettings, x => x.Fees, model.Fees_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(okPayPaymentSettings, x => x.ReturnFromOkPayWithoutPaymentRedirectsToOrderDetailsPage, model.ReturnFromOkPayWithoutPaymentRedirectsToOrderDetailsPage_OverrideForStore, storeScope, false);
            
            //now clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        [ChildActionOnly]
        public ActionResult PaymentInfo()
        {
            return View("~/Plugins/Payments.OkPay/Views/PaymentInfo.cshtml");
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
                int orderId;
                if (!String.IsNullOrEmpty(val) && Int32.TryParse(val, out orderId))
                {
                    var order = _orderService.GetOrderById(orderId);

                    if (_orderProcessingService.CanMarkOrderAsPaid(order) && txnStatus == TransactionStatus.Completed)
                        _orderProcessingService.MarkOrderAsPaid(order);
                    else if ((order.PaymentStatus == PaymentStatus.Paid ||
                              order.PaymentStatus == PaymentStatus.Authorized) &&
                             _orderProcessingService.CanCancelOrder(order) && txnStatus != TransactionStatus.Completed)
                        _orderProcessingService.CancelOrder(order, true);

                    var sb = new StringBuilder();
                    sb.AppendLine("OkPay IPN:");
                    sb.Append(PreparationOrderNote(form));
                    sb.AppendLine("New transaction status: " + txnStatus);

                    order.OrderNotes.Add(new OrderNote
                    {
                        Note = sb.ToString(),
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow
                    });
                    _orderService.UpdateOrder(order);
                }
                else
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("OkPay error: failed order identifier");
                    sb.AppendLine("Transaction status: " + txnStatus);
                    sb.Append(PreparationOrderNote(form));
                    _logger.Error("OkPay IPN failed.", new NopException(sb.ToString()));
                }
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine("Transaction status: " + txnStatus);
                sb.Append(PreparationOrderNote(form));
                _logger.Error("OkPay IPN failed.", new NopException(sb.ToString()));
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
                int orderId;
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
            int orderId;
            if (!String.IsNullOrEmpty(val) && Int32.TryParse(val, out orderId))
            {
                var order = _orderService.GetOrderById(orderId);
                if (order != null)
                {
                    var processor = GetPaymentProcessor();
                    TransactionStatus txnStatus;
                    if (processor.VerifyIpn(form, out txnStatus))
                    {
                        if (txnStatus == TransactionStatus.Completed)
                            return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
                        else
                            return RedirectToRoute("OrderDetails", new { orderId = order.Id });
                    }
                }
            }
            return RedirectToRoute("HomePage");
        }

        #endregion
    }
}