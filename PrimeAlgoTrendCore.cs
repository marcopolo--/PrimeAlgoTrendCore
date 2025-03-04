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

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None, AddIndicators = true)]
    public class PrimeAlgoTrendCore : Robot
    {
        private double _volumeInUnits;

        private PrimeAlgoKFMRcore fastPrimeAlgoKFMRcore;

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

            fastPrimeAlgoKFMRcore = Indicators.GetIndicator<PrimeAlgoKFMRcore>(MarketData.GetBars(TimeFrame.Renko5), FastMaSource, true, FastMaPeriod);
            slowprimeAlgoKalmanMeanReversion = Indicators.GetIndicator<primeAlgoKalmanMeanReversion>(TimeFrame.Renko5, FastMaSource, true, SlowMaPeriod);

            fastPrimeAlgoKFMRcore.FilterResult.Line.Color = Color.Blue;
            slowprimeAlgoKalmanMeanReversion.FilterResult.Line.Color = Color.Red;
        }

        protected override void OnBarClosed()
        {
            if (fastPrimeAlgoKFMRcore.FilterResult.HasCrossedAbove(slowprimeAlgoKalmanMeanReversion.FilterResult, 0))
            {
                ClosePositions(TradeType.Sell);

                ExecuteMarketOrder(TradeType.Buy, SymbolName, _volumeInUnits, Label, StopLossInPips, TakeProfitInPips);
            }
            else if (fastPrimeAlgoKFMRcore.FilterResult.HasCrossedBelow(slowprimeAlgoKalmanMeanReversion.FilterResult, 0))
            {
                ClosePositions(TradeType.Buy);

                ExecuteMarketOrder(TradeType.Sell, SymbolName, _volumeInUnits, Label, StopLossInPips, TakeProfitInPips);
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
    }
}