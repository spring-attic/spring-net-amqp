

using System.Collections;
using Common.Logging;
using Spring.RabbitQuickStart.Common.Data;
using Spring.RabbitQuickStart.Server.Services;
using Spring.Util;

namespace Spring.RabbitQuickStart.Server.Handlers
{
    public class StockAppHandler
    {
        #region Logging

        private readonly ILog logger = LogManager.GetLogger(typeof(StockAppHandler));

        #endregion
        private IExecutionVenueService executionVenueService;

        private ICreditCheckService creditCheckService;

        private ITradingService tradingService;

        public StockAppHandler(IExecutionVenueService executionVenueService, ICreditCheckService creditCheckService, ITradingService tradingService)
        {
            this.executionVenueService = executionVenueService;
            this.creditCheckService = creditCheckService;
            this.tradingService = tradingService;
        }

        public TradeResponse Handle(TradeRequest tradeRequest)
        {
            logger.Info("Recieved trade request");
            TradeResponse tradeResponse;
            ArrayList errors = new ArrayList();
            if (creditCheckService.CanExecute(tradeRequest, errors))
            {
                tradeResponse = executionVenueService.ExecuteTradeRequest(tradeRequest);
            }
            else
            {
                tradeResponse = new TradeResponse();
                tradeResponse.Error = true;
                tradeResponse.ErrorMessage = StringUtils.ArrayToCommaDelimitedString(errors.ToArray());
                
            }
            tradingService.ProcessTrade(tradeRequest, tradeResponse);
            return tradeResponse;
        }
    }
}