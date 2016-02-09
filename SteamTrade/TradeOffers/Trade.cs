using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Newtonsoft.Json;
using SteamKit2;

namespace SteamTrade.TradeOffers
{
    public class Trade
    {
        private readonly TradeOffers _tradeOffers;
        private readonly SteamWeb _steamWeb;
        private readonly SteamID _partnerId;
        public TradeStatus tradeStatus;

        public Trade(TradeOffers tradeOffers, SteamID partnerId, SteamWeb steamWeb)
        {
            _tradeOffers = tradeOffers;
            _partnerId = partnerId;
            _steamWeb = steamWeb;
            tradeStatus = new TradeStatus
            {
                version = 1,
                newversion = true
            };
            tradeStatus.me = new TradeStatusUser(ref tradeStatus);
            tradeStatus.them = new TradeStatusUser(ref tradeStatus);
        }

        /// <summary>
        /// Send the current trade offer with a token.
        /// </summary>
        /// <param name="message">Message to send with trade offer.</param>
        /// <param name="token">Trade offer token.</param>
        /// <exception cref="TradeOfferSteamException">Thrown when Steam gives an unexpected response.</exception>
        /// <returns>ID of the newly created trade offer.</returns>
        public ulong SendTradeWithToken(string message, string token)
        {
            return SendTrade(message, token);
        }
        /// <summary>
        /// Send the current trade offer.
        /// </summary>
        /// <param name="message">Message to send with trade offer.</param>
        /// <param name="token">Optional trade offer token.</param>
        /// <exception cref="TradeOfferSteamException">Thrown when Steam gives an unexpected response.</exception>
        /// <returns>ID of the newly created trade offer.</returns>
        public ulong SendTrade(string message, string token = "")
        {
            const string url = "https://steamcommunity.com/tradeoffer/new/send";
            var referer = "https://steamcommunity.com/tradeoffer/new/?partner=" + _partnerId.AccountID;
            var data = new NameValueCollection
                {
                    {"sessionid", _steamWeb.SessionId},
                    {"serverid", "1"},
                    {"partner", _partnerId.ConvertToUInt64().ToString()},
                    {"tradeoffermessage", message},
                    {"json_tradeoffer", JsonConvert.SerializeObject(tradeStatus)},
                    {
                        "trade_offer_create_params",
                        token == "" ? "{}" : "{\"trade_offer_access_token\":\"" + token + "\"}"
                    }
                };
            dynamic jsonResponse;
            try
            {
                var response = TradeOffers.RetryWebRequest(_steamWeb, url, "POST", data, true, referer);
                jsonResponse = JsonConvert.DeserializeObject<dynamic>(response);
            }
            catch
            {
                throw new TradeOfferSteamException();
            }
            if (jsonResponse.strError != null) throw new TradeOfferSteamException(jsonResponse.strError);
            try
            {
                ulong tradeOfferId = Convert.ToUInt64(jsonResponse.tradeofferid);
                _tradeOffers.AddPendingTradeOfferToList(tradeOfferId);
                return tradeOfferId;
            }
            catch
            {
                throw new TradeOfferSteamException();
            }
        }

        /// <summary>
        /// Add a bot's item to the trade offer.
        /// </summary>
        /// <param name="asset">TradeAsset object</param>
        /// <returns>True if item hasn't been added already, false if it has.</returns>
        public bool AddMyItem(TradeAsset asset)
        {
            return tradeStatus.me.AddItem(asset);
        }
        /// <summary>
        /// Add a bot's item to the trade offer.
        /// </summary>
        /// <param name="appId">App ID of item</param>
        /// <param name="contextId">Context ID of item</param>
        /// <param name="assetId">Asset (unique) ID of item</param>
        /// <param name="amount">Amount to add (default = 1)</param>
        /// <returns>True if item hasn't been added already, false if it has.</returns>
        public bool AddMyItem(int appId, ulong contextId, ulong assetId, int amount = 1)
        {
            var asset = new TradeAsset(appId, contextId, assetId, amount);
            return tradeStatus.me.AddItem(asset);
        }

        /// <summary>
        /// Add a user's item to the trade offer.
        /// </summary>
        /// <param name="asset">TradeAsset object</param>
        /// <returns>True if item hasn't been added already, false if it has.</returns>
        public bool AddOtherItem(TradeAsset asset)
        {
            return tradeStatus.them.AddItem(asset);
        }
        /// <summary>
        /// Add a user's item to the trade offer.
        /// </summary>
        /// <param name="appId">App ID of item</param>
        /// <param name="contextId">Context ID of item</param>
        /// <param name="assetId">Asset (unique) ID of item</param>
        /// <param name="amount">Amount to add (default = 1)</param>
        /// <returns>True if item hasn't been added already, false if it has.</returns>
        public bool AddOtherItem(int appId, ulong contextId, ulong assetId, int amount = 1)
        {
            var asset = new TradeAsset(appId, contextId, assetId, amount);
            return tradeStatus.them.AddItem(asset);
        }

        public class TradeStatus
        {
            public bool newversion { get; set; }
            public int version { get; set; }
            public TradeStatusUser me { get; set; }
            public TradeStatusUser them { get; set; }
            [JsonIgnore]
            public string message { get; set; }
            [JsonIgnore]
            public ulong tradeid { get; set; }
        }

        public class TradeStatusUser
        {
            public List<TradeAsset> assets { get; set; }
            public List<TradeAsset> currency = new List<TradeAsset>();
            public bool ready { get; set; }
            [JsonIgnore]
            public TradeStatus tradeStatus;
            [JsonIgnore]
            public SteamID steamId;

            public TradeStatusUser(ref TradeStatus tradeStatus)
            {
                this.tradeStatus = tradeStatus;
                ready = false;
                assets = new List<TradeAsset>();
            }

            public bool AddItem(TradeAsset asset)
            {
                if (!assets.Contains(asset))
                {
                    tradeStatus.version++;
                    assets.Add(asset);
                    return true;
                }
                return false;
            }
            public bool AddItem(int appId, ulong contextId, ulong assetId, int amount = 1)
            {
                var asset = new TradeAsset(appId, contextId, assetId, amount);
                return AddItem(asset);
            }
        }

        public class TradeAsset
        {
            public readonly int appid;
            public readonly string contextid;
            public readonly int amount;
            public readonly string assetid;

            public TradeAsset(int appId, ulong contextId, ulong itemId, int amount)
            {
                this.appid = appId;
                this.contextid = contextId.ToString();
                this.assetid = itemId.ToString();
                this.amount = amount;
            }

            public TradeAsset(int appId, string contextId, string itemId, int amount)
            {
                this.appid = appId;
                this.contextid = contextId;
                this.assetid = itemId;
                this.amount = amount;
            }

            public override bool Equals(object obj)
            {
                if (obj == null)
                    return false;

                var other = obj as TradeAsset;
                return other != null && Equals(other);
            }

            public bool Equals(TradeAsset other)
            {
                return (this.appid == other.appid) &&
                        (this.contextid == other.contextid) &&
                        (this.amount == other.amount) &&
                        (this.assetid == other.assetid);
            }

            public override int GetHashCode()
            {
                return (Convert.ToUInt64(appid) ^ Convert.ToUInt64(contextid) ^ Convert.ToUInt64(amount) ^ Convert.ToUInt64(assetid)).GetHashCode();
            }
        }
    }
}
