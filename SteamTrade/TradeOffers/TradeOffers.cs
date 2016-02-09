using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using SteamKit2;
using System.Net;
using Newtonsoft.Json;
using SteamTrade.TradeOffers.Enums;
using SteamTrade.TradeOffers.Objects;

namespace SteamTrade.TradeOffers
{
    public class TradeOffers
    {
        public List<ulong> OurPendingTradeOffers;

        private readonly object _ourPendingTradeOffersLock;
        private readonly SteamID _botId;
        private readonly SteamWeb _steamWeb;        
        private readonly List<ulong> _handledTradeOffers;
        private readonly List<ulong> _awaitingConfirmationTradeOffers;
        private readonly List<ulong> _inEscrowTradeOffers; 
        private readonly string _accountApiKey;
        private bool _shouldCheckPendingTradeOffers;
        private readonly int _tradeOfferRefreshRate;


        public TradeOffers(SteamID botId, SteamWeb steamWeb, string accountApiKey, int tradeOfferRefreshRate, List<ulong> pendingTradeOffers = null)
        {
            _botId = botId;
            _steamWeb = steamWeb;
            _accountApiKey = accountApiKey;
            _shouldCheckPendingTradeOffers = true;
            _tradeOfferRefreshRate = tradeOfferRefreshRate;

            OurPendingTradeOffers = pendingTradeOffers ?? new List<ulong>();

            _ourPendingTradeOffersLock = new object();
            _handledTradeOffers = new List<ulong>();
            _awaitingConfirmationTradeOffers = new List<ulong>();
            _inEscrowTradeOffers = new List<ulong>();

            new Thread(CheckPendingTradeOffers).Start();
        }

        /// <summary>
        /// Create a new trade offer session.
        /// </summary>
        /// <param name="partnerId">The SteamID of the user you want to send a trade offer to.</param>
        /// <returns>A 'Trade' object in which you can apply further actions</returns>
        public Trade CreateTrade(SteamID partnerId)
        {
            return new Trade(this, partnerId, _steamWeb);
        }

        /// <summary>
        /// Accepts a pending trade offer.
        /// </summary>
        /// <param name="tradeOfferId">The ID of the trade offer</param>
        /// <param name="tradeId">The trade ID of the completed trade</param>
        /// <exception cref="TradeOfferSteamException">Thrown if and only if Steam returns a response with an error message and error code.</exception>
        /// <returns>True if successful, false if not</returns>
        public bool AcceptTrade(ulong tradeOfferId)
        {
            var tradeOfferResponse = GetTradeOffer(tradeOfferId);
            return tradeOfferResponse.Offer != null && AcceptTrade(tradeOfferResponse.Offer);
        }

        /// <summary>
        /// Accepts a pending trade offer
        /// </summary>
        /// <param name="tradeOffer">The trade offer object</param>
        /// <param name="tradeId">The trade ID of the completed trade</param>
        /// <exception cref="TradeOfferSteamException">Thrown if and only if Steam returns a response with an error message and error code.</exception>
        /// <returns>True if successful, false if not</returns>
        public bool AcceptTrade(TradeOffer tradeOffer)
        {
            var tradeOfferId = tradeOffer.Id;
            var url = "https://steamcommunity.com/tradeoffer/" + tradeOfferId + "/accept";
            var referer = "http://steamcommunity.com/tradeoffer/" + tradeOfferId + "/";
            var data = new NameValueCollection
                {
                    {"sessionid", _steamWeb.SessionId},
                    {"serverid", "1"},
                    {"tradeofferid", tradeOfferId.ToString()},
                    {"partner", tradeOffer.OtherSteamId.ToString()}
                };
            var response = RetryWebRequest(_steamWeb, url, "POST", data, true, referer);
            if (string.IsNullOrEmpty(response)) return false;
            dynamic json = JsonConvert.DeserializeObject(response);
            if (json.strError != null) throw new TradeOfferSteamException(json.strError);
            if (json.tradeid == null) return false;
            return true;
        }

