

using System;
using System.Threading;
using Common.Logging;
using Spring.RabbitQuickStart.Common.Data;

namespace Spring.RabbitQuickStart.Server.Services.Stubs
{
    public class ExecutionVenueServiceStub : IExecutionVenueService
    {
        #region Logging

        private readonly ILog logger = LogManager.GetLogger(typeof(ExecutionVenueServiceStub));

        #endregion

        public TradeResponse ExecuteTradeRequest(TradeRequest request)
        {
            TradeResponse response = new TradeResponse();
            response.OrderType = request.OrderType;
            response.Price = CalculatePrice(request.Ticker, request.Quantity, request.OrderType, request.Price, request.UserName);
            response.Quantity = request.Quantity;
            response.Ticker = request.Ticker;
            response.ConfirmationNumber = new Guid().ToString();

            logger.Info("Sleeping 2 seconds to simulate processing...");
            Thread.Sleep(2000);
            return response;
        }

        private decimal CalculatePrice(string ticker, long quantity, string ordertype, decimal limitPrice, string userName)
        {
            // provide as sophisticated an impl as testing requires...for now all the same price.
            if (ordertype.CompareTo("LIMIT") == 0)
            {
                return limitPrice;
            }
            else
            {
                return 27.6m;
            }
            
        }
    }
}