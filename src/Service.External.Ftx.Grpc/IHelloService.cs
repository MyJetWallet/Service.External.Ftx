using System.ServiceModel;
using System.Threading.Tasks;
using Service.External.Ftx.Grpc.Models;

namespace Service.External.Ftx.Grpc
{
    [ServiceContract]
    public interface IHelloService
    {
        [OperationContract]
        Task<HelloMessage> SayHelloAsync(HelloRequest request);
    }
}