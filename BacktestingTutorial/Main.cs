using System;
using QuantConnect.Data;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    public class BacktestingTutorial : QCAlgorithm
    {
        private Symbol _spy;
        private Symbol _bnd;
        private SimpleMovingAverage _sma;
        private DateTime _rebalanceTime;
        private bool _uptrend;

        public override void Initialize()
        {
            SetStartDate(2018, 1, 1); // Set Start Date
            SetEndDate(2021, 1, 1); // Set Start Date
            SetCash(100000); // Set Strategy Cash
            
            _spy = AddEquity("SPY", Resolution.Daily).Symbol;
            _bnd = AddEquity("BND", Resolution.Daily).Symbol;
            var smaLength = Int32.Parse(GetParameter("smaLength") ?? "30");
            _sma = SMA(_spy, smaLength, Resolution.Daily);
            _rebalanceTime = DateTime.MinValue;
            _uptrend = true;
        }

        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// Slice object keyed by symbol containing the stock data
        public override void OnData(Slice data)
        {
            if (!_sma.IsReady || !data.ContainsKey(_spy) || !data.ContainsKey(_bnd))
            {
                return;
            }

            if (data[_spy].Price >= _sma.Current.Value)
            {
                if (Time >= _rebalanceTime || !_uptrend)
                {
                    SetHoldings(_spy, 0.8m);
                    SetHoldings(_bnd, 0.2m);
                    _uptrend = true;
                    _rebalanceTime = Time.AddDays(30);
                    
                }
            }

            else
            {
                if (Time>=_rebalanceTime || _uptrend)
                {
                    SetHoldings(_spy, 0.2m);
                    SetHoldings(_bnd, 0.8m);
                    _uptrend = false;
                    _rebalanceTime = Time.AddDays(30);
                }
            }
            
            Plot("Benchmark", "SMA", _sma.Current.Value);
        }
    }
}
