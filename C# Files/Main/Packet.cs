using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace GameServerExample
{
    public class Packet
    {
        MemoryStream stream;
        BinaryWriter writer;

        static uint packetCounter;

        //GameServer server;

        uint id;
        public uint Id { get { return id; } }

        int attempts;
        public int Attempts { get { return attempts; } }

        float expires;
        

        public bool NeedsAck;

        public float SendAfter;

        public bool OneShot;

        public Packet()
        {
            stream = new MemoryStream();
            writer = new BinaryWriter(stream);
            id = ++packetCounter;
            attempts = 0;
            OneShot = false;
            SendAfter = 0;
        }

        //params: il packet può ricevere un numero variante di elementi
        public Packet(byte commandNumber, params object[] elements) : this() 
        {
            //this.server = server;

            writer.Write(commandNumber);
            foreach(object element in elements) // foreach(var element in elements)
            {
                if (element is byte)
                {
                    writer.Write((byte)element);
                }
                else if (element is float)
                {
                    writer.Write((float)element);
                }
                else if (element is int)
                {
                    writer.Write((int)element);
                }
                else if (element is uint)
                {
                    writer.Write((uint)element);
                }
                else if (element is char)
                {
                    writer.Write((char)element);
                }
                else
                {
                    throw new Exception("unknown type");
                }
            }
        }

        public void Write(byte[] data)
        {
            writer.Write(data);
        }

        public byte[] GetData()
        {
            return stream.ToArray();
        }

        public void IncreaseAttempts()
        {
            attempts++;
        }

        public bool IsExpired(float now)
        {
            return expires > now;
        }

        public void SetExpire(float death)
        {
            expires = death;
        }
    }
}
