using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
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
        private readonly HttpContextBase _httpContext;
        private readonly OkPayPaymentSettings _okPayPaymentSettings;
        #endregion

        #region Ctor
        public OkPayPaymentProcessor(
            ICurrencyService currencyService,
            ISettingService settingService,
            IWebHelper webHelper,
            CurrencySettings currencySettings,
            HttpContextBase httpContext,
            OkPayPaymentSettings okPayPaymentSettings)
        {
            _currencyService = currencyService;
            _settingService = settingService;
            _webHelper = webHelper;
            _currencySettings = currencySettings;
            _httpContext = httpContext;
            _okPayPaymentSettings = okPayPaymentSettings;
        }
        #endregion

        #region Utilites

        public bool VerifyIpn(FormCollection formCollection, out TransactionStatus status)
        {
            var isVerified = false;
            if (formCollection != null)
            {
                var url = String.Concat(Constants.OK_BASE_URL, Constants.OK_VERIFY_URL_NODE);

                formCollection.Add(Constants.OK_VERIFY, "true");

                using (var client = new WebClient())
                {
                    var resultBytes = client.UploadValues(url, "POST", formCollection);
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
                case "error":
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
            var amount = String.Format(CultureInfo.InvariantCulture, "{0:0.00}", orderTotal);
            var orderId = postProcessPaymentRequest.Order.Id;
            var orderItems = postProcessPaymentRequest.Order.OrderItems.ToList();
            var currency = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId);
            var billingInfo = postProcessPaymentRequest.Order.BillingAddress;
            var enumerator = 1;
            var storeUrl = _webHelper.GetStoreLocation(false);

            var form = new RemotePost
            {
                FormName = "PayPoint",
                Url = Constants.OK_BASE_URL
            };

            //TODO: After finish tests, remove this IF DEBUG block
#if DEBUG
            var ipnUrl = "https://www.nopcommerce.com/RecordQueryTest.aspx";
            var successUrl = "https://www.nopcommerce.com/RecordQueryTest.aspx";
            var failUrl = "https://www.nopcommerce.com/RecordQueryTest.aspx";

            form.Add(Constants.OK_IPN_URL_KEY, ipnUrl);
            form.Add(Constants.OK_RETURN_SUCCESS_URL_KEY, successUrl);
            form.Add(Constants.OK_RETURN_FAIL_URL_KEY, failUrl);
#else 
            // URL`s
            form.Add(Constants.OK_IPN_URL_KEY, String.Concat(storeUrl, "plugins/okpay/ipnhandler"));
            form.Add(Constants.OK_RETURN_SUCCESS_URL_KEY, String.Concat(storeUrl, "plugins/okpay/success"));
            form.Add(Constants.OK_RETURN_FAIL_URL_KEY, String.Concat(storeUrl, "plugins/okpay/fail"));
#endif

            form.Add(Constants.OK_RECEIVER_KEY, _okPayPaymentSettings.WalletId);
            /*
            Currently OkPay does not support a separate parameter discounts and gift cards. 
            Therefore, the code commented out. OkPay developers promise to include support for gift cards in the near future.

            TODO: Uncomment next time
            */

            //if (_okPayPaymentSettings.PassProductNamesAndTotals)
            //{
            //    decimal cartTotal = decimal.Zero;
            //    // Send to OkPay order details information, include Product Name, Quantity, Price and SKU
            //    foreach (var item in orderItems)
            //    {
            //        form.Add(String.Format(Constants.OK_ITEM_NAME_FORMATED_KEY, enumerator), item.Product.Name);
            //        form.Add(String.Format(Constants.OK_ITEM_QTY_FORMATED_KEY, enumerator), item.Quantity.ToString());
            //        form.Add(String.Format(Constants.OK_ITEM_TYPE_FORMATED_KEY, enumerator),
            //            item.Product.IsDownload ? "digital" : "shipment");
            //        if (!string.IsNullOrEmpty(item.Product.Sku))
            //            form.Add(String.Format(Constants.OK_ITEM_ARTICLE_FORMATED_KEY, enumerator), item.Product.Sku);
            //        form.Add(String.Format(Constants.OK_ITEM_PRICE_FORMATED_KEY, enumerator),
            //            String.Format(CultureInfo.InvariantCulture, "{0:0.00}", Math.Round(item.UnitPriceExclTax, 2)));
            //        cartTotal += item.PriceExclTax;
            //        enumerator++;
            //    }
            //    // attributes
            //    var attributeValues =
            //        _checkoutAttributeParser.ParseCheckoutAttributeValues(
            //            postProcessPaymentRequest.Order.CheckoutAttributesXml);
            //    foreach (var val in attributeValues)
            //    {
            //        var attPrice = _taxService.GetCheckoutAttributePrice(val, false,
            //            postProcessPaymentRequest.Order.Customer);
            //        //round
            //        var attPriceRounded = Math.Round(attPrice, 2);
            //        if (attPrice > decimal.Zero) //if it has a price
            //        {
            //            var attribute = val.CheckoutAttribute;
            //            if (attribute != null)
            //            {
            //                var attName = attribute.Name; //set the name
            //                form.Add(String.Format(Constants.OK_ITEM_NAME_FORMATED_KEY, enumerator), attName);
            //                form.Add(String.Format(Constants.OK_ITEM_QTY_FORMATED_KEY, enumerator), "1");
            //                form.Add(String.Format(Constants.OK_ITEM_PRICE_FORMATED_KEY, enumerator),
            //                    attPriceRounded.ToString("0.00", CultureInfo.InvariantCulture));
            //                enumerator++;
            //                cartTotal += attPrice;
            //            }
            //        }
            //    }
            //    //shipping
            //    var orderShippingExclTax = postProcessPaymentRequest.Order.OrderShippingExclTax;
            //    var orderShippingExclTaxRounded = Math.Round(orderShippingExclTax, 2);
            //    if (orderShippingExclTax > decimal.Zero)
            //    {
            //        form.Add(String.Format(Constants.OK_ITEM_NAME_FORMATED_KEY, enumerator), "Shipping fee");
            //        form.Add(String.Format(Constants.OK_ITEM_QTY_FORMATED_KEY, enumerator), "1");
            //        form.Add(String.Format(Constants.OK_ITEM_PRICE_FORMATED_KEY, enumerator),
            //            orderShippingExclTaxRounded.ToString("0.00", CultureInfo.InvariantCulture));
            //        enumerator++;
            //        cartTotal += orderShippingExclTax;
            //    }

            //    //payment method additional fee
            //    var paymentMethodAdditionalFeeExclTax =
            //        postProcessPaymentRequest.Order.PaymentMethodAdditionalFeeExclTax;
            //    var paymentMethodAdditionalFeeExclTaxRounded = Math.Round(paymentMethodAdditionalFeeExclTax, 2);
            //    if (paymentMethodAdditionalFeeExclTax > decimal.Zero)
            //    {
            //        form.Add(String.Format(Constants.OK_ITEM_NAME_FORMATED_KEY, enumerator), "Payment method fee");
            //        form.Add(String.Format(Constants.OK_ITEM_QTY_FORMATED_KEY, enumerator), "1");
            //        form.Add(String.Format(Constants.OK_ITEM_PRICE_FORMATED_KEY, enumerator),
            //            paymentMethodAdditionalFeeExclTaxRounded.ToString("0.00", CultureInfo.InvariantCulture));
            //        enumerator++;
            //        cartTotal += paymentMethodAdditionalFeeExclTax;
            //    }

            //    // tax
            //    var orderTax = postProcessPaymentRequest.Order.OrderTax;
            //    var orderTaxRounded = Math.Round(orderTax, 2);
            //    if (orderTax > decimal.Zero)
            //    {
            //        //add tax as item
            //        form.Add(String.Format(Constants.OK_ITEM_NAME_FORMATED_KEY, enumerator), "Sales Tax");
            //        form.Add(String.Format(Constants.OK_ITEM_QTY_FORMATED_KEY, enumerator), "1");
            //        form.Add(String.Format(Constants.OK_ITEM_PRICE_FORMATED_KEY, enumerator),
            //            orderTaxRounded.ToString("0.00", CultureInfo.InvariantCulture));
            //        cartTotal += orderTax;
            //    }

            //    if (cartTotal > postProcessPaymentRequest.Order.OrderTotal)
            //    {
            //        decimal discountTotal = cartTotal - postProcessPaymentRequest.Order.OrderTotal;
            //        discountTotal = Math.Round(discountTotal, 2);
            //    }
            //}
            //else
            //{
            form.Add(String.Format(Constants.OK_ITEM_NAME_FORMATED_KEY, enumerator), String.Format(_localizationService.GetResource("Plugins.Payments.OKPAY.Fields.OrderNumberText"), orderId));
            form.Add(String.Format(Constants.OK_ITEM_QTY_FORMATED_KEY, enumerator), "1");
            form.Add(String.Format(Constants.OK_ITEM_PRICE_FORMATED_KEY, enumerator), amount);
            //}

            form.Add(Constants.OK_KIND_KEY, "payment");
            form.Add(Constants.OK_INVOICE_KEY, orderId.ToString());
            form.Add(Constants.OK_CURRENCY_KEY, currency.CurrencyCode);
            //General info
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
            form.Add(Constants.OK_FEES_KEY, _okPayPaymentSettings.Fees.ToString());

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
            //Currently OkPay does not support a separate parameter discounts and gift cards.
            //Therefore, the code commented out. OkPay developers promise to include support for gift cards in the near future.
            //TODO: Uncomment next time
            //this.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.Fields.PassProductNamesAndTotals", "Pass product names and order totals to OkPay");
            //this.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.Fields.PassProductNamesAndTotals.Hint", "Check if product names and order totals should be passed to OkPay.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.Fields.Fees", "Commission payable");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.Fields.Fees.Hint", "Merchant – commission payable by the merchant (default); Buyer – commission payable by the buyer.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.Fields.Fees.Item.Merchant", "Merchant");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.Fields.Fees.Item.Buyer", "Buyer");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.Fields.OrderNumberText", "Order #{0}");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.Fields.ReturnFromOkPayWithoutPaymentRedirectsToOrderDetailsPage", "Return to order details page");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.OKPAY.Fields.ReturnFromOkPayWithoutPaymentRedirectsToOrderDetailsPage.Hint", "Enable if a customer should be redirected to the order details page when he clicks \"return to store\" link on OkPay site WITHOUT completing a payment.");

            base.Install();
        }
        public override void Uninstall()
        {
            this._settingService.DeleteSetting<OkPayPaymentSettings>();
            this.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.RedirectionTip");
            this.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.WalletId");
            this.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.WalletId.Hint");
            //Currently OkPay does not support a separate parameter discounts and gift cards.
            //Therefore, the code commented out. OkPay developers promise to include support for gift cards in the near future.
            //TODO: Uncomment next time
            //this.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.PassProductNamesAndTotals");
            //this.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.PassProductNamesAndTotals.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.Fees");
            this.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.Fees.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.Fees.Item.Merchant");
            this.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.Fees.Item.Buyer");
            this.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.OrderNumberText");
            this.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.ReturnFromOkPayWithoutPaymentRedirectsToOrderDetailsPage");
            this.DeletePluginLocaleResource("Plugins.Payments.OKPAY.Fields.ReturnFromOkPayWithoutPaymentRedirectsToOrderDetailsPage.Hint");

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