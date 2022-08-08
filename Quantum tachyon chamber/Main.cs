namespace QuantConnect.Algorithm.CSharp
{
    public class QuantumTachyonChamber : QCAlgorithm
    {

        public override void Initialize()
        {
            SetStartDate(2020, 6, 14);  //Set Start Date
            SetCash(100000);             //Set Strategy Cash
            
            // AddEquity("SPY", Resolution.Minute);


        }

        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// Slice object keyed by symbol containing the stock data
        public override void OnData(Slice data)
        {
            // if (!Portfolio.Invested)
            // {
            //    SetHoldings("SPY", 1);
            //    Debug("Purchased Stock");
            //}
        }

    }
}
