using Newtonsoft.Json;

namespace SteamTrade.TradeOffers.Objects
{
    public class GetTradeOffer
    {
        [JsonProperty("response")]
        public GetTradeOfferResponse Response { get; set; }

        public class GetTradeOfferResponse
        {
            [JsonProperty("offer")]
            public TradeOffer Offer { get; set; }

            [JsonProperty("descriptions")]
            public TradeOfferDescriptions[] Descriptions { get; set; }
        }
    }
}
