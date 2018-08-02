using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Plugins;
using Nop.Plugin.Payments.OkPay.Controllers;
using Nop.Plugin.Payments.OkPay.Infrastructure;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Payments;
using Nop.Web.Framework;

namespace Nop.Plugin.Payments.OkPay
{
    public class OkPayPaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        private readonly ICurrencyService _currencyService;
        private readonly ILocalizationService _localizationService;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        private readonly CurrencySettings _currencySettings;
        private readonly OkPayPaymentSettings _okPayPaymentSettings;
        #endregion

        #region Ctor
        public OkPayPaymentProcessor(
            ICurrencyService currencyService,
            ILocalizationService localizationService,
            ISettingService settingService,
            IWebHelper webHelper,
            CurrencySettings currencySettings,
            OkPayPaymentSettings okPayPaymentSettings)
        {
            _currencyService = currencyService;
            _localizationService = localizationService;
            _settingService = settingService;
            _webHelper = webHelper;
            _currencySettings = currencySettings;
            _okPayPaymentSettings = okPayPaymentSettings;
        }
        #endregion

        #region Utilites

        /// <summary>
        /// Verifies IPN
        /// </summary>
        /// <param name="formCollection">Form string</param>
        /// <param name="status">out Transaction Status</param>
        /// <returns>Result</returns>
        public bool VerifyIpn(IFormCollection formCollection, out TransactionStatus status)
        {
            var isVerified = false;           

            if (formCollection != null)
            {
                var data = new NameValueCollection();
                foreach (var pair in formCollection)
                {
                    data.Add(pair.Key, pair.Value);
                }

                data.Add(Constants.OK_VERIFY_KEY, "true");

                using (var client = new WebClient())
                {
                    var resultBytes = client.UploadValues(Constants.OK_VERIFY_URL, "POST", data);
                    var result = System.Text.Encoding.Default.GetString(resultBytes);
                    // for IPN simulation testing response may be TEST
                    if (result.Equals("VERIFIED", StringComparison.InvariantCultureIgnoreCase) ||
                        result.Equals("TEST", StringComparison.InvariantCultureIgnoreCase))
                    {
                        isVerified = true;
                    }
                }

                status = GetStatus(formCollection[Constants.OK_TXN_STATUS_KEY]);
            }
            else
            {
                status = TransactionStatus.Error;
            }

            return isVerified;
        }

        /// <summary>
        /// Get Transaction status
        /// </summary>
        /// <param name="statusStr">string variable of status from response</param>
        /// <returns>Result</returns>
        private TransactionStatus GetStatus(string statusStr)
        {
            if (string.IsNullOrEmpty(statusStr))
                return TransactionStatus.Error;

            switch (statusStr.ToLower())
            {
                case "completed":
                    return TransactionStatus.Completed;
                case "pending":
                    return TransactionStatus.Pending;
                case "reversed":
                    return TransactionStatus.Reversed;
                case "canceled":
                    return TransactionStatus.Canceled;
                case "hold":
                    return TransactionStatus.Hold;
                default:
                    return TransactionStatus.Error;
            }
        }
        
        #endregion

