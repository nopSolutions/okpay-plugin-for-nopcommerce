using System;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Payments.OkPay.Controllers
{
    public class PaymentOkPayController : BasePaymentController
    {
        #region Fields

        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly ISettingService _settingService;
        private readonly ILogger _logger;
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly ILocalizationService _localizationService;
        private readonly IWebHelper _webHelper;
        private readonly IPermissionService _permissionService;

        #endregion

        #region Ctor
        public PaymentOkPayController(IWorkContext workContext,
            IStoreContext storeContext,
            ISettingService settingService,
            ILogger logger,
            IPaymentService paymentService,
            IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            ILocalizationService localizationService, 
            IWebHelper webHelper,
            IPermissionService permissionService)
        {
            this._workContext = workContext;
            this._storeContext = storeContext;
            this._settingService = settingService;
            this._logger = logger;
            this._paymentService = paymentService;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
            this._localizationService = localizationService;
            this._webHelper = webHelper;
            this._permissionService = permissionService;
        }

        #endregion

        #region Utilites

        private OkPayPaymentProcessor GetPaymentProcessor()
        {
            var processor =
                _paymentService.LoadPaymentMethodBySystemName("Payments.OkPay") as OkPayPaymentProcessor;
            if (processor == null ||
                !_paymentService.IsPaymentMethodActive(processor) || !processor.PluginDescriptor.Installed)
                throw new NopException("OkPay module cannot be loaded");
            return processor;
        }

        private string GetValue(string key, IFormCollection form)
        {
            return (form.Keys.Contains(key) ? form[key].ToString() : _webHelper.QueryString<string>(key)) ?? string.Empty;
        }

        private string PreparationOrderNote(IFormCollection form)
        {
            var sb = new StringBuilder();
            foreach (var key in form.Keys)
            {
                sb.AppendLine(key + ": " + GetValue(key, form));
            }

            return sb.ToString();
        }

        #endregion

        #region Methods

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
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
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult Configure(ConfigurationModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
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
        
        public IActionResult IPNHandler(IpnModel model)
        {
            var form = model.Form;
            var processor = GetPaymentProcessor();
            if (processor.VerifyIpn(form, out TransactionStatus txnStatus))
            {
                var val = GetValue(Constants.OK_INVOICE_KEY, form);
                if (!string.IsNullOrEmpty(val) && int.TryParse(val, out int orderId))
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

        public IActionResult Fail(IpnModel model)
        {
            var form = model.Form;
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var okPayPaymentSettings = _settingService.LoadSetting<OkPayPaymentSettings>(storeScope);
            if (!okPayPaymentSettings.ReturnFromOkPayWithoutPaymentRedirectsToOrderDetailsPage)
                return RedirectToRoute("HomePage");

            var val = GetValue(Constants.OK_INVOICE_KEY, form);
            if (string.IsNullOrEmpty(val) || !int.TryParse(val, out int orderId))
                return RedirectToRoute("HomePage");

            var order = _orderService.GetOrderById(orderId);
            return order != null ? RedirectToRoute("OrderDetails", new { orderId = order.Id }) : RedirectToRoute("HomePage");
        }

        public IActionResult Success(FormCollection form)
        {
            var val = GetValue(Constants.OK_INVOICE_KEY, form);
            if (string.IsNullOrEmpty(val) || !int.TryParse(val, out int orderId))
                return RedirectToRoute("HomePage");
            var order = _orderService.GetOrderById(orderId);
            if (order == null)
                return RedirectToRoute("HomePage");

            var processor = GetPaymentProcessor();
            if (!processor.VerifyIpn(form, out TransactionStatus txnStatus))
                return RedirectToRoute("HomePage");

            return RedirectToRoute(txnStatus == TransactionStatus.Completed ? "CheckoutCompleted" : "OrderDetails", new { orderId = order.Id });
        }

        #endregion
    }
}