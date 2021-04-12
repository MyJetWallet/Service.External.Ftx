using Autofac;
using FtxApi;
using MyJetWallet.Connector.Ftx.Rest;
using Service.External.Ftx.Services;

namespace Service.External.Ftx.Modules
{
    public class ServiceModule: Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            FtxRestApi ftxRestClient = FtxRestApiFactory.CreateClient(Program.Settings.ApiKey, Program.Settings.ApiSecret);
            builder.RegisterInstance(ftxRestClient).AsSelf().SingleInstance();

            builder.RegisterType<BalanceCache>().As<IStartable>().AutoActivate().AsSelf().SingleInstance();
            builder.RegisterType<MarketInfoData>().As<IStartable>().AutoActivate().AsSelf().SingleInstance();
            builder.RegisterType<OrderBookManager>().AsSelf().SingleInstance();
        }
    }
}