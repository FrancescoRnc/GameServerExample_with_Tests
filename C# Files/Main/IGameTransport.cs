using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace GameServerExample
{
    public interface IGameTransport
    {
        void Bind(string address, int port);
        byte[] Receive(int buffersize, ref EndPoint sender);
        bool Send(byte[] data, EndPoint destination);
        EndPoint CreateEndPoint();
    }
}
