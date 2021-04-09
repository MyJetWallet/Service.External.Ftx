using Autofac;
using Service.External.Ftx.Grpc;

// ReSharper disable UnusedMember.Global

namespace Service.External.Ftx.Client
{
    public static class AutofacHelper
    {
        public static void RegisterExternalFtxClient(this ContainerBuilder builder, string grpcServiceUrl)
        {
            var factory = new ExternalFtxClientFactory(grpcServiceUrl);

            builder.RegisterInstance(factory.GetHelloService()).As<IHelloService>().SingleInstance();
        }
    }
}
