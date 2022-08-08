using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using QuantConnect.Packets;

namespace QuantConnect.Algorithm.CSharp
{
    public class Alternativo : QCAlgorithm
    {
        private Symbol _symbol;
        private RollingWindow<QuoteBar> _rollingWindow;
        private ExponentialMovingAverage _fastEma;
        private ExponentialMovingAverage _midEma;
        private ExponentialMovingAverage _slowEma;
        private RollingWindow<IndicatorDataPoint> _slowEmaRollingWindow;
        private RollingWindow<IndicatorDataPoint> _midEmaRollingWindow;
        private RollingWindow<IndicatorDataPoint> _fastEmaRollingWindow;
        private bool _awaitingBuyTrigger = false;
        private bool _awaitingPullbackAboveFastEmaToBuy = false;
        private bool _awaitingPullbackAboveMidEmaToBuy;
        private decimal _stopLoss;
        private decimal _takeProfit;

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug("Order event detected");
            if (orderEvent.Status == OrderStatus.Filled)
            {
                var avgOrderPrice = orderEvent.FillPrice;
                var minimumPrice = GetMinimumOfLastFiveCandles();
                var risk = avgOrderPrice - minimumPrice;
                _stopLoss = minimumPrice;
                _takeProfit = avgOrderPrice +  risk;
                Debug($"Order placed. Status: {orderEvent.Status.ToString()}, AvgOrderPrice: {avgOrderPrice}, MinPrice: {minimumPrice} ,Risk: {risk}, Stop loss: {_stopLoss}, Take Profit: {_takeProfit}");
            }
        }

        private decimal GetMinimumOfLastFiveCandles()
        {
            var minimums = new List<decimal>();
            
            for (int i = 0; i < 5; i++)
            {
                minimums.Add(_rollingWindow[i].Low);
            }

            return minimums.Min();
        }

        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// Slice object keyed by symbol containing the stock data
        public override void OnData(Slice data)
        {
            _rollingWindow.Add(data[_symbol.Value]);
            _slowEmaRollingWindow.Add(_slowEma.Current);
            _midEmaRollingWindow.Add(_midEma.Current);
            _fastEmaRollingWindow.Add(_fastEma.Current);

            if (IsWarmingUp)
            {
                Debug("Still Warming up");
                return;
            }
            

            if (Portfolio.Invested)
            {
                var lastClose = _rollingWindow[0].Ask.Close;
                if (lastClose >= _takeProfit || lastClose <= _stopLoss)
                {
                    Liquidate();
                    return;
                }
                return;
            }

            if (_awaitingPullbackAboveMidEmaToBuy)
            {
                if (_rollingWindow[0].Close < _slowEma.Current.Value)
                {
                    ResetTriggers();
                    Debug("Breached slow Ema, no trade");
                    return;
                }
                if (_rollingWindow[0].Close > _midEma.Current.Value)
                {
                    PlaceOrder();
                    Debug("*************We had pullback, placed order************");
                    ResetTriggers();
                    return;
                }
            }

            if (_awaitingPullbackAboveFastEmaToBuy)
            {
                if (_rollingWindow[0].Close < _midEma.Current.Value)
                {
                    _awaitingPullbackAboveFastEmaToBuy = false;
                    _awaitingPullbackAboveMidEmaToBuy = true;
                    Debug("Breached mid Ema, awaiting pullback");
                    return;
                }
                
                if (_rollingWindow[0].Close > _fastEma.Current.Value)
                {
                    PlaceOrder();
                    Debug("*************We had pullback, placed order************");
                    ResetTriggers();
                    return;
                }
            }

            if (_awaitingBuyTrigger)
            {
                if (IsTrendingUp(data))
                {
                    Debug("No trigger, in uptrend");
                    return;
                }

                if (_rollingWindow[0].Close < _midEma.Current.Value)
                {
                    _awaitingPullbackAboveMidEmaToBuy = true;
                    Debug("Breached mid Ema, awaiting pullback");
                    return;
                }
                if (_rollingWindow[0].Close < _fastEma.Current.Value)
                {
                    _awaitingPullbackAboveFastEmaToBuy = true;
                    Debug("Breached fast Ema, awaiting pullback");
                    return;
                }

                ResetTriggers();
            }

            if (IsTrendingUp(data))
            {
                _awaitingBuyTrigger = true;
            }

            else
            {
                _awaitingBuyTrigger = false;
            }
        }

        private void ResetTriggers()
        {
            _awaitingBuyTrigger = false;
            _awaitingPullbackAboveFastEmaToBuy = false;
            _awaitingPullbackAboveMidEmaToBuy = false;
        }

        private void PlaceOrder()
        {
            var quantity = CalculateOrderQuantity(_symbol, 0.1m);
            var ticket = MarketOrder(_symbol, quantity, false, "mktorder");
            Debug("Order placed");
        }

        private bool IsTrendingUp(Slice slice)
        {
            var results = new List<bool>();
            for (int i = 0; i < 4; i++)
            {
                var result = _fastEmaRollingWindow[i] > _midEmaRollingWindow[i]
                             && _midEmaRollingWindow[i] > _slowEmaRollingWindow[i]
                             && _rollingWindow[i].Low > _fastEmaRollingWindow[i];
                results.Add(result);
            }
            return results.All(r => r == true);
        }

        public override void Initialize()
        {
            SetStartDate(2021, 10, 1); // Set Start Date
            SetEndDate(2021, 11, 1); // Set Start Date
            SetCash(10000); // Set Strategy Cash
            SetBrokerageModel(BrokerageName.OandaBrokerage, AccountType.Margin);

            // Symbol
            _symbol = AddForex("EURUSD", Resolution.Minute, Market.Oanda).Symbol;

            // Indicators
            _fastEma = EMA(_symbol, 50, Resolution.Minute);
            _midEma = EMA(_symbol, 100, Resolution.Minute);
            _slowEma = EMA(_symbol, 150, Resolution.Minute);
            
            // Rolling Windows
            _rollingWindow = new RollingWindow<QuoteBar>(100);
            _slowEmaRollingWindow = new RollingWindow<IndicatorDataPoint>(100);
            _midEmaRollingWindow = new RollingWindow<IndicatorDataPoint>(100);
            _fastEmaRollingWindow = new RollingWindow<IndicatorDataPoint>(100);

            SetWarmup(200);
        }
    }
}
