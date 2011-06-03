using Spring.RabbitQuickStart.Common.Data;

namespace Spring.RabbitQuickStart.Server.Services.Stubs
{
    public class TradingServiceStub : ITradingService
    {
        public void ProcessTrade(TradeRequest request, TradeResponse response)
        {
            //do nothing implementation, typical implementations would persist state to the database.
        }
    }
}