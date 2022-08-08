using System;
using System.Collections.Generic;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    public class Umber : QCAlgorithm
    {
        private Symbol _symbol;
        private RollingWindow<QuoteBar> _slowCandlesRollingWindow = new RollingWindow<QuoteBar>(5);
        private RollingWindow<QuoteBar> _fastCandlesRollingWindow = new RollingWindow<QuoteBar>(5);
        private readonly ExponentialMovingAverage _slow21Ema = new ExponentialMovingAverage(21);
        private readonly ExponentialMovingAverage _slow8Ema = new ExponentialMovingAverage(8);
        private readonly ExponentialMovingAverage _fast21Ema = new ExponentialMovingAverage(21);
        private readonly ExponentialMovingAverage _fast13Ema = new ExponentialMovingAverage(13);
        private readonly ExponentialMovingAverage _fast8Ema = new ExponentialMovingAverage(8);
        private readonly RollingWindow<IndicatorDataPoint> _slow21EmaRollingWindow = new RollingWindow<IndicatorDataPoint>(5);
        private readonly RollingWindow<IndicatorDataPoint> _slow8EmaRollingWindow = new RollingWindow<IndicatorDataPoint>(5);
        private readonly RollingWindow<IndicatorDataPoint> _fast21EmaRollingWindow = new RollingWindow<IndicatorDataPoint>(5);
        private readonly RollingWindow<IndicatorDataPoint> _fast13EmaRollingWindow = new RollingWindow<IndicatorDataPoint>(5);
        private readonly RollingWindow<IndicatorDataPoint> _fast8EmaRollingWindow = new RollingWindow<IndicatorDataPoint>(5);
        private readonly List<OrderTicket> _orderTickets = new List<OrderTicket>(){};
        private decimal _risk;
        private decimal _trailingStopLossHigh;
        
        public override void Initialize()
        {
            Debug("Initializing");
            
            SetStartDate(2021, 1, 10); // Set Start Date
            SetEndDate(2021, 1, 20); // Set Start Date
            SetCash(100000); // Set Strategy Cash
            SetBrokerageModel(BrokerageName.OandaBrokerage, AccountType.Margin);
            
            //Rolling Windows
            _slowCandlesRollingWindow = new RollingWindow<QuoteBar>(5);
            _fastCandlesRollingWindow = new RollingWindow<QuoteBar>(5);
            Consolidate<QuoteBar>(_symbol, TimeSpan.FromMinutes(5), FiveMinutelyDataHandler);
            Consolidate<QuoteBar>(_symbol, TimeSpan.FromHours(1), HourlyDataHandler);

            // Pair
            _symbol = AddForex("EURSGD", Resolution.Minute, Market.Oanda, true, 1M).Symbol;

        }

        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// Slice object keyed by symbol containing the stock data
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings("SPY", 1);
                Debug("Purchased Stock");
            }
        }
        
        private void HourlyDataHandler(QuoteBar obj)
        {
            _slowCandlesRollingWindow.Add(obj);
            _slow21EmaRollingWindow.Add(_slow21Ema.Current);
            _slow8EmaRollingWindow.Add(_slow8Ema.Current);
        }

        private void FiveMinutelyDataHandler(QuoteBar obj)
        {
            _fastCandlesRollingWindow.Add(obj);
            _fast21EmaRollingWindow.Add(_fast21Ema.Current);
            _fast13EmaRollingWindow.Add(_fast13Ema.Current);
            _fast8EmaRollingWindow.Add(_fast8Ema.Current);
        }
    }
}
