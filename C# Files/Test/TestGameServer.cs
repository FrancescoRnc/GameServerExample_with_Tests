using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace GameServerExample.Test
{
    class TestGameServer
    {
        FakeTransport transport;
        FakeClock clock;
        GameServer server;

        [SetUp]
        public void SetupTests()
        {
            transport = new FakeTransport();
            clock = new FakeClock();
            server = new GameServer(transport, clock);
        }

        [Test]
        public void TestGameServerNowIsZero()
        {
            Assert.That(server.Now, Is.EqualTo(0));
        }

        [Test]
        public void TestClientsOnStart()
        {
            Assert.That(server.NumClients, Is.EqualTo(0));
        }

        [Test]
        public void TestGameObjectsOnStart()
        {
            Assert.That(server.NumGameObjects, Is.EqualTo(0));
        }

        [Test]
        public void TestJoinNumOfClients()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();

            Assert.That(server.NumClients, Is.EqualTo(1));
        }

        [Test]
        public void TestJoinNumOfGameObjects()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();

            Assert.That(server.NumGameObjects, Is.EqualTo(1));
        }

        [Test]
        public void TestWelcomeAfterJoin()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();

            FakeData welcome = transport.ClientDequeue();
            Assert.That(welcome.Data[0], Is.EqualTo(1));
        }

        [Test]
        public void TestSpawnAvatarAfterJoin()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();

            transport.ClientDequeue();
            Assert.That(() => transport.ClientDequeue(), Throws.InstanceOf<FakeQueueEmpty>());
        }

        [Test]
        public void TestJoinSameClient()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();

            Assert.That(server.NumClients, Is.EqualTo(1));
        }

        [Test]
        public void TestJoinSameAddressClient()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientEnqueue(packet, "tester", 1);
            server.SingleStep();

            Assert.That(server.NumClients, Is.EqualTo(2));
        }

        [Test]
        public void TestJoinSameAddressAvatar()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientEnqueue(packet, "foobar", 1);
            server.SingleStep();

            Assert.That(server.NumGameObjects, Is.EqualTo(2));
        }

        [Test]
        public void TestJoinTwoClientsWelcome()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientEnqueue(packet, "foobar", 1);
            server.SingleStep();

            Assert.That(transport.ClientQueueCounter, Is.EqualTo(5));
            Assert.That(transport.ClientDequeue().EndPoint.Address, Is.EqualTo("tester"));
            Assert.That(transport.ClientDequeue().EndPoint.Address, Is.EqualTo("tester"));
            Assert.That(transport.ClientDequeue().EndPoint.Address, Is.EqualTo("tester"));
            Assert.That(transport.ClientDequeue().EndPoint.Address, Is.EqualTo("foobar"));
            Assert.That(transport.ClientDequeue().EndPoint.Address, Is.EqualTo("foobar"));
        }

        // a casa, molti test più specifici
        // SENZA LANCIARE IL PROGRAM

        [Test]
        public void TestEvilUpdate()
        {
            Packet packet = new Packet(0);

            FakeEndPoint firstEndPoint = new FakeEndPoint("tester", 0);
            transport.ClientEnqueue(packet, firstEndPoint.Address, firstEndPoint.Port);
            server.SingleStep();

            Cube cube = server.Spawn<Cube>();

            FakeEndPoint secondEndPoint = new FakeEndPoint("foobar", 1);
            transport.ClientEnqueue(packet, secondEndPoint.Address, secondEndPoint.Port);
            server.SingleStep();

            uint firstClientId = server.GetClient(firstEndPoint).Id;
            Assert.That(firstClientId, Is.Not.EqualTo(0));
            uint secondClientId = server.GetClient(secondEndPoint).Id;
            Assert.That(secondClientId, Is.Not.EqualTo(0));

            Packet move = new Packet(3, secondClientId, 1.0f, 1.0f, 2.0f);
            transport.ClientEnqueue(move, "tester", 0);

            Assert.That(() => server.SingleStep(), Throws.InstanceOf<DifferentAvatarOwningUse>());
            // get the id from the welcome packets
            // try to move the id from the other player
        }

        [Test]
        public void TestJoinThreeClientsWelcome()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientEnqueue(packet, "foobar", 1);
            server.SingleStep();
            transport.ClientEnqueue(packet, "toto", 2);
            server.SingleStep();

            Assert.That(transport.ClientQueueCounter, Is.EqualTo(14));
        }

        [Test]
        public void TestJoinFourClientsWelcome()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientEnqueue(packet, "foobar", 1);
            server.SingleStep();
            transport.ClientEnqueue(packet, "toto", 2);
            server.SingleStep();
            transport.ClientEnqueue(packet, "africa", 3);
            server.SingleStep();

            Assert.That(transport.ClientQueueCounter, Is.EqualTo(30));
        }

        // In sostanza, per ogni nuovo client che joina, 
        // vengono inviati i*2 messaggi totali

        [Test]
        public void TestGameObjectIsAlreadyRegistered()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();

            Cube cube = server.Spawn<Cube>();
            
            Assert.That(() => server.RegisterGameObject(cube), Throws.InstanceOf<Exception>());
        }

        [Test]
        public void TestSendSpawnToFirstClientAfterSecondAvatarJoin()
        {
            Packet packet = new Packet(0);
            FakeData firstEntryData = new FakeData()
            { Data = packet.GetData(), EndPoint = new FakeEndPoint("tester", 0) };
            FakeData secondEntryData = new FakeData()
            { Data = packet.GetData(), EndPoint = new FakeEndPoint("foobar", 1) };
                        
            transport.ClientEnqueue(firstEntryData);
            server.SingleStep();
            transport.ClientEnqueue(secondEntryData);
            server.SingleStep();

            FakeData firstData = transport.ClientDequeue();
            FakeData secondData = transport.ClientDequeue();
            FakeData thirdData = transport.ClientDequeue();

            Assert.That(thirdData.Data[0], Is.EqualTo(2));            
            Assert.That(thirdData.EndPoint, Is.EqualTo(firstEntryData.EndPoint));
        }

        [Test]
        public void TestClientMalusOverLimit()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();

            Assert.That(server.NumClients, Is.EqualTo(0));
        }

        [Test]
        public void TestAvatarPositionVectorIsZeroOnRegistration()
        {
            Packet packet = new Packet(0);
            FakeEndPoint endPoint = new FakeEndPoint("tester", 0);
            transport.ClientEnqueue(packet, endPoint.Address, endPoint.Port);
            server.SingleStep();

            GameClient client = server.ClientsTable[endPoint];
            Avatar avatar = server.GetAvatar(client);

            Assert.That(avatar.X, Is.EqualTo(0));
            Assert.That(avatar.Y, Is.EqualTo(0));
            Assert.That(avatar.Z, Is.EqualTo(0));            
        }

        [Test]
        public void TestClientIsAvatarOwner()
        {
            Packet packet = new Packet(0);
            FakeEndPoint endPoint = new FakeEndPoint("tester", 0);
            transport.ClientEnqueue(packet, endPoint.Address, endPoint.Port);
            server.SingleStep();

            GameClient client = server.ClientsTable[endPoint];
            Avatar avatar = server.GetAvatar(client);

            Assert.That(client.Id, Is.EqualTo(avatar.Id));
        }

        [Test]
        public void TestPacketSendAfter()
        {
            FakeEndPoint endPoint = new FakeEndPoint("tester", 0);
            GameClient client = new GameClient(server, endPoint);

            Packet packet = new Packet(1, 1, 1, 0, 0, 0);
            packet.SendAfter = server.Now + 1f;

            client.Enqueue(packet);
            client.Process();

            Assert.That(transport.ClientQueueCounter, Is.EqualTo(0));
        }

        [Test]
        public void TestAckPacketWorks()
        {
            FakeEndPoint endPoint = new FakeEndPoint("tester", 0);
            GameClient client = new GameClient(server, endPoint);

            Packet packet = new Packet(1, 1, 1, 0, 0, 0);
            packet.NeedsAck = true;

            client.Enqueue(packet);
            client.Process();

            Assert.That(client.AckTable, Is.Not.Empty);
        }


    }
}
