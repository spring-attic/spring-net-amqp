

using System.Collections;
using Spring.RabbitQuickStart.Common.Data;

namespace Spring.RabbitQuickStart.Server.Services.Stubs
{
    public class CreditCheckServiceStub : ICreditCheckService
    {
        public bool CanExecute(TradeRequest tradeRequest, IList errors)
        {
            return true;
        }
    }
}