        #region Methods

        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            return new ProcessPaymentResult
            {
                NewPaymentStatus = PaymentStatus.Pending
            };
        }

        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            var orderTotal = postProcessPaymentRequest.Order.OrderTotal;
            var amount = string.Format(CultureInfo.InvariantCulture, "{0:0.00}", orderTotal);
            var orderId = postProcessPaymentRequest.Order.Id;
            var currency = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId);
            var enumerator = 1;
            var storeUrl = _webHelper.GetStoreLocation();

            var form = new RemotePost
            {
                FormName = "PayPoint",
                Url = Constants.OK_PAYMENT_URL
            };

            //var ipnUrl = "https://www.nopcommerce.com/RecordQueryTest.aspx";
            var ipnUrl = string.Concat(storeUrl, "plugins/okpay/ipnhandler");

            var successUrl = string.Concat(storeUrl, "plugins/okpay/success");
            var failUrl = string.Concat(storeUrl, "plugins/okpay/fail");
            form.Add(Constants.OK_IPN_URL_KEY, ipnUrl);
            form.Add(Constants.OK_RETURN_SUCCESS_URL_KEY, successUrl);
            form.Add(Constants.OK_RETURN_FAIL_URL_KEY, failUrl);

            form.Add(Constants.OK_RECEIVER_KEY, _okPayPaymentSettings.WalletId);

            form.Add(string.Format(Constants.OK_ITEM_NAME_FORMATED_KEY, enumerator), string.Format(_okPayPaymentSettings.OrderDescription, orderId));
            form.Add(string.Format(Constants.OK_ITEM_QTY_FORMATED_KEY, enumerator), "1");
            form.Add(string.Format(Constants.OK_ITEM_PRICE_FORMATED_KEY, enumerator), amount);

            form.Add(Constants.OK_KIND_KEY, "payment");
            form.Add(Constants.OK_INVOICE_KEY, orderId.ToString());
            form.Add(Constants.OK_CURRENCY_KEY, currency.CurrencyCode);
            form.Add(Constants.OK_FEES_KEY, _okPayPaymentSettings.Fees.ToString());

            if (_okPayPaymentSettings.PassBillingInfo)
            {
                var billingInfo = postProcessPaymentRequest.Order.BillingAddress;
                form.Add(Constants.OK_PAYER_FIRST_NAME_KEY, billingInfo.FirstName.ToTransliterate());
                form.Add(Constants.OK_PAYER_LAST_NAME_KEY, billingInfo.LastName.ToTransliterate());
                if (!string.IsNullOrEmpty(billingInfo.Company))
                    form.Add(Constants.OK_PAYER_COMPANY_NAME_KEY, billingInfo.Company.ToTransliterate());
                form.Add(Constants.OK_PAYER_EMAIL_KEY, billingInfo.Email.ToTransliterate());
                form.Add(Constants.OK_PAYER_PHONE_KEY, billingInfo.PhoneNumber.ToTransliterate());
                form.Add(Constants.OK_PAYER_COUNTRY_CODE_KEY, billingInfo.Country.TwoLetterIsoCode.ToTransliterate());
                form.Add(Constants.OK_PAYER_COUNTRY_KEY, billingInfo.Country.Name.ToTransliterate());
                form.Add(Constants.OK_PAYER_CITY_KEY, billingInfo.City.ToTransliterate());
                form.Add(Constants.OK_PAYER_STATE_KEY, billingInfo.StateProvince.Name);
                form.Add(Constants.OK_PAYER_STREET_KEY, billingInfo.Address1);
                form.Add(Constants.OK_PAYER_ZIP_KEY, billingInfo.ZipPostalCode);
            }

            form.Post();
        }

        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            return false;
        }

        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            return 0M;
        }

        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();
            result.AddError("Capture method not supported");
            return result;
        }

        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            var result = new RefundPaymentResult();
            result.AddError("Refund method not supported");
            return result;
        }

        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();
            result.AddError("Void method not supported");
            return result;
        }

        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var result = new CancelRecurringPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order));
            }

            return (DateTime.UtcNow - order.CreatedOnUtc).TotalMinutes >= 1.0;
        }

        public Type GetControllerType()
        {
            return typeof(PaymentOkPayController);
        }

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PaymentOkPay/Configure";
        }

        public IList<string> ValidatePaymentForm(IFormCollection form)
        {
            return new List<string>();
        }

        public ProcessPaymentRequest GetPaymentInfo(IFormCollection form)
        {
            return new ProcessPaymentRequest();
        }

        public override void Install()
        {
            var settings = new OkPayPaymentSettings
            {
                OrderDescription = Constants.ORDER_DESCRIPTION,
                WalletId = "OK"
            };
            _settingService.SaveSetting(settings);
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.OkPay.Fields.OrderDescription", "Order description");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.OkPay.Fields.OrderDescription.Hint", "Order description template.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.Fields.RedirectionTip", "You will be redirected to OKPAY site to complete the order.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.Fields.WalletId", "Wallet ID");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.Fields.WalletId.Hint", "Specify your OkPay wallet Id.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.Fields.PassBillingInfo", "Pass billing info to OkPay");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.Fields.PassBillingInfo.Hint", "Check if billing info should be passed to OkPay.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.Fields.Fees", "Commission payable");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.Fields.Fees.Hint", "Merchant – commission payable by the merchant (default); Buyer – commission payable by the buyer.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.Fields.Fees.Item.Merchant", "Merchant");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.Fields.Fees.Item.Buyer", "Buyer");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.Fields.ReturnFromOkPayWithoutPaymentRedirectsToOrderDetailsPage", "Return to order details page");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.Fields.ReturnFromOkPayWithoutPaymentRedirectsToOrderDetailsPage.Hint", "Enable if a customer should be redirected to the order details page when he clicks \"return to store\" link on OkPay site WITHOUT completing a payment.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.PaymentMethodDescription", "You will be redirected to OKPAY site to complete the order.");

            base.Install();
        }

        public override void Uninstall()
        {
            _settingService.DeleteSetting<OkPayPaymentSettings>();
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.OkPay.Fields.OrderDescription");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.OkPay.Fields.OrderDescription.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.RedirectionTip");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.WalletId");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.WalletId.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.PassBillingInfo");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.PassBillingInfo.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.Fees");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.Fees.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.Fees.Item.Merchant");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.Fees.Item.Buyer");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.ReturnFromOkPayWithoutPaymentRedirectsToOrderDetailsPage");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.ReturnFromOkPayWithoutPaymentRedirectsToOrderDetailsPage.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.OKPAY.PaymentMethodDescription");

            base.Uninstall();
        }

        /// <summary>
        /// Gets a name of a view component for displaying plugin in public store ("payment info" checkout step)
        /// </summary>
        /// <returns>View component name</returns>
        public string GetPublicViewComponentName()
        {
            return "PaymentOkPay";
        }

        #endregion

        #region Properties

        public bool SupportCapture { get { return false; } }
        public bool SupportPartiallyRefund { get { return false; } }
        public bool SupportRefund { get { return false; } }
        public bool SupportVoid { get { return false; } }
        public RecurringPaymentType RecurringPaymentType { get { return RecurringPaymentType.NotSupported; } }
        public PaymentMethodType PaymentMethodType { get { return PaymentMethodType.Redirection; } }
        public bool SkipPaymentInfo { get { return false; } }

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        public string PaymentMethodDescription
        {
            get { return _localizationService.GetResource("Plugins.Payments.OKPAY.PaymentMethodDescription"); }
        }

        #endregion
    }
}