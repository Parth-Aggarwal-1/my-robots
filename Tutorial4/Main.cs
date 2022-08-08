using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    public class Tutorial4 : QCAlgorithm
    {
        private Symbol _symbol;
        private RollingWindow<TradeBar> _rollingWindow;

        public override void Initialize()
        {
            // Backtest conditions
            SetStartDate(2014, 5, 2); // Set Start Date
            SetEndDate(2014, 5, 14); // Set Start Date
            SetCash(100000); // Set Strategy Cash
            
            _symbol = AddEquity("SPY", Resolution.Minute).Symbol;
            _rollingWindow = new RollingWindow<TradeBar>(2);
            Consolidate(_symbol, Resolution.Daily, CustomBarHandler);

            Schedule.On(DateRules.EveryDay(_symbol), TimeRules.BeforeMarketClose(_symbol, 15), ExitPositions);
        }

        private void ExitPositions()
        {
            Liquidate(_symbol);
        }


        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// Slice object keyed by symbol containing the stock data
        public override void OnData(Slice data)
        {
            if (!_rollingWindow.IsReady) return;

            if (Time.Hour != 9 && Time.Minute != 31) return;

            if (data[_symbol].Open >= 1.01m*_rollingWindow[0].Close)
            {
                SetHoldings(_symbol, -1);
                return;
            }

            if (data[_symbol].Open <= 0.99m*_rollingWindow[0].Close)
            {
                SetHoldings(_symbol, 1);
                return;
            }
        }

        private void CustomBarHandler(TradeBar bar)
        {
            _rollingWindow.Add(bar);
        }
    }
}
