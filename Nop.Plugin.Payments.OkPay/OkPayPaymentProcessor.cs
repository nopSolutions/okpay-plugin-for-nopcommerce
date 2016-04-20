using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Routing;
using BinaryAnalysis.UnidecodeSharp;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Plugins;
using Nop.Plugin.Payments.OkPay.Controllers;
using Nop.Plugin.Payments.OkPay.Infrastructure;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Tax;
using Nop.Web.Framework;

namespace Nop.Plugin.Payments.OkPay
{
    public class OkPayPaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields
        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        private readonly ICurrencyService _currencyService;
        private readonly ICustomerService _customerService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly ISettingService _settingService;
        private readonly ITaxService _taxService;
        private readonly IWebHelper _webHelper;
        private readonly CurrencySettings _currencySettings;
        private readonly HttpContextBase _httpContext;
        private readonly OkPayPaymentSettings _okPayPaymentSettings;
        #endregion

        #region Ctor
        public OkPayPaymentProcessor(
            ICheckoutAttributeParser checkoutAttributeParser,
            ICurrencyService currencyService,
            ICustomerService customerService,
            IOrderTotalCalculationService orderTotalCalculationService,
            ITaxService taxService,
            ISettingService settingService,
            IWebHelper webHelper,
            CurrencySettings currencySettings,
            HttpContextBase httpContext,
            OkPayPaymentSettings okPayPaymentSettings)
        {
            _checkoutAttributeParser = checkoutAttributeParser;
            _currencyService = currencyService;
            _customerService = customerService;
            _orderTotalCalculationService = orderTotalCalculationService;
            _taxService = taxService;
            _settingService = settingService;
            _webHelper = webHelper;
            _currencySettings = currencySettings;
            _httpContext = httpContext;
            _okPayPaymentSettings = okPayPaymentSettings;
        }
        #endregion

        #region Utilites

        /// <summary>
        /// Get OkPay URL
        /// </summary>
        /// <returns></returns>
        public string GetOkPayUrl()
        {
            return "https://checkout.okpay.com/";
        }

        #endregion

        #region Methods

        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            //var result = new ProcessPaymentResult();

            //var customer = _customerService.GetCustomerById(processPaymentRequest.CustomerId);

