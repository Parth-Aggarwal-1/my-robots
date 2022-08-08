using System;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Securities.Equity;

namespace QuantConnect.Algorithm.CSharp
{
    public class Tutorial2 : QCAlgorithm
    {
        private Symbol _symbol;
        private decimal _highestPrice = 0;
        private OrderTicket _entryTicket;
        private OrderTicket _stopMarketTicket;
        private DateTime _entryTime = DateTime.MinValue;
        private DateTime _stopMarketOrderFillTime = DateTime.MinValue;

        public override void Initialize()
        {
            SetStartDate(2018, 1, 1); // Set Start Date
            SetEndDate(2021, 1, 1); // Set Start Date
            SetCash(100000); // Set Strategy Cash

            _symbol = AddEquity("BHE", Resolution.Hour).Symbol;
        }

        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// Slice object keyed by symbol containing the stock data
        public override void OnData(Slice data)
        {
            // Wait 30 days since last execution
            if ((Time - _stopMarketOrderFillTime).Days < 30) return;

            var price = Securities[_symbol].Price;
            
            // Send entry limit order
            if (!Portfolio.Invested && !Transactions.GetOpenOrders(_symbol).Any())
            {
                var quantity = CalculateOrderQuantity(_symbol, 0.9);
                _entryTicket = LimitOrder(_symbol, quantity, price, "Entry Order");
                _entryTime = Time;
            }
            
            // Move limit price if not filled after 1 day
            _entryTime = Time;
            _entryTicket.Update(new UpdateOrderFields() {LimitPrice = price});
            
            // Move up trailing stop price
            if (_stopMarketTicket is not null && Portfolio.Invested)
            {
                if (price > _highestPrice)
                {
                    _highestPrice = price;
                    _stopMarketTicket.Update(new UpdateOrderFields() {StopPrice = price * 0.95m});
                }
            }

            return;
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status != OrderStatus.Filled) return;
            
            // send stop loss order if entry limit order is filled
            if (_entryTicket is not null && orderEvent.OrderId == _entryTicket.OrderId)
            {
                _stopMarketTicket = StopMarketOrder(
                    _symbol, 
                    -_entryTicket.Quantity,
                    _entryTicket.AverageFillPrice * 0.95m);
            }

            // save fill time of stop loss order
            if (_stopMarketTicket is not null && _stopMarketTicket.OrderId == orderEvent.OrderId)
            {
                _stopMarketOrderFillTime = Time;
                _highestPrice = 0;
            }
        }
    }
}
