using System;
using QuantConnect.Data;
using QuantConnect;
using QuantConnect.Parameters;
using QuantConnect.Benchmarks;
using QuantConnect.Brokerages;
using QuantConnect.Util;
using QuantConnect.Interfaces;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Indicators;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Custom;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Notifications;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;
using QuantConnect.Orders.Slippage;
using QuantConnect.Scheduling;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Forex;
using QuantConnect.Securities.Interfaces;
using QuantConnect.Python;
using QuantConnect.Storage;


namespace QuantConnect.Algorithm.CSharp
{
    public class Genesis : QCAlgorithm
    {
        private Symbol _symbol;
        private decimal _entryPrice = 0;
        private readonly TimeSpan _period = new(31,0,0,0);
        private DateTime _nextEntryTime;

        public override void Initialize()
        {
            SetStartDate(2020, 1, 1); // Set Start Date
            SetEndDate(2021, 1, 1); // Set Start Date
            SetCash(100000); // Set Strategy Cash
            
            var spy = AddEquity("SPY", Resolution.Daily);
            spy.SetDataNormalizationMode(DataNormalizationMode.Raw);
            _symbol = spy.Symbol;
            _nextEntryTime = this.Time;

            //Brokerage
            SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage, AccountType.Margin);
            
            SetBenchmark("SPY");
        }

        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// Slice object keyed by symbol containing the stock data
        public override void OnData(Slice data)
        {
            var price = data[_symbol].Close;
            
            if (!Portfolio.Invested)
            {
                if (_nextEntryTime <= this.Time)
                {
                    //SetHoldings(_symbol,1m);
                    MarketOrder(_symbol, Math.Round(Portfolio.Cash / price));
                    Debug($"Purchased Stock SPY @ {price}");
                    _entryPrice = price;
                }
            }
            else
            {
                if ((_entryPrice * 1.1m) < price || (_entryPrice*0.9m) > price)
                {
                    Liquidate();
                    Debug($"Closed SPY position @{price}");
                    _nextEntryTime = Time.AddDays(31);
                }
            }
        }
    }
}