            //var webClient = new WebClient();
            return new ProcessPaymentResult
            {
                NewPaymentStatus = PaymentStatus.Pending
            };
        }

        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            var orderTotal = postProcessPaymentRequest.Order.OrderTotal;
            var amount = String.Format(CultureInfo.InvariantCulture, "{0:0.00}", orderTotal);
            var orderId = postProcessPaymentRequest.Order.Id;
            var orderItems = postProcessPaymentRequest.Order.OrderItems.ToList();
            var currency = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId);
            var billingInfo = postProcessPaymentRequest.Order.BillingAddress;
            var enumerator = 1;

            var form = new RemotePost
            {
                FormName = "PayPoint",
                Url = GetOkPayUrl()
            };

            // URL`s
            if (!string.IsNullOrEmpty(_okPayPaymentSettings.IpnUrl))
                form.Add(Constants.OK_IPN_URL_KEY, _okPayPaymentSettings.IpnUrl);
            if (!string.IsNullOrEmpty(_okPayPaymentSettings.SuccessUrl))
                form.Add(Constants.OK_RETURN_SUCCESS_URL_KEY, _okPayPaymentSettings.SuccessUrl);
            if(!string.IsNullOrEmpty(_okPayPaymentSettings.FailUrl))
                form.Add(Constants.OK_RETURN_FAIL_URL_KEY, _okPayPaymentSettings.FailUrl);


            form.Add(Constants.OK_RECEIVER_KEY, _okPayPaymentSettings.WalletId);
            if (_okPayPaymentSettings.PassProductNamesAndTotals)
            {
                decimal cartTotal = decimal.Zero;
                // Send to OkPay order details information, include Product Name, Quantity, Price and SKU
                foreach (var item in orderItems)
                {
                    form.Add(String.Format(Constants.OK_ITEM_NAME_FORMATED_KEY, enumerator), item.Product.Name);
                    form.Add(String.Format(Constants.OK_ITEM_QTY_FORMATED_KEY, enumerator), item.Quantity.ToString());
                    form.Add(String.Format(Constants.OK_ITEM_TYPE_FORMATED_KEY, enumerator),
                        item.Product.IsDownload ? "digital" : "shipment");
                    if (!string.IsNullOrEmpty(item.Product.Sku))
                        form.Add(String.Format(Constants.OK_ITEM_ARTICLE_FORMATED_KEY, enumerator), item.Product.Sku);
                    form.Add(String.Format(Constants.OK_ITEM_PRICE_FORMATED_KEY, enumerator),
                        String.Format(CultureInfo.InvariantCulture, "{0:0.00}", Math.Round(item.UnitPriceExclTax, 2)));
                    cartTotal += item.PriceExclTax;
                    enumerator++;
                }
                // attributes
                var attributeValues =
                    _checkoutAttributeParser.ParseCheckoutAttributeValues(
                        postProcessPaymentRequest.Order.CheckoutAttributesXml);
                foreach (var val in attributeValues)
                {
                    var attPrice = _taxService.GetCheckoutAttributePrice(val, false,
                        postProcessPaymentRequest.Order.Customer);
                    //round
                    var attPriceRounded = Math.Round(attPrice, 2);
                    if (attPrice > decimal.Zero) //if it has a price
                    {
                        var attribute = val.CheckoutAttribute;
                        if (attribute != null)
                        {
                            var attName = attribute.Name; //set the name
                            form.Add(String.Format(Constants.OK_ITEM_NAME_FORMATED_KEY, enumerator), attName);
                            form.Add(String.Format(Constants.OK_ITEM_QTY_FORMATED_KEY, enumerator), "1");
                            form.Add(String.Format(Constants.OK_ITEM_PRICE_FORMATED_KEY, enumerator),
                                attPriceRounded.ToString("0.00", CultureInfo.InvariantCulture));
                            enumerator++;
                            cartTotal += attPrice;
                        }
                    }
                }
                //shipping
                var orderShippingExclTax = postProcessPaymentRequest.Order.OrderShippingExclTax;
                var orderShippingExclTaxRounded = Math.Round(orderShippingExclTax, 2);
                if (orderShippingExclTax > decimal.Zero)
                {
                    form.Add(String.Format(Constants.OK_ITEM_NAME_FORMATED_KEY, enumerator), "Shipping fee");
                    form.Add(String.Format(Constants.OK_ITEM_QTY_FORMATED_KEY, enumerator), "1");
                    form.Add(String.Format(Constants.OK_ITEM_PRICE_FORMATED_KEY, enumerator),
                        orderShippingExclTaxRounded.ToString("0.00", CultureInfo.InvariantCulture));
                    enumerator++;
                    cartTotal += orderShippingExclTax;
                }

                //payment method additional fee
                var paymentMethodAdditionalFeeExclTax =
                    postProcessPaymentRequest.Order.PaymentMethodAdditionalFeeExclTax;
                var paymentMethodAdditionalFeeExclTaxRounded = Math.Round(paymentMethodAdditionalFeeExclTax, 2);
                if (paymentMethodAdditionalFeeExclTax > decimal.Zero)
                {
                    form.Add(String.Format(Constants.OK_ITEM_NAME_FORMATED_KEY, enumerator), "Payment method fee");
                    form.Add(String.Format(Constants.OK_ITEM_QTY_FORMATED_KEY, enumerator), "1");
                    form.Add(String.Format(Constants.OK_ITEM_PRICE_FORMATED_KEY, enumerator),
                        paymentMethodAdditionalFeeExclTaxRounded.ToString("0.00", CultureInfo.InvariantCulture));
                    enumerator++;
                    cartTotal += paymentMethodAdditionalFeeExclTax;
                }

                // tax
                var orderTax = postProcessPaymentRequest.Order.OrderTax;
                var orderTaxRounded = Math.Round(orderTax, 2);
                if (orderTax > decimal.Zero)
                {
                    //add tax as item
                    form.Add(String.Format(Constants.OK_ITEM_NAME_FORMATED_KEY, enumerator), "Sales Tax");
                    form.Add(String.Format(Constants.OK_ITEM_QTY_FORMATED_KEY, enumerator), "1");
                    form.Add(String.Format(Constants.OK_ITEM_PRICE_FORMATED_KEY, enumerator),
                        orderTaxRounded.ToString("0.00", CultureInfo.InvariantCulture));
                    cartTotal += orderTax;
                }

                if (cartTotal > postProcessPaymentRequest.Order.OrderTotal)
                {
                    decimal discountTotal = cartTotal - postProcessPaymentRequest.Order.OrderTotal;
                    discountTotal = Math.Round(discountTotal, 2);
                }
            }
            else
            {
                form.Add(String.Format(Constants.OK_ITEM_NAME_FORMATED_KEY, enumerator), String.Format("Order Number {0}", orderId));
                form.Add(String.Format(Constants.OK_ITEM_QTY_FORMATED_KEY, enumerator), "1");
                form.Add(String.Format(Constants.OK_ITEM_PRICE_FORMATED_KEY, enumerator), amount);
            }
            form.Add(Constants.OK_KIND_KEY, "payment");
            form.Add(Constants.OK_INVOICE_KEY, orderId.ToString());
            form.Add(Constants.OK_CURRENCY_KEY, currency.CurrencyCode);
            //General info
            form.Add(Constants.OK_PAYER_FIRST_NAME_KEY, billingInfo.FirstName.ToTransliterate());
            form.Add(Constants.OK_PAYER_LAST_NAME_KEY, billingInfo.LastName.ToTransliterate());
            if(!string.IsNullOrEmpty(billingInfo.Company))
                form.Add(Constants.OK_PAYER_COMPANY_NAME_KEY, billingInfo.Company.ToTransliterate());
            form.Add(Constants.OK_PAYER_EMAIL_KEY, billingInfo.Email.ToTransliterate());
            form.Add(Constants.OK_PAYER_PHONE_KEY, billingInfo.PhoneNumber.ToTransliterate());
            form.Add(Constants.OK_PAYER_COUNTRY_CODE_KEY, billingInfo.Country.TwoLetterIsoCode.ToTransliterate());
            form.Add(Constants.OK_PAYER_COUNTRY_KEY, billingInfo.Country.Name.ToTransliterate());
            form.Add(Constants.OK_PAYER_CITY_KEY, billingInfo.City.ToTransliterate());
            form.Add(Constants.OK_PAYER_STATE_KEY, billingInfo.StateProvince.Name);
            form.Add(Constants.OK_PAYER_STREET_KEY, billingInfo.Address1);
            form.Add(Constants.OK_PAYER_ZIP_KEY, billingInfo.ZipPostalCode);
            form.Add(Constants.OK_FEES_KEY, _okPayPaymentSettings.Fees.ToString());

            form.Post();
        }

        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            return false;
        }

        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            return 0m;
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

        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
            {
                throw new ArgumentNullException("order");
            }
            return (DateTime.UtcNow - order.CreatedOnUtc).TotalMinutes >= 1.0;
        }

        public Type GetControllerType()
        {
            return typeof(PaymentOkPayController);
        }

        public void GetPaymentInfoRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "PaymentInfo";
            controllerName = "PaymentOkPay";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Payments.OkPay.Controllers" }, { "area", null } };
        }

        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "PaymentOkPay";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Payments.OkPay.Controllers" }, { "area", null } };
        }

        public override void Install()
        {
            var settings = new OkPayPaymentSettings
            {
                WalletId = "OK"
            };
            this._settingService.SaveSetting<OkPayPaymentSettings>(settings);
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.Fields.RedirectionTip", "You will be redirected to OKPAY site to complete the order.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.Fields.WalletId", "Wallet ID");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.Fields.WalletId.Hint", "Specify your OkPay wallet Id.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.Fields.IpnUrl", "IPN Handler");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.Fields.IpnUrl.Hint", "Specify IPN Handler.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.Fields.SuccessUrl", "Success payment url");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.Fields.SuccessUrl.Hint", "User will be redirected this address after payment success");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.Fields.FailUrl", "Fail payment url");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.Fields.FailUrl.Hint", "User will be redirected this address after payment fail");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.Fields.PassProductNamesAndTotals", "Pass product names and order totals to OkPay");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.Fields.PassProductNamesAndTotals.Hint", "Check if product names and order totals should be passed to OkPay");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.Fields.Fees", "Commission payable");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.Fields.Fees.Hint", "Merchant – commission payable by the merchant (default); Buyer – commission payable by the buyer.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.Fields.Fees.Item.Merchant", "Merchant");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.Fields.Fees.Item.Buyer", "Buyer");
            base.Install();
        }
        public override void Uninstall()
        {
            this._settingService.DeleteSetting<OkPayPaymentSettings>();
            this.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.RedirectionTip");
            this.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.WalletId");
            this.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.WalletId.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.IpnUrl");
            this.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.IpnUrl.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.SuccessUrl");
            this.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.SuccessUrl.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.FailUrl");
            this.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.FailUrl.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.PassProductNamesAndTotals");
            this.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.PassProductNamesAndTotals.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.Fees");
            this.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.Fees.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.Fees.Item.Merchant");
            this.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.Fees.Item.Buyer");
            base.Uninstall();
        }

        #endregion

        #region Properties

        public bool SupportCapture { get { return false; } }
        public bool SupportPartiallyRefund { get { return false; } }
        public bool SupportRefund { get { return false; } }
        public bool SupportVoid { get { return false; } }
        public RecurringPaymentType RecurringPaymentType { get { return RecurringPaymentType.NotSupported; } }
        public PaymentMethodType PaymentMethodType { get { return PaymentMethodType.Redirection; } }
        public bool SkipPaymentInfo { get; private set; }

        #endregion
    }
}