        /// <summary>
        /// Declines a pending trade offer
        /// </summary>
        /// <param name="tradeOffer">The trade offer object</param>
        /// <exception cref="TradeOfferSteamException">Thrown if and only if Steam returns a response with an error message and error code.</exception>
        /// <returns>True if successful, false if not</returns>
        public bool DeclineTrade(TradeOffer tradeOffer)
        {
            return DeclineTrade(tradeOffer.Id);
        }
        /// <summary>
        /// Declines a pending trade offer
        /// </summary>
        /// <param name="tradeOfferId">The trade offer ID</param>
        /// <exception cref="TradeOfferSteamException">Thrown if and only if Steam returns a response with an error message and error code.</exception>
        /// <returns>True if successful, false if not</returns>
        public bool DeclineTrade(ulong tradeOfferId)
        {
            var url = "https://steamcommunity.com/tradeoffer/" + tradeOfferId + "/decline";
            const string referer = "http://steamcommunity.com/";
            var data = new NameValueCollection {{"sessionid", _steamWeb.SessionId}, {"serverid", "1"}};
            var response = RetryWebRequest(_steamWeb, url, "POST", data, true, referer);
            dynamic json = JsonConvert.DeserializeObject(response);
            if (json.strError != null) throw new TradeOfferSteamException(json.strError);
            return json.tradeofferid != null;            
        }

        /// <summary>
        /// Cancels a pending sent trade offer
        /// </summary>
        /// <param name="tradeOffer">The trade offer object</param>
        /// <exception cref="TradeOfferSteamException">Thrown if and only if Steam returns a response with an error message and error code.</exception>
        /// <returns>True if successful, false if not</returns>
        public bool CancelTrade(TradeOffer tradeOffer)
        {
            return CancelTrade(tradeOffer.Id);
        }
        /// <summary>
        /// Cancels a pending sent trade offer
        /// </summary>
        /// <param name="tradeOfferId">The trade offer ID</param>
        /// <exception cref="TradeOfferSteamException">Thrown if and only if Steam returns a response with an error message and error code.</exception>
        /// <returns>True if successful, false if not</returns>
        public bool CancelTrade(ulong tradeOfferId)
        {
            var url = "https://steamcommunity.com/tradeoffer/" + tradeOfferId + "/cancel";
            const string referer = "http://steamcommunity.com/";
            var data = new NameValueCollection {{"sessionid", _steamWeb.SessionId}};
            var response = RetryWebRequest(_steamWeb, url, "POST", data, true, referer);
            dynamic json = JsonConvert.DeserializeObject(response);
            if (json.strError != null) throw new TradeOfferSteamException(json.strError);
            return json.tradeofferid != null;
        }

