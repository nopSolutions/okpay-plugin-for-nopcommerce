namespace Nop.Plugin.Payments.OkPay.Infrastructure
{
    public static class Constants
    {
        public const string ORDER_DESCRIPTION = "Order #{0}";
        public const string OK_RECEIVER_KEY = "ok_receiver";
        public const string OK_INVOICE_KEY = "ok_invoice";
        public const string OK_PAYER_FIRST_NAME_KEY = "ok_payer_first_name";
        public const string OK_PAYER_LAST_NAME_KEY = "ok_payer_last_name";
        public const string OK_PAYER_EMAIL_KEY = "ok_payer_email";
        public const string OK_PAYER_PHONE_KEY = "ok_payer_phone";
        public const string OK_PAYER_COUNTRY_KEY = "ok_payer_country";
        public const string OK_PAYER_CITY_KEY = "ok_payer_city";
        public const string OK_PAYER_COUNTRY_CODE_KEY = "ok_payer_country_code";
        public const string OK_PAYER_STATE_KEY = "ok_payer_state";
        public const string OK_PAYER_STREET_KEY = "ok_payer_street";
        public const string OK_PAYER_ZIP_KEY = "ok_payer_zip";
        public const string OK_CURRENCY_KEY = "ok_currency";
        public const string OK_IPN_URL_KEY = "ok_ipn";
        public const string OK_RETURN_SUCCESS_URL_KEY = "ok_return_success";
        public const string OK_RETURN_FAIL_URL_KEY = "ok_return_fail";
        public const string OK_FEES_KEY = "ok_fees";
        public const string OK_KIND_KEY = "ok_kind";
        public const string OK_VERIFY_KEY = "ok_verify";
        public const string OK_PAYMENT_URL = "https://checkout.okpay.com/";
        public const string OK_VERIFY_URL = "https://checkout.okpay.com/ipn-verify";
        public const string OK_TXN_STATUS_KEY = "ok_txn_status";
        public const string OK_PAYER_COMPANY_NAME_KEY = "ok_payer_business_name";
        public const string OK_ITEM_NAME_FORMATED_KEY = "ok_item_{0}_name";
        public const string OK_ITEM_QTY_FORMATED_KEY = "ok_item__{0}_quantity";
        public const string OK_ITEM_PRICE_FORMATED_KEY = "ok_item_{0}_price";
    }
}