using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Util;

namespace QuantConnect.Algorithm.CSharp
{
    public class UniverseTutorial : QCAlgorithm
    {
        private DateTime _rebalanceTime;
        private HashSet<Symbol> _activeStocks;
        private IEnumerable<PortfolioTarget> _portfolioTargets;

        public override void Initialize()
        {
            //Backtest settings
            SetStartDate(2019,1,1); // Set Start Date
            SetEndDate(2021, 1,1); // Set Start Date
            SetCash(100000); // Set Strategy Cash

            _rebalanceTime = DateTime.MinValue;
            _activeStocks = new HashSet<Symbol>();

            AddUniverse(CoarseFilter, FineFilter);
            UniverseSettings.Resolution = Resolution.Hour;

            _portfolioTargets = new PortfolioTarget[] {};
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach (var removedSecurity in changes.RemovedSecurities)
            {
                var removedSymbol = removedSecurity.Symbol;
                Liquidate(removedSymbol);
                _activeStocks.Remove(removedSymbol);
            }

            foreach (var addedSecurity in changes.AddedSecurities)
            {
                var addedSymbol = addedSecurity.Symbol;
                _activeStocks.Add(addedSymbol);
            }

            _portfolioTargets = _activeStocks.Select(s => new PortfolioTarget(s, 1m / (_activeStocks.Count)));
        }

        private IEnumerable<Symbol> FineFilter(IEnumerable<FineFundamental> arg)
        {
            return arg
                .OrderBy(x => x.MarketCap)
                .Where(x => x.MarketCap > 0)
                .Take(10)
                .Select(x => x.Symbol);
        }

        private IEnumerable<Symbol> CoarseFilter(IEnumerable<CoarseFundamental> arg)
        {
            if (Time<=_rebalanceTime)
            {
                return Universe.Unchanged;
            }
            
            _rebalanceTime = Time.AddDays(30);

            var sortedByDollarVolume = arg.OrderByDescending(x => x.DollarVolume);

            return sortedByDollarVolume
                .Where(s => s.HasFundamentalData && s.Price > 10m)
                .Take(200)
                .Select(s => s.Symbol);

        }

        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// Slice object keyed by symbol containing the stock data
        public override void OnData(Slice data)
        {
            if (_portfolioTargets.IsNullOrEmpty()) return;

            if (_activeStocks.Any(s => !data.ContainsKey(s))) return;

            SetHoldings(_portfolioTargets.ToList());
            _portfolioTargets = new PortfolioTarget[] {};
        }
    }
}