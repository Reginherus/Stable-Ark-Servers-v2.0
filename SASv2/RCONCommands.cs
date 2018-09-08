using Rcon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASv2
{
    class RCONCommands
    {
        public static void Connect(ArkServerInfo Server)
        {
            RconBase client = new RconBase();
            client.Connect(Server.IPAddress, Int32.Parse(Server.RCONPort));

            if (client.Connected)
            {
                client.Authenticate(Server.ServerPassword);
                //Console.WriteLine(DateTime.Now + ": Client has been successfully connected!");
                Methods.Log(Server, DateTime.Now + ": Client has been successfully connected!");
            }
            if (!client.Connected)
            {
                //Console.WriteLine(DateTime.Now + ": Client can't connect successfully!");
                Methods.Log(Server, DateTime.Now + ": Client can't connect successfully!");
            }
            client.Disconnect();
        }
        public static bool WorldSave(ArkServerInfo Server)
        {
            RconBase client = new RconBase();
            client.Connect(Server.IPAddress, Int32.Parse(Server.RCONPort));
            if (client.Connected)
            {
                client.Authenticate(Server.ServerPassword);
                RconPacket request = new RconPacket(PacketType.ServerdataExeccommand, new Rcon.Commands.SaveWorld().ToString());
                RconPacket response = client.SendReceive(request);
                string stringResponse = response?.Body.Trim();
                if (stringResponse.Contains("World Saved"))
                {
                    Console.WriteLine(DateTime.Now + ": Server " + Server.Name + "- World Saved!");
                    Methods.Log(Server, DateTime.Now + ": Server " + Server.Name + "- World Saved!");
                    client.Disconnect();
                    return true;
                }
            }
            client.Disconnect();
            return false;
        }
        public static void ShutdownServer(ArkServerInfo Server)
        {
            try
            {
                RconBase client = new RconBase();
                client.Connect(Server.IPAddress, Int32.Parse(Server.RCONPort));
                if (client.Connected)
                {
                    client.Authenticate(Server.ServerPassword);
                    RconPacket request = new RconPacket(PacketType.ServerdataExeccommand, new Rcon.Commands.DoExit().ToString());
                    RconPacket response = client.SendReceive(request);
                    Console.WriteLine(response?.Body.Trim());
                    Methods.Log(Server, response?.Body.Trim());
                }
                client.Disconnect();
            }
            catch (Exception ex)
            {
                //Console.WriteLine(DateTime.Now + ": Exception occured when trying to send a Ark Server Shutdown Command. Exception: " + ex.Message);
                Methods.Log(Server, DateTime.Now + ": Exception occured when trying to send a Ark Server Shutdown Command. Exception: " + ex.Message);
            }

        }
        public static void ServerRestartTriggered(ArkServerInfo Server, string Reason)
        {
            string message = string.Format("Server needs to restart for the following reason: {0}", Reason);
            GlobalNotification(Server, message);
        }
        public static void GlobalNotification(ArkServerInfo Server, string message)
        {
            try
            {
                RconBase client = new RconBase();
                client.Connect(Server.IPAddress, Int32.Parse(Server.RCONPort));
                if (client.Connected)
                {
                    client.Authenticate(Server.ServerPassword);
                    RconPacket request = new RconPacket(PacketType.ServerdataExeccommand, new Rcon.Commands.Broadcast(message).ToString());
                    RconPacket response = client.SendReceive(request);
                    //Console.WriteLine(DateTime.Now + ": Broadcast sent to " + Server.Name + " Server Message: " + message);
                    Methods.Log(Server, DateTime.Now + ": Broadcast sent to " + Server.Name + " Server Message: " + message);
                }

                client.Disconnect();
            }
            catch (Exception ex)
            {
                //Console.WriteLine(DateTime.Now + ": Exception occured when trying to send an Ark Server Global Notification. Exception: " + ex.Message);
                Methods.Log(Server, DateTime.Now + ": Exception occured when trying to send an Ark Server Global Notification. Exception: " + ex.Message);
            }

        }
        public static bool IsServerResponding(ArkServerInfo Server)
        {
            bool serverIsRunning = false;
            RconClient client = new RconClient();
            try
            {
                client.Connect(Server.IPAddress, Int32.Parse(Server.RCONPort), Server.ServerPassword);
                if (client.IsConnected)
                {
                    serverIsRunning = true;
                }
            }
            catch (Exception ex)
            {
                serverIsRunning = false;
                //Console.WriteLine(DateTime.Now + ": " + Server.Name + " Exception:"  + ex.Message);
                Methods.Log(Server,DateTime.Now + ": " + Server.Name + " Exception:" + ex.Message);
            }
            client.Disconnect();


            return serverIsRunning;
        }

    }
}
