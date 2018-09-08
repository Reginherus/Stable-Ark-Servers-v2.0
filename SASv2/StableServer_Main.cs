using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using IniParser;
using IniParser.Model;
using System.Net.Sockets;
using System.Net;
using HtmlAgilityPack;

namespace SASv2
{
    class StableServer_Main
    {
        static int numOfServers;
        static List<ArkServerInfo> Servers = new List<ArkServerInfo>();
        private static int nRunningEventsTriggered = 1;
        private static System.Timers.Timer primaryTimer;

        static void Main(string[] args)
        {
            Task startServer = Task.Factory.StartNew(() => Init());
            startServer.Wait();
            if (startServer.IsCompleted)
            {
                Console.WriteLine(DateTime.Now + ": Initialization process completed.");
            }
                

            //While readline doesn't equal exit, keep the application open.
            string response = "";
            while (response != "Exit")
            {
                Console.Write(DateTime.Now + ": Ready for command: ");
                response = Console.ReadLine();
                if (response == "stop global timer")
                {
                    primaryTimer.Stop();
                    Console.WriteLine(DateTime.Now + ": Stopping global timer.");
                }
                if (response == "start global timer" && !primaryTimer.Enabled)
                {
                    primaryTimer.Start();
                    Console.WriteLine(DateTime.Now + ": Starting global timer.");
                }
                if(response.Contains("check"))
                {
                    if(response.Contains("timer"))
                    {
                        Console.Write(DateTime.Now + ": Timer is ");
                        if(primaryTimer.Enabled == true)
                        {
                            Console.WriteLine("running.");
                        }
                        else
                        {
                            Console.WriteLine("stopped");
                        }
                    }
                }
                if (response.Contains("restart"))
                {
                    foreach(var Server in Servers)
                    {
                        if(response.Contains(Server.Name))
                        {
                            Console.WriteLine(DateTime.Now + ": Restart triggered by admin for " + Server.Name);
                            RestartProcedures.BroadCastModUpdateRestartTimer(Server, "Admin Triggered a restart");
                        }
                    }
                }
                if (response.Contains("update"))
                {
                    foreach(var Server in Servers)
                    {
                        if(response.Contains(Server.Name))
                        {
                            Console.WriteLine(DateTime.Now + ": Update triggered by admin for " + Server.Name);
                            RestartProcedures.BroadCastArkUpdateRestartTimer(Server, "Admin triggered restart to update server.");
                        }
                    }
                }
                if (response.ToLower().Contains("bm all:"))
                {
                    try
                    {
                        string trimResponse = response.Substring(response.IndexOf(':') + 1);
                        foreach(var Server in Servers)
                        {
                            Console.WriteLine(DateTime.Now + ": Message sent to " + Server.Name + " - " + trimResponse);
                            RCONCommands.GlobalNotification(Server, trimResponse);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Does not contain : character. Exception: " + ex.Message);
                    }

                }
                if (response == "Exit")
                {
                    primaryTimer.Stop();
                    Environment.Exit(0);
                }
            }
                
        }
        private static void Init()
        {
            Console.WriteLine("Load server info from file?");
            if (Console.ReadLine() == "y")
            {
                LoadServersFromFile();
                Console.WriteLine("Read file");
            }
            else
            {
                ConfigureServerInfoFile();
            }
            foreach(var Server in Servers)
            {
                
                if(Server.isProcessOpen)
                {
                    if (File.Exists(Server.CurrentWorkshopACF))
                        File.Delete(Server.CurrentWorkshopACF);
                    File.Copy(Server.UpdatedWorkshopACF, Server.CurrentWorkshopACF);
                }
                if (!File.Exists(Server.CurrentWorkshopACF))
                    File.Copy(Server.UpdatedWorkshopACF, Server.CurrentWorkshopACF);
                DirectoryInfo dirInfo = new DirectoryInfo(Server.SASLogs);
                FileInfo[] Files = dirInfo.GetFiles("*" + "SASLog" + "*.*").OrderBy(p => p.CreationTime).ToArray();
                foreach(var file in Files)
                {
                    while (Directory.GetFiles(Server.SASLogs).Where(fileName => fileName.Contains("SASLog")).Count() > 10)
                    {
                        file.Delete();
                    }
                }
            }
            

            //Start primary operating timer.
            primaryTimer = new System.Timers.Timer();
            primaryTimer.Interval = 60000;
            primaryTimer.Elapsed += (sender, e) => CheckRoutine(sender, e);
            primaryTimer.Start();
        }
        private static void CheckRoutine(object sender, EventArgs e)
        {
            
            //Run Server check
            foreach (var Server in Servers)
            {
                //Log that server is currently updating game files
                if(Server.CurrentlyUpdating)
                {
                    //Console.WriteLine(DateTime.Now + ": " + Server.Name + " is currently updating.");
                    Methods.Log(Server, DateTime.Now + ": " + Server.Name + " is currently updating.");
                }
                //If any event was triggered, or if the server is updating, do not run check routines.
                 if (!Server.stopServerTimer && !Server.CurrentlyUpdating)
                {
                    //Console.WriteLine(DateTime.Now + ": Entered " + Server.Name + "'s check method. RunningTimer = " + nRunningEventsTriggered);
                    Methods.Log(Server, DateTime.Now + ": Entered " + Server.Name + "'s check method. RunningTimer = " + nRunningEventsTriggered);
                    bool processOpen = GlobalVariables.IsProcessOpen(Server);
                    bool ServerIsResponding = RCONCommands.IsServerResponding(Server);
                    
                    //Console.WriteLine("Server Response: " + ServerIsResponding);
                    Methods.Log(Server, "Server Response: " + ServerIsResponding);
                    Server.ModUpdateNeeded = GlobalVariables.NeedsModUpdate(Server);
                    
                    //Console.WriteLine("Mods are up to date: " + !Server.ModUpdateNeeded);
                    Methods.Log(Server,DateTime.Now + ": Mods are up to date: " + !Server.ModUpdateNeeded);
                    Server.GameUpdateNeeded = GlobalVariables.NeedsArkUpdate(Server);
                    
                    //Console.WriteLine("Game is up to date: " + !Server.GameUpdateNeeded);
                    Methods.Log(Server,DateTime.Now + ": Game are up to date: " + !Server.GameUpdateNeeded);

                    if (Server.ModUpdateNeeded && processOpen)
                    {
                        //Restart and Update the server if update is needed.
                        RestartProcedures.BroadCastArkUpdateRestartTimer(Server, "Mod Update: " + Server.theModNeedingUpdate);
                        Server.ModUpdateNeeded = false;
                        Server.stopServerTimer = true;
                    }
                    //If process is open and game needs to be updated, trigger restart counter
                    if (Server.GameUpdateNeeded && processOpen)
                    {
                        RestartProcedures.BroadCastArkUpdateRestartTimer(Server, "Ark Update");
                        Server.GameUpdateNeeded = false;
                        Server.stopServerTimer = true;
                    }
                    if (processOpen && !ServerIsResponding)
                    {
                        if (Server.nEventsProcessOpenNotResponding == 10)
                        {
                            RestartUnresponsive(Server);
                        }
                        if(!Server.CurrentlyUpdating)
                        Server.nEventsProcessOpenNotResponding++;
                    }
                    if(nRunningEventsTriggered % 60 == 0)
                    {
                        string message = "25 TC Reward points for recruiting players. See Dicord for more information.";
                        Console.WriteLine(DateTime.Now + ": Message sent to " + Server.Name + " - " + message);
                        RCONCommands.GlobalNotification(Server, message);
                    }
                    //Run Game Version Update every 20 minutes
                    if (nRunningEventsTriggered % 20 == 0)
                    {
                        Processes.ModVersionUpdate(Server);
                    }
                    //If process is not running, AND timer is not stopped and server is not update then trigger the start routine.
                    if (!processOpen)
                    {
                        Methods.StartServerProcedure(Server);
                    }
                }
                //If the server has ran for 12 hours, trigger the routine maintenance restart.
                //if (nRagEventsTriggered % GlobalVariables.timeIntervalMaintenanceRestart == 0 && RagProcessOpen)
                //{
                //    ServerRestartForModORRoutineMaintenance(Ragnarok, "Routine Maintenance");
                //    RagnarokTimerTriggerStopped = true;
                //}
                //Backup the server every 2 hours.
                if (nRunningEventsTriggered % 30 == 0)
                {
                    Methods.BackupServerFiles(Server);
                }
            }
            nRunningEventsTriggered++;
        }
        public static void LoadServersFromFile()
        {
            foreach (var Server in ServersArrayPreSplit())
            {
                ArkServerInfo tempServer = new ArkServerInfo();
                string[] serverInfo = { "" };
                serverInfo = Server.Split(',');
                tempServer.Name = serverInfo[0];
                tempServer.ServerDir = serverInfo[1];
                Servers.Add(tempServer);

                if (!File.Exists(tempServer.CurrentWorkshopACF))
                {
                    File.Copy(tempServer.UpdatedWorkshopACF, tempServer.CurrentWorkshopACF);
                }
                string sasFile = tempServer.SASLogs + "/SASLog" + DateTime.Now.ToString("MMddyyyy_hhmmss") + ".txt";
                if (!File.Exists(sasFile))
                {
                    tempServer.SASFile = sasFile;
                    var myFile = File.Create(sasFile);
                    myFile.Close();
                }
            }
            foreach (var Server in Servers)
            {
                Methods.Log(Server, "Build Version: " + VersionCheck.GetGameInformation(376030));
                Methods.Log(Server, "Game Version: " + VersionCheck.GetGameBuildID(Server));
                Processes.ModVersionUpdate(Server);
            }
        }
        public static void ConfigureServerInfoFile()
        {
            Console.WriteLine("How many servers do you want to add?");
            Int32.TryParse(Console.ReadLine(), out numOfServers);

            File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ServerDirectories.txt"));

            //Add server to list
            for (var i = 0; i < numOfServers; i++)
            {
                ArkServerInfo tempServer = new ArkServerInfo();
                Console.WriteLine("Server Name: ");
                tempServer.Name = Console.ReadLine();
                //tempServer.IPAddress = ipAddress;
                string shooterGameDir;
                Console.WriteLine("Paste directory to shootergame.exe");
                shooterGameDir = Console.ReadLine();

                while (!Directory.Exists(shooterGameDir))
                {
                    Console.WriteLine("Directory does not exist, try again or exit: ");
                    shooterGameDir = Console.ReadLine();
                    if (shooterGameDir.Contains("exit"))
                        Environment.Exit(0);
                }
                tempServer.ServerDir = shooterGameDir + "\\";
                File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ServerDirectories.txt"), tempServer.Name + "," + tempServer.ServerDir + Environment.NewLine);

                Servers.Add(tempServer);

                if (!File.Exists(tempServer.CurrentWorkshopACF))
                {
                    File.Copy(tempServer.UpdatedWorkshopACF, tempServer.CurrentWorkshopACF);
                }
                string sasFile = tempServer.SASLogs + "/SASLog" + DateTime.Now.ToString("MMddyyyy_hhmmss") + ".txt";
                if (!File.Exists(sasFile))
                {
                    tempServer.SASFile = sasFile;
                    var myFile = File.Create(sasFile);
                    myFile.Close();
                }
            }
        }
        public static void RestartUnresponsive(ArkServerInfo Server)
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(GlobalVariables.appName);
                foreach (Process clsProcess in processes)
                {
                    if (clsProcess.ProcessName.Contains(GlobalVariables.appName))
                    {
                        if (clsProcess.MainModule.FileName.Contains(Server.ServerDir))
                        {
                            clsProcess.Kill();
                            Processes.ServerUpdateWValidate(Server);
                            Server.stopServerTimer = true;

                        }

                    }
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine(DateTime.Now + ": Exception occured when killing ShooterGameServer.exe. Exception: " + ex.Message);
                Methods.Log(Server, DateTime.Now + ": Exception occured when killing ShooterGameServer.exe. Exception: " + ex.Message);

            }
        }
        public static void killServer(ArkServerInfo Server)
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(GlobalVariables.appName);
                foreach (Process clsProcess in processes)
                {
                    if (clsProcess.ProcessName.Contains(GlobalVariables.appName))
                    {
                        if (clsProcess.MainModule.FileName.Contains(Server.ServerDir))
                        {
                            clsProcess.Kill();
                            Processes.ServerUpdateWValidate(Server);
                            Server.stopServerTimer = true;

                        }

                    }
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine(DateTime.Now + ": Exception occured when killing ShooterGameServer.exe. Exception: " + ex.Message);
                Methods.Log(Server, DateTime.Now + ": Exception occured when killing ShooterGameServer.exe. Exception: " + ex.Message);

            }
        }
        private static string[] ServersArrayPreSplit()
        {
            return File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ServerDirectories.txt"));
        }
    }
}
