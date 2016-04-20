using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Web.Mvc;
using System.Xml;
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
        private readonly ILogger _logger;
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
            this._logger = logger;
            this._paymentSettings = paymentSettings;
            this._localizationService = localizationService;
            this._webHelper = webHelper;
        }

        #endregion

        #region Utilites
        /// <summary>
        /// Get OkPay Verify URL
        /// </summary>
        /// <returns></returns>
        private string GetOkPayVerifyUrl()
        {
            return "https://checkout.okpay.com/ipn-verify";
        }

        private OkPayPaymentProcessor GetPaymentProcessor()
        {
            var processor =
                _paymentService.LoadPaymentMethodBySystemName("Payments.OkPay") as OkPayPaymentProcessor;
            if (processor == null ||
                !processor.IsPaymentMethodActive(_paymentSettings) || !processor.PluginDescriptor.Installed)
                throw new NopException("OkPay module cannot be loaded");
            return processor;
        }

        private void UpdateSetting<TPropType>(int storeScope, bool overrideForStore, OkPayPaymentSettings settings, Expression<Func<OkPayPaymentSettings, TPropType>> keySelector)
        {
            if (overrideForStore || storeScope == 0)
                _settingService.SaveSetting(settings, keySelector, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(settings, keySelector, storeScope);
        }

        private ContentResult GetResponse(string textToResponse, OkPayPaymentProcessor processor, bool success = false)
        {
            throw new NotImplementedException();
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
                IpnUrl = okPayPaymentSettings.IpnUrl,
                SuccessUrl = okPayPaymentSettings.SuccessUrl,
                FailUrl = okPayPaymentSettings.FailUrl,
                OrderDescription = !string.IsNullOrEmpty(okPayPaymentSettings.OrderDescription) ? okPayPaymentSettings.OrderDescription : Constants.ORDER_DESCRIPTION,
                ActiveStoreScopeConfiguration = storeScope,
                PassProductNamesAndTotals = okPayPaymentSettings.PassProductNamesAndTotals,
                Fees = okPayPaymentSettings.Fees,
            };

            model.AvailableFees.Add(new SelectListItem
            {
                Text = _localizationService.GetResource("Plugins.Payments.OKPAY.Fields.Fees.Item.Merchant", _workContext.WorkingLanguage.Id, defaultValue:"Merchant"),
                Selected = okPayPaymentSettings.Fees==0,
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
                model.WalletIdOverrideForStore = _settingService.SettingExists(okPayPaymentSettings, x => x.WalletId, storeScope);
                model.IpnUrlOverrideForStore = _settingService.SettingExists(okPayPaymentSettings, x => x.IpnUrl, storeScope);
                model.SuccessUrlOverrideForStore = _settingService.SettingExists(okPayPaymentSettings, x => x.SuccessUrl, storeScope);
                model.FailUrlOverrideForStore = _settingService.SettingExists(okPayPaymentSettings, x => x.FailUrl, storeScope);
                model.PassProductNamesAndTotalsOverrideForStore = _settingService.SettingExists(okPayPaymentSettings,
                    x => x.PassProductNamesAndTotals, storeScope);
                model.FeesOverrideForStore = _settingService.SettingExists(okPayPaymentSettings, x => x.Fees, storeScope);
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
            okPayPaymentSettings.IpnUrl = model.IpnUrl;
            okPayPaymentSettings.SuccessUrl = model.SuccessUrl;
            okPayPaymentSettings.FailUrl = model.FailUrl;
            okPayPaymentSettings.OrderDescription = model.OrderDescription;
            okPayPaymentSettings.PassProductNamesAndTotals = model.PassProductNamesAndTotals;
            okPayPaymentSettings.Fees = model.Fees;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            UpdateSetting(storeScope, model.WalletIdOverrideForStore, okPayPaymentSettings, x => x.WalletId);
            UpdateSetting(storeScope, model.IpnUrlOverrideForStore, okPayPaymentSettings, x => x.IpnUrl);
            UpdateSetting(storeScope, model.SuccessUrlOverrideForStore, okPayPaymentSettings, x => x.SuccessUrl);
            UpdateSetting(storeScope, model.FailUrlOverrideForStore, okPayPaymentSettings, x => x.FailUrl);
            UpdateSetting(storeScope, model.OrderDescriptionOverrideForStore, okPayPaymentSettings, x => x.OrderDescription);
            UpdateSetting(storeScope, model.PassProductNamesAndTotalsOverrideForStore, okPayPaymentSettings, x => x.PassProductNamesAndTotals);
            UpdateSetting(storeScope, model.FeesOverrideForStore, okPayPaymentSettings, x => x.Fees);

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

        

        public override IList<string> ValidatePaymentForm(FormCollection form)
        {
            return new List<string>();
        }

        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            return new ProcessPaymentRequest();
        }

        public ActionResult ConfirmPay(FormCollection form)
        {
            var processor = GetPaymentProcessor();
            return null;
        }

        

        #endregion
    }
}