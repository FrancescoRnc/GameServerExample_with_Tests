using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServerExample
{
    public abstract class GameObject
    {
        static uint gameobjectCounter;
        

        public float X;
        public float Y;
        public float Z;

        protected GameServer server;
        GameClient owner;

        uint internalId;
        public uint Id { get { return internalId; } }

        uint internalObjectType;
        public uint ObjectType { get { return internalObjectType; } }

        public GameObject(uint objectType, GameServer server)
        {
            this.server = server;

            internalObjectType = objectType;
            internalId = ++gameobjectCounter;
            Console.WriteLine("spawned GameObject {0} of type {1}", Id, ObjectType);
        }

        public void Register(GameServer server)
        { this.server = server; }

        public bool IsOwnedBy(GameClient client)
        {
            return client == owner;
        }
        public void SetOwner(GameClient client)
        {
            owner = client;
        }       
        
        public void SetPosition(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public virtual void Tick()
        {

        }

    }
}
