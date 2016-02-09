using System;
using Newtonsoft.Json;
using SteamTrade.TradeOffers.Enums;
using SteamKit2;

namespace SteamTrade.TradeOffers.Objects
{
    public class TradeOffer
    {
        [JsonProperty("tradeofferid")]
        public ulong Id { get; set; }

        [JsonProperty("accountid_other")]
        public ulong OtherAccountId { get; set; }

        public ulong OtherSteamId
        {
            get
            {
                return new SteamID(String.Format("STEAM_0:{0}:{1}", OtherAccountId & 1, OtherAccountId >> 1)).ConvertToUInt64();
            }
            set
            {

            }
        }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("expiration_time")]
        public ulong ExpirationTime { get; set; }

        [JsonProperty("trade_offer_state")]
        private int state { get; set; }
        public TradeOfferState State { get { return (TradeOfferState)state; } set { state = (int)value; } }

        [JsonProperty("items_to_give")]
        private CEconAsset[] itemsToGive { get; set; }
        public CEconAsset[] ItemsToGive
        {
            get
            {
                if (itemsToGive == null)
                {
                    return new CEconAsset[0];
                }
                return itemsToGive;
            }
            set
            {
                itemsToGive = value;
            }
        }

        [JsonProperty("items_to_receive")]
        private CEconAsset[] itemsToReceive { get; set; }
        public CEconAsset[] ItemsToReceive
        {
            get
            {
                if (itemsToReceive == null)
                {
                    return new CEconAsset[0];
                }
                return itemsToReceive;
            }
            set
            {
                itemsToReceive = value;
            }
        }

        [JsonProperty("is_our_offer")]
        public bool IsOurOffer { get; set; }

        [JsonProperty("time_created")]
        public int TimeCreated { get; set; }

        [JsonProperty("time_updated")]
        public int TimeUpdated { get; set; }

        [JsonProperty("from_real_time_trade")]
        public bool FromRealTimeTrade { get; set; }

        [JsonProperty("escrow_end_date")]
        public int EscrowEndDate { get; set; }

        [JsonProperty("confirmation_method")]
        private int confirmationMethod { get; set; }
        public TradeOfferConfirmationMethod ConfirmationMethod { get { return (TradeOfferConfirmationMethod)confirmationMethod; } set { confirmationMethod = (int)value; } }

        public class CEconAsset
        {
            [JsonProperty("appid")]
            public int AppId { get; set; }

            [JsonProperty("contextid")]
            public ulong ContextId { get; set; }

            [JsonProperty("assetid")]
            public ulong AssetId { get; set; }

            [JsonProperty("currencyid")]
            public ulong CurrencyId { get; set; }

            [JsonProperty("classid")]
            public ulong ClassId { get; set; }

            [JsonProperty("instanceid")]
            public ulong InstanceId { get; set; }

            [JsonProperty("amount")]
            public int Amount { get; set; }

            [JsonProperty("missing")]
            public bool IsMissing { get; set; }
        }
    }
}
