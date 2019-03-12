using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GameServerExample
{
    public class GameTransportIPv4 : IGameTransport
    {
        Socket socket;

        public GameTransportIPv4()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Blocking = false;
        }        

        public void Bind(string address, int port)
        {
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(address), port);
            socket.Bind(endpoint);
        }

        public EndPoint CreateEndPoint()
        {
            return new IPEndPoint(0, 0);
        }

        public byte[] Receive(int buffersize, ref EndPoint sender)
        {
            int rlen = -1;
            byte[] data = new byte[buffersize];
            try
            {
                rlen = socket.ReceiveFrom(data, ref sender);
                if (rlen <= 0)
                    return null;
            }
            catch
            {
                return null;
            }
            
            byte[] trueData = new byte[rlen];
            Buffer.BlockCopy(data, 0, trueData, 0, rlen);
            return trueData;
        }

        public bool Send(byte[] data, EndPoint destination)
        {
            bool success = false;
            try
            {
                int rlen = socket.SendTo(data, destination);
                if (rlen == data.Length)
                    success = true;
            }
            catch
            {
                success = false;
            }
            return success;
        }
    }
}
