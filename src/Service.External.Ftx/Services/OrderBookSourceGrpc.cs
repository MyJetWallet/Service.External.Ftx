using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.Domain.ExternalMarketApi;
using MyJetWallet.Domain.ExternalMarketApi.Dto;

namespace Service.External.Ftx.Services
{
    public class OrderBookSourceGrpc: IOrderBookSource
    {
        private readonly OrderBookManager _manager;
        private readonly ILogger<OrderBookSourceGrpc> _logger;

        public OrderBookSourceGrpc(
            OrderBookManager manager,
            ILogger<OrderBookSourceGrpc> logger
            )
        {
            _manager = manager;
            _logger = logger;
        }

        public Task<GetNameResult> GetNameAsync(GetOrderBookNameRequest request)
        {
            return Task.FromResult(new GetNameResult() { Name = FtxConst.Name });
        }

        public Task<GetSymbolResponse> GetSymbolsAsync(GetSymbolsRequest request)
        {
            return Task.FromResult(new GetSymbolResponse() {Symbols = _manager.GetSymbols()});
        }

        public Task<HasSymbolResponse> HasSymbolAsync(MarketRequest request)
        {
            return Task.FromResult(new HasSymbolResponse() {Result = _manager.HasSymbol(request.Market)});
        }

        public Task<GetOrderBookResponse> GetOrderBookAsync(MarketRequest request)
        {
            _logger.LogInformation("Receive GetOrderBookAsync {@Request}", request);
            
            var result = _manager.GetOrderBook(request.Market);

            if (result == null)
            {
                _logger.LogWarning("Failed to GetOrderBook, result is null");
            }

            return Task.FromResult(new GetOrderBookResponse {OrderBook = result});
        }
    }
}