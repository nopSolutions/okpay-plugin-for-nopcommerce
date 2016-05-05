namespace Nop.Plugin.Payments.OkPay.Infrastructure
{
    public static class Constants
    {
        internal const string ORDER_DESCRIPTION = "Payment order #$orderId";
        internal const string OK_CHARSET_KEY = "ok_charset";
        internal const string OK_RECEIVER_KEY = "ok_receiver";
        internal const string OK_RECEIVER_ID_KEY = "ok_receiver_id";
        internal const string OK_RECEIVER_WALLET_KEY = "ok_receiver_wallet";
        internal const string OK_RECEIVER_EMAIL_KEY = "ok_receiver_email";
        internal const string OK_TXN_ID_KEY = "ok_txn_id";
        internal const string OK_TXN_KIND_KEY = "ok_txn_kind";
        internal const string OK_TXN_PAYMENT_TYPE = "ok_txn_payment_type";
        internal const string OK_TXN_PAYMENT_METHOD_KEY = "ok_txn_payment_method";
        internal const string OK_TXN_GROSS_KEY = "ok_txn_gross";
        internal const string OK_TXN_AMOUNT_KEY = "ok_txn_amount";
        internal const string OK_TXN_NET_KEY = "ok_txn_net";
        internal const string OK_TXN_FEE_KEY = "ok_txn_fee";
        internal const string OK_TXN_CURRENCY_KEY = "ok_txn_currency";
        internal const string OK_TXN_DATETIME_KEY = "ok_txn_datetime";
        internal const string OK_TXN_STATUS_KEY = "ok_txn_status";
        internal const string OK_INVOICE_KEY = "ok_invoice";
        internal const string OK_PAYER_STATUS_KEY = "ok_payer_status";
        internal const string OK_PAYER_ID_KEY = "ok_payer_id";
        internal const string OK_PAYER_WALLET_KEY = "ok_payer_wallet";
        internal const string OK_PAYER_REPUTATION_KEY = "ok_payer_reputation";
        internal const string OK_PAYER_FIRST_NAME_KEY = "ok_payer_first_name";
        internal const string OK_PAYER_LAST_NAME_KEY = "ok_payer_last_name";
        internal const string OK_PAYER_EMAIL_KEY = "ok_payer_email";
        internal const string OK_PAYER_PHONE_KEY = "ok_payer_phone";
        internal const string OK_PAYER_COUNTRY_KEY = "ok_payer_country";
        internal const string OK_PAYER_CITY_KEY = "ok_payer_city";
        internal const string OK_PAYER_COUNTRY_CODE_KEY = "ok_payer_country_code";
        internal const string OK_PAYER_STATE_KEY = "ok_payer_state";
        internal const string OK_PAYER_ADDRESS_STATUS_KEY = "ok_payer_address_status";
        internal const string OK_PAYER_STREET_KEY = "ok_payer_street";
        internal const string OK_PAYER_ZIP_KEY = "ok_payer_zip";
        internal const string OK_PAYER_ADDRESS_NAME_KEY = "ok_payer_address_name";
        internal const string OK_PAYER_COMPANY_NAME_KEY = "ok_payer_business_name";
        internal const string OK_ITEMS_COUNT_KEY = "ok_items_count";
        internal const string OK_ITEM_NAME_FORMATED_KEY = "ok_item_{0}_name";
        internal const string OK_ITEM_TYPE_FORMATED_KEY = "ok_item_{0}_type";
        internal const string OK_ITEM_QTY_FORMATED_KEY = "ok_item__{0}_quantity";
        internal const string OK_ITEM_GROSS_FORMATED_KEY = "ok_item_{0}_gross";
        internal const string OK_ITEM_PRICE_FORMATED_KEY = "ok_item_{0}_price";
        internal const string OK_ITEM_TAX_FORMATED_KEY = "ok_item_{0}_tax";
        internal const string OK_ITEM_ARTICLE_FORMATED_KEY = "ok_item_{0}_article";
        internal const string OK_ITEM_CUSTOM_TITLE_FORMATED_KEY = "ok_item_{0}_custom_{1}_title";
        internal const string OK_ITEM_CUSTOM_VALUE_FORMATED_KEY = "ok_item_{0}_custom_{1}_value";
        internal const string OK_IPN_ID_KEY = "ok_ipn_id";
        internal const string OK_CURRENCY_KEY = "ok_currency";
        internal const string OK_IPN_URL_KEY = "ok_ipn";
        internal const string OK_RETURN_SUCCESS_URL_KEY = "ok_return_success";
        internal const string OK_RETURN_FAIL_URL_KEY = "ok_return_fail";
        internal const string OK_FEES_KEY = "ok_fees";
        internal const string OK_KIND_KEY = "ok_kind";
        internal const string OK_VERIFY_KEY = "ok_verify";
        internal const string OK_PAYMENT_URL = "https://checkout.okpay.com/";
        internal const string OK_VERIFY_URL = "https://checkout.okpay.com/ipn-verify";
    }
}