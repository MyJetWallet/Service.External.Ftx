using System.Threading.Tasks;
using MyJetWallet.Sdk.ExternalMarketsSettings.Grpc;
using MyJetWallet.Sdk.ExternalMarketsSettings.Grpc.Models;
using MyJetWallet.Sdk.ExternalMarketsSettings.Models;
using MyJetWallet.Sdk.ExternalMarketsSettings.Settings;
using Service.External.Ftx.Services;

namespace Service.External.Ftx.GrpcServices
{
    public class ExternalMarketSettingsManagerGrpc : IExternalMarketSettingsManagerGrpc
    {
        private readonly IExternalMarketSettingsAccessor _accessor;
        private readonly OrderBookManager _orderBookManager;

        public ExternalMarketSettingsManagerGrpc(IExternalMarketSettingsAccessor accessor, 
            OrderBookManager orderBookManager)
        {
            _accessor = accessor;
            _orderBookManager = orderBookManager;
        }

        public Task GetExternalMarketSettings(GetMarketRequest request)
        {
            return Task.FromResult(_accessor.GetExternalMarketSettings(request.Symbol));
        }

        public Task<GrpcList<ExternalMarketSettings>> GetExternalMarketSettingsList()
        {
            return Task.FromResult(GrpcList<ExternalMarketSettings>.Create(_accessor.GetExternalMarketSettingsList()));
        }

        public Task AddExternalMarketSettings(ExternalMarketSettings settings)
        {
            return _orderBookManager.Subscribe(settings.Market);
        }

        public Task UpdateExternalMarketSettings(ExternalMarketSettings settings)
        {
            return _orderBookManager.Resubscribe(settings.Market);
        }

        public Task RemoveExternalMarketSettings(RemoveMarketRequest request)
        {
            return _orderBookManager.Unsubscribe(request.Symbol);
        }
    }
}