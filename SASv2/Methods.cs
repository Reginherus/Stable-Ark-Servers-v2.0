using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SASv2
{
    class Methods
    {
        
        public static void StartServerProcedure(ArkServerInfo Server)
        {
            if(!GlobalVariables.IsProcessOpen(Server))
            {
                Task startServer = Task.Factory.StartNew(() => ServerStartCommand(Server));
                startServer.Wait(2000);
            }
            
        }
        public static void ServerStartCommand(ArkServerInfo Server)
        {
            Process startServer = new Process();
            startServer.StartInfo.FileName = Server.RunBatFile;
            startServer.StartInfo.CreateNoWindow = false;

            startServer.Start();

            //Start a boot timer to make sure server boots properly. If not, close app and restart every 15 minutes until successful.
            System.Timers.Timer bootTimer = new System.Timers.Timer();
            bootTimer.Interval = 20000;
            bootTimer.Elapsed += (sender, e) => HasServerBooted(sender, e, Server);

            bootTimer.Start();

        }
        private static void HasServerBooted(object sender, EventArgs e, ArkServerInfo Server)
        {
            if (GlobalVariables.IsProcessOpen(Server))
            {
                if (RCONCommands.IsServerResponding(Server))
                {
                    Server.nNotRunning = 0;
                        
                    //StableServer.nAbEventsTriggered = 1;
                    Server.nEventsProcessOpenNotResponding = 0;
                    Server.stopServerTimer = false;
                    if(File.Exists(Server.UpdatedWorkshopACF))
                    {
                        File.Delete(Server.CurrentWorkshopACF);
                        File.Copy(Server.UpdatedWorkshopACF, Server.CurrentWorkshopACF);
                    }
                    else
                    {
                        File.Copy(Server.UpdatedWorkshopACF, Server.CurrentWorkshopACF);
                    }

                    ((System.Timers.Timer)sender).Close();

                }
                if (Server.nNotRunning == 45)
                {
                    //Console.WriteLine(DateTime.Now + ": Killing Server " + Server.Name + " due to boot time exceeding " + Server.nNotRunning * ((System.Timers.Timer)sender).Interval / 60000 + " minutes.");
                    Log(Server, DateTime.Now + ": Killing Server " + Server.Name + " due to boot time exceeding " + Server.nNotRunning * ((System.Timers.Timer)sender).Interval / 60000 + " minutes.");
                    //kill process and restart it.
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
                                    ((System.Timers.Timer)sender).Close();
                                    Thread.Sleep(2000);
                                    StartServerProcedure(Server);
                                }

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        //Console.WriteLine(DateTime.Now + ": Exception occured when killing ShooterGameServer.exe. Exception: " + ex.Message);
                        Log(Server, DateTime.Now + ": Exception occured when killing ShooterGameServer.exe. Exception: " + ex.Message);
                    }
                    //Reset Events not running
                    Server.nNotRunning = 0;
                }
                Server.nNotRunning++;
            }
        }
        public static void ArkUpdateShutdownProcedure(ArkServerInfo Server)
        {
            if (RCONCommands.WorldSave(Server))
            {
                Task stopServer = Task.Factory.StartNew(() => StableServer_Main.killServer(Server));
                stopServer.Wait(2000);
                
            }
            Thread.Sleep(2000);
            Processes.ServerUpdate(Server);
        }
        public static void ModUpdateShutdownProcedure(ArkServerInfo Server)
        {
            if (RCONCommands.WorldSave(Server))
            {
                Task stopServer = Task.Factory.StartNew(() => StableServer_Main.killServer(Server));
                stopServer.Wait(2000);
            }
            Thread.Sleep(2000);
            Server.ModUpdateNeeded = false;
            StartServerProcedure(Server);
        }
        public static void BackupServerFiles(ArkServerInfo Server)
        {
            List<string> listOfBackups = new List<string>();
            string SourcePath = Server.SavedGame;
            string DateStamp = "Backup_" + DateTime.Now.ToString("MMddyyyy_hhmmss");
            string DestinationPath = Server.BackupGames;

            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(SourcePath, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(SourcePath, DestinationPath + DateStamp));
            if(!Directory.Exists(DestinationPath))
            {
                Directory.CreateDirectory(DestinationPath);
            }
            string[] fileEntries = Directory.GetDirectories(DestinationPath);
            foreach (string fileName in fileEntries)
                listOfBackups.Add(fileName);
            Array.Clear(fileEntries, 0, fileEntries.Length);

            //Copy all the files & Replaces any files with the same name
            try
            {
                Directory.CreateDirectory(DestinationPath + "\\" + DateStamp);
                foreach (string newPath in Directory.GetFiles(SourcePath, "*.*",
                SearchOption.AllDirectories).Where(name => !name.Contains("SaveGames")))
                    File.Copy(newPath, newPath.Replace(SourcePath, DestinationPath +"\\" + DateStamp), true);

                Log(Server, DateTime.Now + "Server " + Server.Name + ": Files copied to backup");

            }
            catch (Exception ex)
            {
                //Console.WriteLine(DateTime.Now + ": Could not save back up since: " + ex.Message);
                Log(Server, DateTime.Now + ": Could not save back up since: " + ex.Message);
            }
            while(Directory.GetDirectories(DestinationPath).Length > 10)
            {
                FileSystemInfo fileInfo = new DirectoryInfo(DestinationPath).GetFileSystemInfos().OrderByDescending(fi => fi.CreationTime).Last();
                Directory.Delete(fileInfo.FullName, true);
            }
                
        }
        public static void Log(ArkServerInfo Server, string message)
        {
            string filepath = Server.SASFile;
            if(Directory.Exists(Server.SASLogs))
            {
                if (!File.Exists(filepath)) { using (FileStream fs = File.Create(filepath)) { } }
                if (File.Exists(filepath))  { using (System.IO.StreamWriter file = new StreamWriter(filepath,true)) {
                        file.WriteLine(message);
                    }
                }
            }
            else
            {
                Directory.CreateDirectory(Server.SASLogs);
                if (!File.Exists(filepath))  { using (FileStream fs = File.Create(filepath)) {
                    }
                }
                if (File.Exists(filepath)) { using (System.IO.StreamWriter file = new StreamWriter(filepath, true)) {
                        file.WriteLine(message);
                    }
                }
            }
        }
    }
}
