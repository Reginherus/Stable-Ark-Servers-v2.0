using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASv2
{
    class Processes
    {
        public static void ModVersionUpdate(ArkServerInfo Server)
        {

            Methods.Log(Server, DateTime.Now + ": Updating Mod Versions for server " + Server.Name);

            string command = Server.SteamCMDDir + "\\SteamCMD.exe"+ " +login anonymous +force_install_dir " + Server.SteamCMDDir;
            foreach (string mod in GlobalVariables.ActiveServerMods(Server))
            {
                command = command + " +workshop_download_item 346110 " + mod;
            }

            command = command + " +quit";

            Process steamUpdateServer = new Process();
            steamUpdateServer.StartInfo.UseShellExecute = false;
            steamUpdateServer.StartInfo.RedirectStandardInput = true;
            steamUpdateServer.StartInfo.RedirectStandardOutput = true;
            //steamUpdateServer.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            steamUpdateServer.EnableRaisingEvents = true;
            steamUpdateServer.StartInfo.FileName = "cmd.exe";
            steamUpdateServer.Exited += (sender, e) =>
            {
                //Console.WriteLine(DateTime.Now + ": Mod Version Update completed for " + Server.GamePort + "    Time: {0} sec " +
                //"Exit code:    {1}", DateTime.Now.Second - steamUpdateServer.StartTime.Second, steamUpdateServer.ExitCode);
                Methods.Log(Server, DateTime.Now + ": Process Complete for " + Server.Name + " Time: " + (DateTime.Now.Second - steamUpdateServer.StartTime.Second) + " sec " +
                "Exit code: " + steamUpdateServer.ExitCode);

                ((Process)sender).Dispose();
            };
            steamUpdateServer.Start();
            steamUpdateServer.StandardInput.WriteLine(command);
            steamUpdateServer.StandardOutput.ReadToEndAsync();
            steamUpdateServer.StandardInput.WriteLine("exit");
        }
        public static void ServerUpdate(ArkServerInfo Server)
        {

            Process steamUpdateServer = new Process();
            steamUpdateServer.StartInfo.UseShellExecute = false;
            steamUpdateServer.StartInfo.RedirectStandardInput = true;
            steamUpdateServer.StartInfo.RedirectStandardOutput = true;
            //steamUpdateServer.StartInfo.CreateNoWindow = false;
            //steamUpdateServer.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            steamUpdateServer.EnableRaisingEvents = true;
            steamUpdateServer.StartInfo.FileName = "cmd.exe";
            steamUpdateServer.Exited += (sender, e) =>
            {
                //Console.WriteLine(DateTime.Now + ": Process Complete for " + Server.Name + "    Time: {0} sec " +
                //"Exit code:    {1}", DateTime.Now.Second - steamUpdateServer.StartTime.Second, steamUpdateServer.ExitCode);
                Methods.Log(Server, DateTime.Now + ": Process Complete for " + Server.Name + " Time: " + (DateTime.Now.Second - steamUpdateServer.StartTime.Second) + " sec " +
                "Exit code: " + steamUpdateServer.ExitCode);

                Server.CurrentlyUpdating = false;

                if (!Server.stopServerTimer)
                {
                    
                    Server.GameUpdateNeeded = GlobalVariables.NeedsArkUpdate(Server);
                    Server.ModUpdateNeeded = GlobalVariables.NeedsModUpdate(Server);

                }
                Methods.StartServerProcedure(Server);
                ((Process)sender).Dispose();
            };
            //Console.WriteLine(DateTime.Now + ": Running ServerUpdate method for " + Server.Name);
            Methods.Log(Server, DateTime.Now + ": Running ServerUpdate method for " + Server.Name);
            steamUpdateServer.Start();
            Server.CurrentlyUpdating = true;
            steamUpdateServer.StandardInput.WriteLine(GlobalVariables.SteamCMDCommand(Server));
            steamUpdateServer.StandardOutput.ReadToEndAsync();
            steamUpdateServer.StandardInput.WriteLine("exit");

        }
        public static void ServerUpdateWValidate(ArkServerInfo Server)
        {
            string command = Server.SteamCMDDir + "SteamCMD.exe " + "+login anonymous +force_install_dir " + Server.ServerDir + " +app_update 376030 validate";
            command = command + " +force_install_dir " + Server.SteamWorkshopDownloadDir;
            foreach (string mod in GlobalVariables.ActiveServerMods(Server))
            {
                command = command + " +workshop_download_item 346110 " + mod;
            }

            command = command + " +quit";
            

            Process steamUpdateServer = new Process();
            steamUpdateServer.StartInfo.UseShellExecute = false;
            steamUpdateServer.StartInfo.RedirectStandardInput = true;
            steamUpdateServer.StartInfo.RedirectStandardOutput = true;
            //steamUpdateServer.StartInfo.CreateNoWindow = false;
            steamUpdateServer.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            steamUpdateServer.EnableRaisingEvents = true;
            steamUpdateServer.StartInfo.FileName = "cmd.exe";
            steamUpdateServer.Exited += (sender, e) =>
            {
                //Console.WriteLine(DateTime.Now + ": Process Complete for " + Server.Name + "    Time: {0} sec " +
                //"Exit code:    {1}", DateTime.Now.Second - steamUpdateServer.StartTime.Second, steamUpdateServer.ExitCode);
                Methods.Log(Server, DateTime.Now + ": Process Complete for " + Server.Name + " Time: " + (DateTime.Now.Second - steamUpdateServer.StartTime.Second) + " sec " +
                "Exit code: " + steamUpdateServer.ExitCode);
                Server.CurrentlyUpdating = false;

                if (!Server.stopServerTimer)
                {
                    Server.GameUpdateNeeded = GlobalVariables.NeedsArkUpdate(Server);
                    Server.ModUpdateNeeded = GlobalVariables.NeedsModUpdate(Server);

                }
                Methods.StartServerProcedure(Server);
                ((Process)sender).Dispose();
            };
            steamUpdateServer.Start();
            //Console.WriteLine(DateTime.Now + ": Running ServerUpdate with Validate method for " + Server.Name);
            Methods.Log(Server, DateTime.Now + ": Running ServerUpdate with Validate method for " + Server.Name);
            steamUpdateServer.StandardOutput.ReadToEndAsync();
            steamUpdateServer.StandardInput.WriteLine(command);
            Server.CurrentlyUpdating = true;
            steamUpdateServer.StandardInput.WriteLine("exit");
        }
    }
}
