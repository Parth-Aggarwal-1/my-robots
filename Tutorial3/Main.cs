using System;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    public class Tutorial3 : QCAlgorithm
    {
        private Symbol _symbol;
        private SimpleMovingAverage _sma;

        public override void Initialize()
        {
            SetStartDate(2019, 1, 1); // Set Start Date
            SetEndDate(2020, 1, 1); // Set Start Date
            SetCash(100000); // Set Strategy Cash
            
            _symbol = AddEquity("SPY", Resolution.Daily).Symbol;
            _sma = SMA(_symbol, 30, Resolution.Daily);
            var closingPrices = History(_symbol, 30, Resolution.Daily)
                .Select(tb => new Tuple<DateTime, decimal>(tb.EndTime,tb.Close));
            foreach (var closingPrice in closingPrices)
            {
                _sma.Update(closingPrice.Item1, closingPrice.Item2);
            }
        }

        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// Slice object keyed by symbol containing the stock data
        public override void OnData(Slice data)
        {
            if (!_sma.IsReady) return;

            var hist = History(
                _symbol,
                new TimeSpan(365, 0, 0, 0),
                Resolution.Daily);

            var low = hist.Select(x => x.Low).Min();
            var high = hist.Select(x => x.High).Max();

            var price = Securities[_symbol].Price;
            
            if (price*1.05m >= high 
                && _sma.Current.Value < price
                && !Portfolio[_symbol].IsLong)
            {
                SetHoldings(_symbol, 1);
            }
            else
            {
                if (price*0.95m<=low
                    && _sma.Current.Value>price
                    && !Portfolio[_symbol].IsShort)
                {
                    SetHoldings(_symbol, -1);
                }
                else
                {
                    Liquidate();
                }
            }
            
            Plot("Benchmark", "52w-High", high);
            Plot("Benchmark", "52w-Low", low);
            Plot("Benchmark", "SMA", _sma.Current.Value);
        }
    }
}
