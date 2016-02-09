using Newtonsoft.Json;

namespace SteamTrade.TradeOffers.Objects
{
    public class TradeOfferDescriptions
    {
        [JsonProperty("appid")]
        public int AppId { get; set; }

        [JsonProperty("classid")]
        public long ClassId { get; set; }

        [JsonProperty("instanceid")]
        public long InstanceId { get; set; }

        [JsonProperty("currency")]
        public bool IsCurrency { get; set; }

        [JsonProperty("background_color")]
        public string BackgroundColor { get; set; }

        [JsonProperty("icon_url")]
        public string IconUrl { get; set; }

        [JsonProperty("icon_url_large")]
        public string IconUrlLarge { get; set; }

        [JsonProperty("descriptions")]
        public DescriptionsData[] Descriptions { get; set; }

        [JsonProperty("tradable")]
        public bool IsTradable { get; set; }

        [JsonProperty("actions")]
        public ActionsData[] Actions { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_color")]
        public string NameColor { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("market_name")]
        public string MarketName { get; set; }

        [JsonProperty("market_hash_name")]
        public string MarketHashName { get; set; }

        [JsonProperty("market_actions")]
        public MarketActionsData[] MarketActions { get; set; }

        [JsonProperty("commodity")]
        public bool IsCommodity { get; set; }

        public class DescriptionsData
        {
            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("value")]
            public string Value { get; set; }
        }

        public class ActionsData
        {
            [JsonProperty("link")]
            public string Link { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }
        }

        public class MarketActionsData
        {
            [JsonProperty("link")]
            public string Link { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }
        }
    }
}
