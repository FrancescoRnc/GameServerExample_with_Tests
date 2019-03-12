using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace GameServerExample
{
    public class GameServer
    {
        delegate void GameCommand(byte[] data, EndPoint sender);
        Dictionary<byte, GameCommand> commandsTable;
        Dictionary<EndPoint, GameClient> clientsTable;
        public Dictionary<EndPoint, GameClient> ClientsTable { get { return clientsTable; } }
        Dictionary<uint, GameObject> gameobjectsTable;

        IGameTransport transport;
        IMonotonicClock clock;
        
        float currentTime;
        public float Now { get { return currentTime; } }

        public int NumClients { get { return clientsTable.Count; } }
        public int NumGameObjects { get { return gameobjectsTable.Count; } }

        public uint MaxTollerableMalus;

        List<EndPoint> clientsToRemove;

        public GameServer(IGameTransport transport, IMonotonicClock clock)
        {
            this.transport = transport;
            this.clock = clock;

            MaxTollerableMalus = 3;

            clientsTable = new Dictionary<EndPoint, GameClient>();
            gameobjectsTable = new Dictionary<uint, GameObject>();

            clientsToRemove = new List<EndPoint>();

            /* DATI DEI COMANDI:
             * Join: 0;
             * Welcome : 1, ObjType, Id, X, Y, Z;
             * Spawn   : 2, ObjType, Id, X, Y, Z;
             * Update  : 3, Id, X, Y, Z;
             * Ack     : 255, Id;
            */

            commandsTable = new Dictionary<byte, GameCommand>();
            commandsTable[0] = Join;
            commandsTable[3] = Update;
            commandsTable[255] = Ack;
        }

        public void Run()
        {
            Console.WriteLine("Server started");

            while (true)
            {
                SingleStep();
            }
        }

        public void SingleStep()
        {
            TimeTick();

            CommandsManagement();

            CheckDeadClients();

            //server tick
            ClientsTick();
            GameObjectsTick();
        }

        public void CommandsManagement()
        {
            EndPoint sender = transport.CreateEndPoint();
            byte[] data = transport.Receive(256, ref sender);
            if (data != null)
            {
                byte command = data[0];
                if (commandsTable.ContainsKey(command))
                {
                    commandsTable[command](data, sender);
                }
            }
        }

        public void CheckDeadClients()
        {
            // check for dead clients or with high malus
            foreach (EndPoint end in clientsTable.Keys)
            {
                if (clientsTable[end].IsDead || clientsTable[end].Malus >= MaxTollerableMalus)
                {
                    clientsToRemove.Add(end);
                }
            }

            foreach (EndPoint end in clientsToRemove)
            {
                clientsTable.Remove(end);
                Console.WriteLine("client {0} is dead", end);
            }

            clientsToRemove.Clear();
        }

        public void ClientsTick()
        {            
            foreach (GameClient client in clientsTable.Values)
            {
                client.Process();
            }
            
        }
        public void GameObjectsTick()
        {
            foreach (GameObject obj in gameobjectsTable.Values)
            {
                obj.Tick();
            }
        }

        void Join(byte[] data, EndPoint sender)
        {
            // check if the client has already joined
            if (clientsTable.ContainsKey(sender))
            {
                clientsTable[sender].Malus++;
                return;
            }
            GameClient client = new GameClient(this, sender);
            clientsTable[sender] = client;
            Avatar avatar = Spawn<Avatar>();

            // ------------------------------
            // Oppure:
            // Avatar avatar = new Avatar(this);
            // RegisterGameObject(avatar);
            // ------------------------------

            avatar.SetOwner(client);

            Packet welcomePacket = new Packet(1, avatar.ObjectType, avatar.Id, avatar.X, avatar.Y, avatar.Z);
            welcomePacket.NeedsAck = true;
            client.Enqueue(welcomePacket);
            client.SetId(avatar.Id);

            foreach (GameObject gameobject in gameobjectsTable.Values)
            {
                if (gameobject == avatar)
                    continue;
                Packet spawnPacket = new Packet(2, gameobject.ObjectType, gameobject.Id, gameobject.X, gameobject.Y, gameobject.Z);
                spawnPacket.NeedsAck = true;
                client.Enqueue(spawnPacket);
            }

            Packet newClientSpawned = new Packet(2, avatar.ObjectType, avatar.Id, avatar.X, avatar.Y, avatar.Z);
            newClientSpawned.NeedsAck = true;
            SendToAllClientsExceptOne(newClientSpawned, client);

            Console.WriteLine("client {0} has joined with avatar {1}", client, avatar.Id);
        }

        void Update(byte[] data, EndPoint sender)
        {
            if (!clientsTable.ContainsKey(sender))
                return;
            GameClient client = clientsTable[sender];
            uint netId = BitConverter.ToUInt32(data, 1);
            if (GetAvatar(client).Id != netId)
                throw new DifferentAvatarOwningUse();

            if (gameobjectsTable.ContainsKey(netId))
            {
                GameObject gameobject = gameobjectsTable[netId];
                if (gameobject.IsOwnedBy(client))
                {
                    gameobject.SetPosition(BitConverter.ToSingle(data, 5), 
                                           BitConverter.ToSingle(data, 9), 
                                           BitConverter.ToSingle(data, 13));
                }
            }
            client.MarkAsAlive();
        }

        void Ack(byte[] data, EndPoint sender)
        {
            if (!clientsTable.ContainsKey(sender))
                return;
            GameClient client = clientsTable[sender];
            uint packetId = BitConverter.ToUInt32(data, 1);
            client.Ack(packetId);
        }

        public void RegisterGameObject(GameObject obj)
        {
            if (gameobjectsTable.ContainsKey(obj.Id))
                throw new Exception("GameObject already registered");
            gameobjectsTable[obj.Id] = obj;
        }

        public T Spawn<T>() where T : GameObject
        {
            object[] ctorParams = { this };
            T newGameObj = Activator.CreateInstance(typeof(T), ctorParams) as T;
            RegisterGameObject(newGameObj);
            return newGameObj;
        }

        public bool Send(Packet packet, EndPoint endPoint)
        {
            return transport.Send(packet.GetData(), endPoint);
        }

        public void TimeTick()
        {
            currentTime = clock.GetNow();
        }

        public void SendToAllClients(Packet packet)
        {
            foreach (GameClient client in clientsTable.Values)
            {
                client.Enqueue(packet);
            }
        }

        public void SendToAllClientsExceptOne(Packet packet, GameClient except)
        {
            foreach (GameClient client in clientsTable.Values)
            {
                if (client != except)
                    client.Enqueue(packet);
            }
        }

        public GameObject GetGameObject(uint id)
        {
            return gameobjectsTable[id];
        }

        public Avatar GetAvatar(uint id)
        {
            if (gameobjectsTable[id] is Avatar)
                return gameobjectsTable[id] as Avatar;
            return null;
        }
        public Avatar GetAvatar(GameClient client)
        {
            Avatar avatar = null;

            foreach (GameObject go in gameobjectsTable.Values)
            {
                if (go is Avatar && go.IsOwnedBy(client))
                    return go as Avatar;                
            }
            return avatar;
        }

        public GameClient GetClient(EndPoint endPoint)
        {
            if (clientsTable.ContainsKey(endPoint))
                return clientsTable[endPoint];
            return null;
        }

        public uint GetClientId(EndPoint endPoint)
        {
            if (clientsTable.ContainsKey(endPoint))
                return clientsTable[endPoint].Id;
            return 0;
        }
    }
}
