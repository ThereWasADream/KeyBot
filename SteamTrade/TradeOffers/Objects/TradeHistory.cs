using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SteamTrade.TradeOffers.Objects
{
    public class TradeHistory
    {
        public List<HistoryItem> ReceivedItems { get; set; }
        public List<HistoryItem> GivenItems { get; set; }

        public TradeHistory()
        {
            ReceivedItems = new List<HistoryItem>();
            GivenItems = new List<HistoryItem>();
        }

        public class HistoryItem
        {
            private Trade.TradeAsset _tradeAsset;
            public Trade.TradeAsset TradeAsset
            {
                get
                {
                    return _tradeAsset ?? (_tradeAsset = new Trade.TradeAsset(AppId, ContextId, Id, Amount));
                }
                set
                {
                    _tradeAsset = value;
                }
            }

            [JsonProperty("id")]
            public ulong Id { get; set; }

            [JsonProperty("contextid")]
            public ulong ContextId { get; set; }

            [JsonProperty("amount")]
            public int Amount { get; set; }

            [JsonProperty("owner")]
            private dynamic _owner { get; set; }
            public ulong OwnerId
            {
                get
                {
                    ulong ownerId = 0;
                    ulong.TryParse(Convert.ToString(_owner), out ownerId);
                    return ownerId;
                }
                set
                {
                    _owner = value.ToString();
                }
            }

            [JsonProperty("appid")]
            public int AppId { get; set; }

            [JsonProperty("classid")]
            public ulong ClassId { get; set; }

            [JsonProperty("instanceid")]
            public ulong InstanceId { get; set; }

            [JsonProperty("is_currency")]
            public bool IsCurrency { get; set; }

            [JsonProperty("icon_url")]
            public string IconUrl { get; set; }

            [JsonProperty("icon_url_large")]
            public string IconUrlLarge { get; set; }

            [JsonProperty("icon_drag_url")]
            public string IconDragUrl { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("market_hash_name")]
            public string MarketHashName { get; set; }

            [JsonProperty("market_name")]
            public string MarketName { get; set; }

            [JsonProperty("name_color")]
            public string NameColor { get; set; }

            [JsonProperty("background_color")]
            public string BackgroundColor { get; set; }

            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("tradable")]
            public bool IsTradable { get; set; }

            [JsonProperty("marketable")]
            public bool IsMarketable { get; set; }

            [JsonProperty("commodity")]
            public bool IsCommodity { get; set; }

            [JsonProperty("market_tradable_restriction")]
            public int MarketTradableRestriction { get; set; }

            [JsonProperty("market_marketable_restriction")]
            public int MarketMarketableRestriction { get; set; }

            [JsonProperty("fraudwarnings")]
            public dynamic FraudWarnings { get; set; }

            [JsonProperty("descriptions")]
            private dynamic _descriptions { get; set; }
            public DescriptionsData[] Descriptions
            {
                get
                {
                    if (!string.IsNullOrEmpty(Convert.ToString(_descriptions)))
                    {
                        try
                        {
                            return JsonConvert.DeserializeObject<DescriptionsData[]>(_descriptions);
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                    return new DescriptionsData[0];
                }
                set
                {
                    _descriptions = JsonConvert.SerializeObject(value);
                }
            }

            [JsonProperty("actions")]
            private dynamic _actions { get; set; }
            public ActionsData[] Actions
            {
                get
                {
                    if (!string.IsNullOrEmpty(Convert.ToString(_actions)))
                    {
                        try
                        {
                            return JsonConvert.DeserializeObject<ActionsData[]>(_actions);
                        }
                        catch
                        {

                        }
                    }
                    return new ActionsData[0];
                }
                set
                {
                    _actions = JsonConvert.SerializeObject(value);
                }
            }

            [JsonProperty("tags")]
            public TagsData[] Tags { get; set; }

            [JsonProperty("app_data")]
            public dynamic AppData { get; set; }

            public class DescriptionsData
            {
                [JsonProperty("type")]
                public string Type { get; set; }

                [JsonProperty("value")]
                public string Value { get; set; }

                [JsonProperty("color")]
                public string Color { get; set; }

                [JsonProperty("app_data")]
                public dynamic AppData { get; set; }
            }

            public class ActionsData
            {
                [JsonProperty("name")]
                public string Name { get; set; }

                [JsonProperty("link")]
                public string Link { get; set; }
            }

            public class TagsData
            {
                [JsonProperty("internal_name")]
                public string InternalName { get; set; }

                [JsonProperty("name")]
                public string Name { get; set; }

                [JsonProperty("category")]
                public string Category { get; set; }

                [JsonProperty("category_name")]
                public string CategoryName { get; set; }

                [JsonProperty("color")]
                public string Color { get; set; }
            }
        }
    }
}
