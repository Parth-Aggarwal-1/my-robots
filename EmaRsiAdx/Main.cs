using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using Timer = System.Threading.Timer;

namespace QuantConnect.Algorithm.CSharp
{
    public class EmaRsiAdx : QCAlgorithm
    {
        private Symbol _symbol;
        private ExponentialMovingAverage _ema;
        private RollingWindow<QuoteBar> _rollingWindow;
        private RollingWindow<IndicatorDataPoint> _emaRollingWindow;
        private RelativeStrengthIndex _rsi;
        private AverageDirectionalIndex _adx;
        private RollingWindow<IndicatorDataPoint> _rsiRollingWindow;
        private RollingWindow<IndicatorDataPoint> _adxRollingWindow;
        private decimal _takeProfit;
        private OrderTicket _ticket;
        private decimal _stopLoss;
        private List<EnteredTrade> _trades =  new List<EnteredTrade>(); 

        public override void Initialize()
        {
            SetStartDate(2021, 10, 1); // Set Start Date
            SetEndDate(2021, 10, 10); // Set Start Date
            SetCash(10000); // Set Strategy Cash
            SetBrokerageModel(BrokerageName.OandaBrokerage, AccountType.Margin);

            // Symbol
            _symbol = AddForex("EURUSD", Resolution.Minute, Market.Oanda).Symbol;

            // Indicators
            _ema = EMA(_symbol, 50, Resolution.Minute);
            _rsi = RSI(_symbol, 3, MovingAverageType.Exponential, Resolution.Minute);
            _adx = ADX(_symbol, 5, Resolution.Minute);
            
            // Rolling Windows
            _rollingWindow = new RollingWindow<QuoteBar>(100);
            _emaRollingWindow = new RollingWindow<IndicatorDataPoint>(100);
            _rsiRollingWindow = new RollingWindow<IndicatorDataPoint>(100);
            _adxRollingWindow = new RollingWindow<IndicatorDataPoint>(100);

            SetWarmup(200);
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            var trade = _trades.First(t => t.IsActive);
            if (orderEvent.Status == OrderStatus.Filled)
            {
                if (orderEvent.Id == trade.MarketOrderTicket.OrderId)
                {
                    var stopLoss = LowestOfFive() - 0.0003m;
                    var risk = orderEvent.FillPrice - stopLoss;
                    var takeProfit = orderEvent.FillPrice + 1.5m * risk;
                    Debug($"Entered Trade. EntryPrice: {orderEvent.FillPrice}, TP: {_takeProfit}, Stop: {stopLoss}");
                    trade.StopMarketOrderTicket = StopMarketOrder(_symbol, -trade.MarketOrderTicket.Quantity, stopLoss);
                    trade.LimitOrderTicket = LimitOrder(_symbol, -trade.MarketOrderTicket.Quantity, takeProfit);
                    return;
                }
                
                if (orderEvent.OrderId == _trades.First(t=>t.IsActive).StopMarketOrderTicket.OrderId)
                {
                    trade.IsActive = false;
                    trade.LimitOrderTicket.Cancel();
                    _trades.RemoveAll(t => t.IsActive == false);
                    Debug($"Exited trade at loss. Exit: {orderEvent.FillPrice}, Entry: {trade.MarketOrderTicket.AverageFillPrice}");
                    return;
                }

                if (orderEvent.OrderId == _trades.First(t=>t.IsActive).LimitOrderTicket.OrderId)
                {
                    trade.IsActive = false;
                    trade.StopMarketOrderTicket.Cancel();
                    _trades.RemoveAll(t => t.IsActive == false);
                    Debug($"Exited trade at profit. Exit: {orderEvent.FillPrice}, Entry: {trade.MarketOrderTicket.AverageFillPrice}");
                    return;
                }
            }
        }

        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// Slice object keyed by symbol containing the stock data
        public override void OnData(Slice data)
        {
            _rollingWindow.Add(data[_symbol.Value]);
            _emaRollingWindow.Add(_ema.Current);
            _rsiRollingWindow.Add(_rsi.Current);
            _adxRollingWindow.Add(_adx.Current);
            
            if (IsWarmingUp)
            {
                return;
            }
            
            if (Portfolio.Invested && _trades.Any(t => t.IsActive))
            {
                return;
            }

            if (TrendCondition() && 
                MomentumCondition() && 
                VolatilityCondition())
            {
                var trade = new EnteredTrade() {IsActive = true};
                _trades.Add(trade);
                var quantity = CalculateOrderQuantity(_symbol, 0.01m);
                trade.MarketOrderTicket = MarketOrder(_symbol, quantity);
                Debug($"Placing Market Order. Current Price: {_rollingWindow[0].Close}");
            }
        }

        private decimal LowestOfFive()
        {
            var minimums = new List<decimal>();
            for (int i = 0; i < 5; i++)
            {
                minimums.Add(_rollingWindow[i].Low);
            }

            return minimums.Min();
        }

        private bool MomentumCondition()
        {
            return _rsi.Current.Value < 20;
        }

        private bool VolatilityCondition()
        {
            return _adx.Current.Value > 30;
        }

        private bool TrendCondition()
        {
            var results = new List<bool>();
            for (int i = 0; i < 9; i++)
            {
                var result = _rollingWindow[i].Low > _emaRollingWindow[i];
                results.Add(result);
            }
            
            return results.All(r => r == true);
        }
    }

    internal class EnteredTrade
    {
        public int TradeId { get; set; }
        public OrderTicket MarketOrderTicket { get; set; }
        public OrderTicket StopMarketOrderTicket { get; set; }
        public OrderTicket LimitOrderTicket { get; set; }
        public bool IsActive { get; set; }
    }
}
