using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServerExample
{
    public class Cube : GameObject
    {
        public Cube(GameServer server) : base(2, server)
        {
        }
    }
}
