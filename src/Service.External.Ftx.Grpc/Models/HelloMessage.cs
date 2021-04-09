using System.Runtime.Serialization;
using Service.External.Ftx.Domain.Models;

namespace Service.External.Ftx.Grpc.Models
{
    [DataContract]
    public class HelloMessage : IHelloMessage
    {
        [DataMember(Order = 1)]
        public string Message { get; set; }
    }
}