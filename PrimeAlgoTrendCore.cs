// -------------------------------------------------------------------------------------------------
//
//    This code is a cTrader Algo API example.
//
//    This cBot is intended to be used as a sample and does not guarantee any particular outcome or
//    profit of any kind. Use it at your own risk.
//
// -------------------------------------------------------------------------------------------------

using cAlgo.API;
using cAlgo.API.Indicators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;


namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess, AddIndicators = true)]
    public class PrimeAlgoTrendCore : Robot
    {
        private double _volumeInUnits;

        private primeAlgoKalmanMeanReversion longTermprimeAlgoKalmanMeanReversion;
        private PrimeAlgoKFMRcore ltPrimeAlgoKFMRcore;

        private primeAlgoKalmanMeanReversion slowprimeAlgoKalmanMeanReversion;
        
        

        [Parameter("Source", Group = "Fast MA")]
        public DataSeries FastMaSource { get; set; }

        [Parameter("Period", DefaultValue = 20, Group = "Fast MA")]
        public int FastMaPeriod { get; set; }

        [Parameter("Source", Group = "Slow MA")]
        public DataSeries SlowMaSource { get; set; }

        [Parameter("Period", DefaultValue = 20, Group = "Slow MA")]
        public int SlowMaPeriod { get; set; }

        [Parameter("Volume (Lots)", DefaultValue = 0.01, Group = "Trade")]
        public double VolumeInLots { get; set; }

        [Parameter("Stop Loss (Pips)", DefaultValue = 10, Group = "Trade", MaxValue = 100, MinValue = 1, Step = 1)]
        public double StopLossInPips { get; set; }

        [Parameter("Take Profit (Pips)", DefaultValue = 10, Group = "Trade", MaxValue = 100, MinValue = 1, Step = 1)]
        public double TakeProfitInPips { get; set; }

        [Parameter("Label", DefaultValue = "PrimeAlgoTrendCore", Group = "Trade")]
        public string Label { get; set; }

        public Position[] BotPositions
        {
            get
            {
                return Positions.FindAll(Label);
            }
        }

        protected override void OnStart()
        {
            _volumeInUnits = Symbol.QuantityToVolumeInUnits(VolumeInLots);
            
            longTermprimeAlgoKalmanMeanReversion = Indicators.GetIndicator<primeAlgoKalmanMeanReversion>(MarketData.GetBars(TimeFrame.Renko20).ClosePrices, true, 30);
            ltPrimeAlgoKFMRcore = Indicators.GetIndicator<PrimeAlgoKFMRcore>(MarketData.GetBars(TimeFrame.Renko20).ClosePrices, false, 10);
            

            //fastPrimeAlgoKFMRcore = Indicators.GetIndicator<PrimeAlgoKFMRcore>(MarketData.GetBars(TimeFrame.Renko5).ClosePrices, true, FastMaPeriod);
            slowprimeAlgoKalmanMeanReversion = Indicators.GetIndicator<primeAlgoKalmanMeanReversion>(MarketData.GetBars(TimeFrame.Renko5).ClosePrices, true, SlowMaPeriod);
            
            
            //fastPrimeAlgoKFMRcore.FilterResult.Line.Color = Color.Blue;
            slowprimeAlgoKalmanMeanReversion.FilterResult.Line.Color = Color.Blue;
        }

        protected override void OnBarClosed()
        {
            Print("Pushover Message");

            var parameters = new Dictionary<string, string>
            {
                { "token", "5ZpwCSG3Mz3snPfBVy2WGAcDifezkG" },
                { "user", "9GMhPCM3oevjc1KdxpKM3YtgSoK7fj" },
                { "message" , GetMessage() }
            };


            //new FormUrlEncodedContent(parameters);
            //using var client = new HttpClient();
            //var response = client.PostAsJsonAsync(("https://api.pushover.net/1/messages.json", new FormUrlEncodedContent(parameters)));
            
            using (HttpClient client = new HttpClient())
            {
                var webRequest = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, "https://api.pushover.net/1/messages.json")
                {
                    Content = new StringContent(JsonSerializer.Serialize(parameters), Encoding.UTF8, "application/json")
                };

                var response = client.Send(webRequest);
                StreamReader reader = new StreamReader(response.Content.ReadAsStream());
                Print(reader.ToString());
            }

            
            if (//fastPrimeAlgoKFMRcore.FilterResult.IsFalling() && 
                slowprimeAlgoKalmanMeanReversion.FilterResult.IsFalling() &&
                double.IsNaN(longTermprimeAlgoKalmanMeanReversion.FilterResultUp.LastValue) &&
                double.IsNaN(longTermprimeAlgoKalmanMeanReversion.FilterResultDown.LastValue)
                )
            {
                ClosePositions(TradeType.Buy);
            }
            
            if (//fastPrimeAlgoKFMRcore.FilterResult.IsRising() &&
                slowprimeAlgoKalmanMeanReversion.FilterResult.IsRising() &&
                double.IsNaN(longTermprimeAlgoKalmanMeanReversion.FilterResultUp.LastValue) &&
                double.IsNaN(longTermprimeAlgoKalmanMeanReversion.FilterResultDown.LastValue)
                )
            {
                ClosePositions(TradeType.Sell);
            }
            
            if (//fastPrimeAlgoKFMRcore.FilterResult.IsRising() &&
                slowprimeAlgoKalmanMeanReversion.FilterResult.IsRising() &&
                !double.IsNaN(longTermprimeAlgoKalmanMeanReversion.FilterResultUp.LastValue)
                )
            {
                ClosePositions(TradeType.Sell);
                
                if (BotPositions.Length == 0)
                {
                    ExecuteMarketOrder(TradeType.Buy, SymbolName, _volumeInUnits, Label, StopLossInPips, TakeProfitInPips);
                }

                
            }
            else if (//fastPrimeAlgoKFMRcore.FilterResult.IsFalling() &&
                    slowprimeAlgoKalmanMeanReversion.FilterResult.IsFalling() && 
                    !double.IsNaN(longTermprimeAlgoKalmanMeanReversion.FilterResultDown.LastValue)
                    )
            {
                ClosePositions(TradeType.Buy);
                
                if (BotPositions.Length == 0)
                {
                    ExecuteMarketOrder(TradeType.Sell, SymbolName, _volumeInUnits, Label, StopLossInPips, TakeProfitInPips);
                }
            }
        }
        

        private void ClosePositions(TradeType tradeType)
        {
            foreach (var position in BotPositions)
            {
                if (position.TradeType != tradeType) continue;

                ClosePosition(position);
                
            }
        }
        
        private string GetMessage()
        {
            string res = $"{Symbol.Name}   {DateTime.Now:HH:mm}   @{(Bid + Ask) / 2:0.0000(0)}\n";
            
            string prev = "none";
            string curr = "none";
            
            //if(!double.IsNaN(longTermprimeAlgoKalmanMeanReversion.FilterResultDown.Last(1)))
            //    prev = "down";
            //else if (!double.IsNaN(longTermprimeAlgoKalmanMeanReversion.FilterResultUp.Last(1)))
            //    prev = "up";
                
            //if(!double.IsNaN(longTermprimeAlgoKalmanMeanReversion.FilterResultDown.Last(0)))
            //    curr = "down";
            //else if (!double.IsNaN(longTermprimeAlgoKalmanMeanReversion.FilterResultUp.Last(0)))
            //    curr = "up"; 
             
            res = res + $"LT: {GetLongTermDirection(1)} --> {GetLongTermDirection(0)}\n";
            
            
            prev = "none";
            curr = "none";
            
            if(!double.IsNaN(slowprimeAlgoKalmanMeanReversion.FilterResultDown.Last(1)))
                prev = "down";
            else if (!double.IsNaN(slowprimeAlgoKalmanMeanReversion.FilterResultUp.Last(1)))
                prev = "up";
                
            if(!double.IsNaN(slowprimeAlgoKalmanMeanReversion.FilterResultDown.Last(0)))
                curr = "down";
            else if (!double.IsNaN(slowprimeAlgoKalmanMeanReversion.FilterResultUp.Last(0)))
                curr = "up"; 
             
            res = res + $"ST: {prev} --> {curr}\n";
            
            
            return res;
        }
        
        private string GetLongTermDirection(int index)
        {
            string res = "none";
            
            if (!double.IsNaN(longTermprimeAlgoKalmanMeanReversion.FilterResultUp.Last(index)))
            {
                res = "up(1)";
            } else if (!double.IsNaN(longTermprimeAlgoKalmanMeanReversion.FilterResultDown.Last(index)))
            {
                res = "down(1)";
            } else if (!double.IsNaN(ltPrimeAlgoKFMRcore.FilterResultUp.Last(index)))
            {
                res = "up(2)";
            } else if (!double.IsNaN(ltPrimeAlgoKFMRcore.FilterResultDown.Last(index)))
            {
                res = "down(2)";
            }
            
            return res;
        
        }
    }
}