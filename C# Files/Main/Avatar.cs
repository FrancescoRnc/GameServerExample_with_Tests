using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServerExample
{
    public class DifferentAvatarOwningUse : Exception
    {

    }

    public class Avatar : GameObject
    {
        Packet packet;

        public Avatar(GameServer server) : base(1, server)
        {
        }

        public override void Tick()
        {
            packet = new Packet(3, Id, X, Y, Z);
            packet.OneShot = true;
            server.SendToAllClients(packet);
        }
    }
}
