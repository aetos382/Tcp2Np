using System.Net;

namespace Tcp2Np;

#nullable disable

internal class ProxyServiceOptions
{
    public IPEndPoint Endpoint { get; set; }

    public string PipeName { get; set; }
}