        /// <summary>
        /// Get a list of incoming trade offers.
        /// </summary>
        /// <returns>An 'int' list of trade offer IDs</returns>
        public List<ulong> GetIncomingTradeOffers()
        {
            var incomingTradeOffers = new List<ulong>();
            var url = "http://steamcommunity.com/profiles/" + _botId.ConvertToUInt64() + "/tradeoffers/";
            var html = RetryWebRequest(_steamWeb, url, "GET", null);
            var reg = new Regex("ShowTradeOffer\\((.*?)\\);");
            var matches = reg.Matches(html);
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    var tradeId = Convert.ToUInt64(match.Groups[1].Value.Replace("'", ""));
                    if (!incomingTradeOffers.Contains(tradeId))
                        incomingTradeOffers.Add(tradeId);
                }
            }
            return incomingTradeOffers;
        }

        public GetTradeOffer.GetTradeOfferResponse GetTradeOffer(ulong tradeOfferId)
        {
            var url = string.Format("https://api.steampowered.com/IEconService/GetTradeOffer/v1/?key={0}&tradeofferid={1}&language={2}", _accountApiKey, tradeOfferId, "en_us");
            var response = RetryWebRequest(_steamWeb, url, "GET", null, false, "http://steamcommunity.com");
            var result = JsonConvert.DeserializeObject<GetTradeOffer>(response);
            if (result.Response != null)
            {
                return result.Response;
            }
            return null;
        }

        /// <summary>
        /// Get list of trade offers from API
        /// </summary>
        /// <param name="getActive">Set this to true to get active-only trade offers</param>
        /// <returns>list of trade offers</returns>
        public List<TradeOffer> GetTradeOffers(bool getActive = false)
        {
            var temp = new List<TradeOffer>();
            var url = "https://api.steampowered.com/IEconService/GetTradeOffers/v1/?key=" + _accountApiKey + "&get_sent_offers=1&get_received_offers=1";
            if (getActive)
            {
                url += "&active_only=1&time_historical_cutoff=" + (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            }
            else
            {
                url += "&active_only=0";
            }
            var response = RetryWebRequest(_steamWeb, url, "GET", null, false, "http://steamcommunity.com");
            var json = JsonConvert.DeserializeObject<dynamic>(response);
            var sentTradeOffers = json.response.trade_offers_sent;
            if (sentTradeOffers != null)
            {
                foreach (var tradeOffer in sentTradeOffers)
                {
                    TradeOffer tempTrade = JsonConvert.DeserializeObject<TradeOffer>(Convert.ToString(tradeOffer));
                    temp.Add(tempTrade);
                }
            }
            var receivedTradeOffers = json.response.trade_offers_received;
            if (receivedTradeOffers != null)
            {
                foreach (var tradeOffer in receivedTradeOffers)
                {
                    TradeOffer tempTrade = JsonConvert.DeserializeObject<TradeOffer>(Convert.ToString(tradeOffer));
                    temp.Add(tempTrade);
                }
            }
            return temp;
        }

        /// <summary>
        /// Manually validate if a trade offer went through by checking /inventoryhistory/
        /// You shouldn't use this since it may be exploitable. I'm keeping it here in case I want to rework this in the future.
        /// </summary>
        /// <param name="tradeOffer">A 'TradeOffer' object</param>
        /// <returns>True if the trade offer was successfully accepted, false if otherwise</returns>
//        public bool ValidateTradeAccept(TradeOffer tradeOffer)
//        {
//            try
//            {
//                var history = GetTradeHistory();
//                foreach (var completedTrade in history)
//                {
//                    if (tradeOffer.ItemsToGive.Length == completedTrade.GivenItems.Count && tradeOffer.ItemsToReceive.Length == completedTrade.ReceivedItems.Count)
//                    {
//                        var numFoundGivenItems = 0;
//                        var numFoundReceivedItems = 0;
//                        var foundItemIds = new List<ulong>();
//                        foreach (var historyItem in completedTrade.GivenItems)
//                        {
//                            foreach (var tradeOfferItem in tradeOffer.ItemsToGive)
//                            {
//                                if (tradeOfferItem.ClassId == historyItem.ClassId && tradeOfferItem.InstanceId == historyItem.InstanceId)
//                                {
//                                    if (!foundItemIds.Contains(tradeOfferItem.AssetId))
//                                    {
//                                        foundItemIds.Add(tradeOfferItem.AssetId);
//                                        numFoundGivenItems++;
//                                    }
//                                }
//                            }
//                        }
//                        foreach (var historyItem in completedTrade.ReceivedItems)
//                        {
//                            foreach (var tradeOfferItem in tradeOffer.ItemsToReceive)
//                            {
//                                if (tradeOfferItem.ClassId == historyItem.ClassId && tradeOfferItem.InstanceId == historyItem.InstanceId)
//                                {
//                                    if (!foundItemIds.Contains(tradeOfferItem.AssetId))
//                                    {
//                                        foundItemIds.Add(tradeOfferItem.AssetId);
//                                        numFoundReceivedItems++;
//                                    }
//                                }
//                            }
//                        }
//                        if (numFoundGivenItems == tradeOffer.ItemsToGive.Length && numFoundReceivedItems == tradeOffer.ItemsToReceive.Length)
//                        {
//                            return true;
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine("Error validating trade:");
//                Console.WriteLine(ex);
//            }
//            return false;
//        }

        /// <summary>
        /// Retrieves completed trades from /inventoryhistory/
        /// </summary>
        /// <param name="limit">Max number of trades to retrieve</param>
        /// <param name="numPages">How many pages to retrieve</param>
        /// <returns>A List of 'TradeHistory' objects</returns>
        public List<TradeHistory> GetTradeHistory(int limit = 0, int numPages = 1)
        {
            var tradeHistoryPages = new Dictionary<int, TradeHistory[]>();
            for (var i = 0; i < numPages; i++)
            {
                var tradeHistoryPageList = new TradeHistory[30];
                try
                {
                    var url = "http://steamcommunity.com/profiles/" + _botId.ConvertToUInt64() + "/inventoryhistory/?p=" + i;
                    var html = RetryWebRequest(_steamWeb, url, "GET", null);
                    // TODO: handle rgHistoryCurrency as well
                    var reg = new Regex("rgHistoryInventory = (.*?)};");
                    var m = reg.Match(html);
                    if (m.Success)
                    {
                        var json = m.Groups[1].Value + "}";
                        var schemaResult = JsonConvert.DeserializeObject<Dictionary<int, Dictionary<ulong, Dictionary<ulong, TradeHistory.HistoryItem>>>>(json);
                        var trades = new Regex("HistoryPageCreateItemHover\\((.*?)\\);");
                        var tradeMatches = trades.Matches(html);
                        foreach (Match match in tradeMatches)
                        {
                            if (!match.Success) continue;
                            var historyString = match.Groups[1].Value.Replace("'", "").Replace(" ", "");
                            var split = historyString.Split(',');
                            var tradeString = split[0];
                            var tradeStringSplit = tradeString.Split('_');
                            var tradeNum = Convert.ToInt32(tradeStringSplit[0].Replace("trade", ""));
                            if (limit > 0 && tradeNum >= limit) break;
                            if (tradeHistoryPageList[tradeNum] == null)
                            {
                                tradeHistoryPageList[tradeNum] = new TradeHistory();
                            }
                            var tradeHistoryItem = tradeHistoryPageList[tradeNum];
                            var appId = Convert.ToInt32(split[1]);
                            var contextId = Convert.ToUInt64(split[2]);
                            var itemId = Convert.ToUInt64(split[3]);
                            var amount = Convert.ToInt32(split[4]);
                            var historyItem = schemaResult[appId][contextId][itemId];
                            if (historyItem.OwnerId == 0)
                                tradeHistoryItem.ReceivedItems.Add(historyItem);
                            else
                                tradeHistoryItem.GivenItems.Add(historyItem);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error retrieving trade history:");
                    Console.WriteLine(ex);
                }
                tradeHistoryPages.Add(i, tradeHistoryPageList);
            }
            return tradeHistoryPages.Values.SelectMany(tradeHistoryPage => tradeHistoryPage).ToList();
        }        

        public void AddPendingTradeOfferToList(ulong tradeOfferId)
        {
            lock (_ourPendingTradeOffersLock)
            {
                OurPendingTradeOffers.Add(tradeOfferId);
            }            
        }
        private void RemovePendingTradeOfferFromList(ulong tradeOfferId)
        {
            lock (_ourPendingTradeOffersLock)
            {
                OurPendingTradeOffers.Remove(tradeOfferId);
            }            
        }

        public void StopCheckingPendingTradeOffers()
        {
            _shouldCheckPendingTradeOffers = false;
        }

        private void CheckPendingTradeOffers()
        {
            new Thread(() =>
                {
                    while (_shouldCheckPendingTradeOffers)
                    {
                        var tradeOffers = GetTradeOffers(true);
                        foreach (var tradeOffer in tradeOffers)
                        {
                            if (tradeOffer.IsOurOffer)
                            {
                                lock (_ourPendingTradeOffersLock)
                                {
                                    if (OurPendingTradeOffers.Contains(tradeOffer.Id)) continue;
                                }
                                AddPendingTradeOfferToList(tradeOffer.Id);
                            }
                            else
                            {
                                var args = new TradeOfferEventArgs(tradeOffer);
                                if (tradeOffer.State == TradeOfferState.Active)
                                {
                                    if (tradeOffer.ConfirmationMethod != TradeOfferConfirmationMethod.Invalid)
                                    {
                                        OnTradeOfferNeedsConfirmation(args);
                                    }
                                    else
                                    {
                                        OnTradeOfferReceived(args);
                                    }                                
                                }
                            }
                        }
                        Thread.Sleep(_tradeOfferRefreshRate);
                    }
                }).Start();
            while (_shouldCheckPendingTradeOffers)
            {
                var checkingThreads = new List<Thread>();
                List<ulong> ourPendingTradeOffers;
                lock (_ourPendingTradeOffersLock)
                {
                    ourPendingTradeOffers = OurPendingTradeOffers.ToList();                    
                }
                foreach (var thread in ourPendingTradeOffers.Select(tradeOfferId => new Thread(() =>
                    {
                        var pendingTradeOffer = GetTradeOffer(tradeOfferId);
                        if (pendingTradeOffer.Offer == null)
                        {
                            // Steam's GetTradeOffer/v1 API only gives data for the last 1000 received and 500 sent trade offers, so sometimes this happens
                            pendingTradeOffer.Offer = new TradeOffer { Id = tradeOfferId };
                            OnTradeOfferNoData(new TradeOfferEventArgs(pendingTradeOffer.Offer));
                        }
                        else
                        {
                            var args = new TradeOfferEventArgs(pendingTradeOffer.Offer);
                            if (pendingTradeOffer.Offer.State == TradeOfferState.Active)
                            {
                                // fire this so that trade can be cancelled in UserHandler if the bot owner wishes (e.g. if pending too long)
                                OnTradeOfferChecked(args);
                            }
                            else
                            {
                                // check if trade offer has been accepted/declined, or items unavailable (manually validate)
                                if (pendingTradeOffer.Offer.State == TradeOfferState.Accepted)
                                {
                                    // fire event                            
                                    OnTradeOfferAccepted(args);
                                    // remove from list
                                    RemovePendingTradeOfferFromList(pendingTradeOffer.Offer.Id);
                                }
                                else
                                {
                                    if (pendingTradeOffer.Offer.State == TradeOfferState.NeedsConfirmation)
                                    {
                                        // fire event
                                        OnTradeOfferNeedsConfirmation(args);
                                    }
                                    else if (pendingTradeOffer.Offer.State == TradeOfferState.Invalid || pendingTradeOffer.Offer.State == TradeOfferState.InvalidItems)
                                    {
                                        OnTradeOfferInvalid(args);
                                        RemovePendingTradeOfferFromList(pendingTradeOffer.Offer.Id);
                                    }
                                    else if (pendingTradeOffer.Offer.State == TradeOfferState.InEscrow)
                                    {
                                        OnTradeOfferInEscrow(args);
                                    }
                                    else
                                    {
                                        if (pendingTradeOffer.Offer.State == TradeOfferState.Canceled)
                                        {
                                            OnTradeOfferCanceled(args);
                                        }
                                        else
                                        {
                                            OnTradeOfferDeclined(args);
                                        }
                                        RemovePendingTradeOfferFromList(pendingTradeOffer.Offer.Id);
                                    }
                                }
                            }
                        }
                    })))
                {
                    checkingThreads.Add(thread);
                    thread.Start();
                }
                foreach (var thread in checkingThreads)
                {
                    thread.Join();
                }
                Thread.Sleep(_tradeOfferRefreshRate);
            }
        }

        protected virtual void OnTradeOfferChecked(TradeOfferEventArgs e)
        {
            var handler = TradeOfferChecked;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        protected virtual void OnTradeOfferReceived(TradeOfferEventArgs e)
        {
            if (!_handledTradeOffers.Contains(e.TradeOffer.Id))
            {
                var handler = TradeOfferReceived;
                if (handler != null)
                {
                    handler(this, e);
                }
                _handledTradeOffers.Add(e.TradeOffer.Id);
            }
        }
        protected virtual void OnTradeOfferAccepted(TradeOfferEventArgs e)
        {
            if (!_handledTradeOffers.Contains(e.TradeOffer.Id))
            {
                var handler = TradeOfferAccepted;
                if (handler != null)
                {
                    handler(this, e);
                }
                _handledTradeOffers.Add(e.TradeOffer.Id);
            }
        }
        protected virtual void OnTradeOfferDeclined(TradeOfferEventArgs e)
        {
            if (!_handledTradeOffers.Contains(e.TradeOffer.Id))
            {
                var handler = TradeOfferDeclined;
                if (handler != null)
                {
                    handler(this, e);
                }
                _handledTradeOffers.Add(e.TradeOffer.Id);
            }
        }
        protected virtual void OnTradeOfferCanceled(TradeOfferEventArgs e)
        {
            if (!_handledTradeOffers.Contains(e.TradeOffer.Id))
            {
                var handler = TradeOfferCanceled;
                if (handler != null)
                {
                    handler(this, e);
                }
                _handledTradeOffers.Add(e.TradeOffer.Id);
            }
        }
        protected virtual void OnTradeOfferInvalid(TradeOfferEventArgs e)
        {
            if (!_handledTradeOffers.Contains(e.TradeOffer.Id))
            {
                var handler = TradeOfferInvalid;
                if (handler != null)
                {
                    handler(this, e);
                }
                _handledTradeOffers.Add(e.TradeOffer.Id);
            }
        }
        protected virtual void OnTradeOfferNeedsConfirmation(TradeOfferEventArgs e)
        {
            if (!_awaitingConfirmationTradeOffers.Contains(e.TradeOffer.Id))
            {
                var handler = TradeOfferNeedsConfirmation;
                if (handler != null)
                {
                    handler(this, e);
                }
                _awaitingConfirmationTradeOffers.Add(e.TradeOffer.Id);
            }
        }
        protected virtual void OnTradeOfferInEscrow(TradeOfferEventArgs e)
        {
            if (!_inEscrowTradeOffers.Contains(e.TradeOffer.Id))
            {
                var handler = TradeOfferInEscrow;
                if (handler != null)
                {
                    handler(this, e);
                }
                _inEscrowTradeOffers.Add(e.TradeOffer.Id);
            }
        }
        protected virtual void OnTradeOfferNoData(TradeOfferEventArgs e)
        {
            if (!_handledTradeOffers.Contains(e.TradeOffer.Id))
            {
                var handler = TradeOfferNoData;
                if (handler != null)
                {
                    handler(this, e);
                }
                _handledTradeOffers.Add(e.TradeOffer.Id);
            }
        }

        public event TradeOfferStatusEventHandler TradeOfferChecked;
        public event TradeOfferStatusEventHandler TradeOfferReceived;
        public event TradeOfferStatusEventHandler TradeOfferAccepted;
        public event TradeOfferStatusEventHandler TradeOfferDeclined;
        public event TradeOfferStatusEventHandler TradeOfferCanceled;
        public event TradeOfferStatusEventHandler TradeOfferInvalid;
        public event TradeOfferStatusEventHandler TradeOfferNeedsConfirmation;
        public event TradeOfferStatusEventHandler TradeOfferInEscrow;
        public event TradeOfferStatusEventHandler TradeOfferNoData;

        public class TradeOfferEventArgs : EventArgs
        {
            public TradeOffer TradeOffer { get; private set; }

            public TradeOfferEventArgs(TradeOffer tradeOffer)
            {
                TradeOffer = tradeOffer;
            }
        }
        public delegate void TradeOfferStatusEventHandler(Object sender, TradeOfferEventArgs e);

        public static string RetryWebRequest(SteamWeb steamWeb, string url, string method, NameValueCollection data, bool ajax = false, string referer = "")
        {
            //(_steamWeb, url, "GET", null, false, "http://steamcommunity.com");
            for (var i = 0; i < 10; i++)
            {
                try
                {
                    var response = steamWeb.Request(url, method, data, ajax, referer);
                    using (var responseStream = response.GetResponseStream())
                    {
                        if (responseStream == null) continue;
                        using (var reader = new System.IO.StreamReader(responseStream))
                        {
                            var result = reader.ReadToEnd();
                            if (string.IsNullOrEmpty(result))
                            {
                                Console.WriteLine("Web request failed (status: {0}). Retrying...", response.StatusCode);
                                Thread.Sleep(1000);
                            }
                            else
                            {
                                return result;
                            }
                        }
                    }
                }
                catch (WebException ex)
                {
                    try
                    {
                        using (var responseStream = ex.Response.GetResponseStream())
                        {
                            if (responseStream == null) continue;
                            using (var reader = new System.IO.StreamReader(responseStream))
                            {
                                var result = reader.ReadToEnd();
                                if (!string.IsNullOrEmpty(result))
                                {
                                    return result;
                                }
                                if (ex.Status == WebExceptionStatus.ProtocolError)
                                {
                                    Console.WriteLine("Status Code: {0}, {1} for {2}", (int)((HttpWebResponse)ex.Response).StatusCode, ((HttpWebResponse)ex.Response).StatusDescription, url);
                                }
                            }
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            return "";
        }
    }
}