using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace GameServerExample
{
    public class GameClient
    {
        GameServer server;
        EndPoint endPoint;
        Queue<Packet> sendQueue;
        Dictionary<uint, Packet> ackTable;
        public Dictionary<uint, Packet> AckTable { get { return ackTable; } }

        // check ack table
        List<uint> packetsToRemove = new List<uint>();

        float lastUpdate;

        uint id;
        public uint Id { get { return id; } }

        public uint Malus;
        public bool IsDead { get { return server.Now - lastUpdate > 30; } }

        public GameClient(GameServer server, EndPoint endPoint)
        {
            this.server = server;
            this.endPoint = endPoint;
            sendQueue = new Queue<Packet>();
            ackTable = new Dictionary<uint, Packet>();
            Malus = 0;
        }

        public void MarkAsAlive()
        {
            lastUpdate = server.Now;
        }

        public void Process()
        {
            int packetsInQueue = sendQueue.Count;
            for (int i = 0; i < packetsInQueue; i++)
            {
                Packet packet = sendQueue.Dequeue();
                // check if the packet can be sent
                if (server.Now >= packet.SendAfter)
                {
                    packet.IncreaseAttempts();
                    if (server.Send(packet, endPoint))
                    {
                        // all fine
                        if (packet.NeedsAck)
                        {
                            ackTable[packet.Id] = packet;
                        }
                    }
                    // on error, retry sending only if NOT OneShot
                    else if (!packet.OneShot)
                    {
                        if (packet.Attempts < 3)
                        {
                            // retry sending after 1 second
                            packet.SendAfter = server.Now + 1.0f;
                            sendQueue.Enqueue(packet);
                        }
                    }
                }
                else
                {
                    // it is too early, re-enqueue the packet
                    sendQueue.Enqueue(packet);
                }
            }
            
            foreach (uint key in ackTable.Keys)
            {
                Packet packet = ackTable[key];
                if (packet.IsExpired(server.Now))
                {
                    //packetsToRemove.Add(key);
                    if (packet.Attempts < 3)
                    {
                        sendQueue.Enqueue(packet);
                    }
                    else
                    {
                        packetsToRemove.Add(key);
                    }
                }
            }
            foreach(uint packetId in packetsToRemove)
            {
                ackTable.Remove(packetId);
            }
        }

        public void Ack(uint id)
        {
            if (ackTable.ContainsKey(id))
            {
                ackTable.Remove(id);
            }
            else
            {
                Malus++;
            }
        }

        public void Enqueue(Packet packet)
        {
            sendQueue.Enqueue(packet);
        }

        public override string ToString()
        {
            return endPoint.ToString();
        }

        public void SetId(uint id)
        {
            this.id = id;
        }
    }
}
