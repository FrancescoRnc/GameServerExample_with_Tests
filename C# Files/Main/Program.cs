using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServerExample
{
    class Program
    {

        static void Main(string[] args)
        {
            GameTransportIPv4 transport = new GameTransportIPv4();
            transport.Bind("192.168.1.218", 9999);

            GameClock clock = new GameClock();

            GameServer server = new GameServer(transport, null);
            
            server.Run();
        }
    }
}
