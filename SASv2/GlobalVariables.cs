using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using IniParser;
using IniParser.Model;
using HtmlAgilityPack;

namespace SASv2
{
    class GlobalVariables
    {
        public static string NameOfMod(string numOfMod)
        {
            var modName = "";
            try
            {
                using (WebClient client = new WebClient())
                {
                    string htmlCode = client.DownloadString("https://steamcommunity.com/sharedfiles/filedetails/?id=" + numOfMod);
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(htmlCode);

                    var titles = doc.DocumentNode.SelectSingleNode("//div[@class='workshopItemTitle']");
                    modName = titles.InnerText;

                }
            }
            catch
            {
                return modName;
            }
            
            return modName;
        }
        public static string appName = "ShooterGameServer";
        public static string serverRestartNotification(int timeTillRestart)
        {
            string serverMessage = "";
            if (timeTillRestart > 0)
                serverMessage = string.Format("Server is going to restart in: {0} minutes.", timeTillRestart.ToString());
            if (timeTillRestart == 0)
                serverMessage = "Server is restarting. See you on the flipside!";
            return serverMessage;
        }
        public static string[] ActiveServerMods(ArkServerInfo Server)
        {
            string[] activeMods;

            var parser = new FileIniDataParser();
            parser.Parser.Configuration.AllowDuplicateKeys = true;
            IniData serverData = new IniData();
            serverData = parser.ReadFile(Server.GUSFile);

            activeMods = serverData["ServerSettings"]["ActiveMods"].Split(',');

            return activeMods;
        }
        public static bool NeedsArkUpdate(ArkServerInfo Server)
        {
            if (Int32.Parse(VersionCheck.GetGameBuildID(Server)) < VersionCheck.GetGameInformation(376030))
            {
                return true;
            }
            return false;
        }
        public static string SteamCMDCommand(ArkServerInfo Server)
        {
            string command = Server.SteamCMDDir + "SteamCMD.exe " + "+login anonymous +force_install_dir " + Server.ServerDir + " +app_update 376030";
            command = command + " +force_install_dir " + Server.SteamWorkshopDownloadDir;
            foreach (string mod in ActiveServerMods(Server))
            {
                command = command + " +workshop_download_item 346110 " + mod;
            }

            command = command + " +quit";
            return command;
        }
        public static bool IsProcessOpen(ArkServerInfo Server)
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(appName);
                foreach (Process clsProcess in processes)
                {
                    if (clsProcess.ProcessName.Contains(appName))
                    {
                        if (clsProcess.MainModule.FileName.Contains(Server.ServerDir))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                //Console.WriteLine(DateTime.Now + ": Exception occured when checking if ShooterGameServer.exe is an active process. Exception: " + ex.Message);
                Methods.Log(Server, DateTime.Now + ": Exception occured when checking if ShooterGameServer.exe is an active process. Exception: " + ex.Message);
            }
            return false;
        }
        private static List<string[]> UpdatedModNTime = new List<string[]>();
        private static List<string[]> ServerModNTime = new List<string[]>();
        public static bool NeedsModUpdate(ArkServerInfo Server)
        {

            bool workshopItemNeedsUpdate = false;

            AcfReader serverReader = new AcfReader(Server.CurrentWorkshopACF);
            serverReader.ACFFileToStruct();
            serverReader.CheckIntegrity();
            ACF_Struct atLaunchACF = serverReader.ACFFileToStruct();

            AcfReader updateReader = new AcfReader(Server.UpdatedWorkshopACF);
            updateReader.ACFFileToStruct();
            updateReader.CheckIntegrity();
            ACF_Struct UpdatedWorkshopACF = updateReader.ACFFileToStruct();

            foreach (string mod in ActiveServerMods(Server))
            {
                string[] UpdatedModNTimeData = new string[2];
                UpdatedModNTimeData[0] = mod;
                try
                {
                    UpdatedModNTimeData[1] = UpdatedWorkshopACF.SubACF["AppWorkshop"].SubACF["WorkshopItemsInstalled"].SubACF[mod].SubItems["timeupdated"];
                }
                catch (Exception ex)
                {
                    //Console.WriteLine(DateTime.Now + ": Error in NeedsModUpdate in AllActiveServerMods on mod " + mod + " with exception: " + ex.Message);
                    Methods.Log(Server, DateTime.Now + ": Exception occured when trying to read all active mod's timeupdated value. Exception: " + ex.Message);
                    break;
                }
                UpdatedModNTime.Add(UpdatedModNTimeData);
            }
            foreach (string mod in ActiveServerMods(Server))
            {
                string[] ServerModNTimeData = new string[2];
                ServerModNTimeData[0] = mod;
                try
                {
                    ServerModNTimeData[1] = atLaunchACF.SubACF["AppWorkshop"].SubACF["WorkshopItemsInstalled"].SubACF[mod].SubItems["timeupdated"];
                }
                catch (Exception ex)
                {
                    //Console.WriteLine(DateTime.Now + ": Error reading working time updated element. Exception: " + ex.Message);
                    Methods.Log(Server, DateTime.Now + ": Error reading working time updated element. Exception: " + ex.Message);
                }

                ServerModNTime.Add(ServerModNTimeData);
            }
            foreach (string[] BMod in UpdatedModNTime)
            {
                foreach (string[] AMod in ServerModNTime)
                {
                    //if (BMod[0] != "1257464589")
                    if (BMod[0] == AMod[0])
                    {
                        if (BMod[1] != AMod[1])
                        {
                            workshopItemNeedsUpdate = true;
                            Server.theModNeedingUpdate = NameOfMod(BMod[0]);
                            //Console.WriteLine(DateTime.Now.ToString() + ": Restart Triggered for Server " + Server.Name + "  to update the following mod: " + BMod[0] + "-" + GlobalVariables.NameOfMod(BMod[0]));
                            Methods.Log(Server, DateTime.Now.ToString() + ": Restart Triggered to update the following mod: " + BMod[0] + "-" + GlobalVariables.NameOfMod(BMod[0]));
                            break;
                        }
                        if (BMod[1] == AMod[1])
                        {
                            workshopItemNeedsUpdate = false;
                        }
                    }
                }
                if (workshopItemNeedsUpdate)
                {
                    break;
                }
            }
            UpdatedModNTime.Clear();
            ServerModNTime.Clear();


            return workshopItemNeedsUpdate;
        }
        //Stability App Settings
        public static int timeIntervalMaintenanceRestart = 240;
        public static int presetTimeToRestart = 10;
    }
    public class ArkServerInfo
    {
        public string Name { get; set; }
        public string IPAddress { get { return "127.0.0.1"; } }
        public string ServerTitle
        {
            get
            {
                var parser = new FileIniDataParser();
                parser.Parser.Configuration.AllowDuplicateKeys = true;
                IniData data = new IniData();
                data = parser.ReadFile(this.GUSFile);
                return data["SessionSettings"]["SessionName"];
            }
        }
        public string QueryPort
        {
            get
            {
                var parser = new FileIniDataParser();
                parser.Parser.Configuration.AllowDuplicateKeys = true;
                IniData data = new IniData();

                data = parser.ReadFile(this.GUSFile);
                return data["SessionSettings"]["QueryPort"];
            }
        }
        public string GamePort {
            get
            {
                var parser = new FileIniDataParser();
                parser.Parser.Configuration.AllowDuplicateKeys = true;
                IniData data = new IniData();

                data = parser.ReadFile(this.GUSFile);
                return data["SessionSettings"]["Port"];
            }
        }
        public string RCONPort {
            get
            {
                var parser = new FileIniDataParser();
                parser.Parser.Configuration.AllowDuplicateKeys = true;
                IniData data = new IniData();

                data = parser.ReadFile(this.GUSFile);
                return data["ServerSettings"]["RCONPort"];
            }
        }
        public string[] Mods {
            get
            {
                var parser = new FileIniDataParser();
                parser.Parser.Configuration.AllowDuplicateKeys = true;
                IniData data = new IniData();

                data = parser.ReadFile(this.GUSFile);
                return data["ServerSettings"]["ActiveMods"].Split(',');
            }
        }
        public string ServerPassword {
            get
            {
                var parser = new FileIniDataParser();
                parser.Parser.Configuration.AllowDuplicateKeys = true;
                IniData data = new IniData();

                data = parser.ReadFile(this.GUSFile);
                return data["ServerSettings"]["ServerAdminPassword"];
            }
        }
        public string ServerDir { get; set; }
        public string SteamCMDDir { get { return this.ServerDir + @"Engine\Binaries\ThirdParty\SteamCMD\Win64\"; } }
        public string iniDir { get{ return this.ServerDir + @"ShooterGame\Saved\Config\WindowsServer\"; } }
        public string SavedGame { get { return this.ServerDir + @"ShooterGame\Saved\SavedArks"; } }
        public string BackupGames { get { return this.ServerDir + @"ShooterGame\Saved\BackupArks"; } }
        public string SASLogs { get { return this.ServerDir + @"ShooterGame\Saved\SASLogs"; } }
        public string SASFile { get; set; }
        public string GameINIFile{ get { return this.iniDir + @"Game.ini"; } }
        public string GUSFile { get { return this.iniDir + @"GameUserSettings.ini"; } }
        public string RunBatFile { get { return this.iniDir + @"RunServer.cmd";  } }
        public string SteamWorkshopDownloadDir { get { return this.SteamCMDDir + @"steamapps\workshop\"; } }
        public string UpdateCommand { get; set; }
        public string VersionPath { get{ return this.ServerDir + "\\version.txt"; }}
        public string UpdatedWorkshopACF { get { return this.SteamWorkshopDownloadDir + @"appworkshop_346110.acf"; } }
        public string CurrentWorkshopACF { get { return this.ServerDir + @"appworkshop_346110.acf"; } }
        public string GameAppManifestACF { get { return this.ServerDir + @"steamapps\appmanifest_376030.acf"; } }
        public string theModNeedingUpdate { get; set; }
        
        public bool ModUpdateNeeded { get; set; }
        public bool isProcessOpen {
            get
            {
                try
                {
                    Process[] processes = Process.GetProcessesByName(GlobalVariables.appName);
                    foreach (Process clsProcess in processes)
                    {
                        if (clsProcess.ProcessName.Contains(GlobalVariables.appName))
                        {
                            if (clsProcess.MainModule.FileName.Contains(this.ServerDir))
                            {
                                return true;
                            }
                        }
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    //Console.WriteLine(DateTime.Now + ": Exception occured when checking if ShooterGameServer.exe is an active process. Exception: " + ex.Message);
                    Methods.Log(this, DateTime.Now + ": Exception occured when checking if ShooterGameServer.exe is an active process. Exception: " + ex.Message);
                }
                return false;
            }
        }
        public bool GameUpdateNeeded { get; set; }
        public bool CurrentlyUpdating { get; set; }
        public bool stopServerTimer { get; set; }

        public int nEventsProcessOpenNotResponding { get; set; }
        public int nOfTicks { get; set; }
        public int nNotRunning { get; set; }
    }
}
