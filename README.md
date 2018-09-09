# Stable-Ark-Servers-v2.0
This project is created for maintaining server stability for Ark Survival Evolved Dedicated Servers. Needless to say, this project is utilized only by windows platforms.

The current project is currently a work in progress. Most of the functionality works as intended but needs to be polished.

# Using SASv2

This project has 4 Dependencies:

HTMLAgilityPack - Install-Package HtmlAgilityPack -Version 1.8.7

INI-Parser - Install-Package ini-parser

SteamKit2 - Install-Package SteamKit2 -Version 2.1.0

You can install these dependencies by using the Package Manager Console.

The next thing you will need is a compiled DLL from the updated RCON Library located here: 

https://github.com/Reginherus/ArkRcon

# Setting up the Ark Servers
First you need to have fully functional ark servers and know the directory to those.

To install an ark dedicated server:

Download SteamCMD

Open up the SteamCMD command line and run this command

@ShutdownOnFailedCommand 1 +@NoPromptForPassword 1 +logon anonymous +force_install_dir "Ark Files Dir" +app_update 376030 validate +exit
  
 If you plan on hosting a cluster, you will need to run this for each server you plan on hosting.
 
 After installation, navigate to Dir/ShooterGame/Binaries and launch shootergame.exe so that ark save files can be created. Close the application after the launch finished. 
 
 Double check that the files were created. You should be able to navigate to Dir/ShooterGame/Saved/Config
 
 # Creating your own launch batch script
 
  (this will be automated in future versions)
 
 Navigate to Dir/ShooterGame/Saved/Config/WindowsServer and right click on empty white space. Hover over new and click on Text Document.
 
 In this text document put all of the command line arguments you want. Command Line Arguments for Ark can be found here: https://ark.gamepedia.com/Server_Configuration
 
 Make sure that the Command Line Syntax is correct.
 
 Then go to File -> Save As and in the Save As Type field, click on the drop down and select All Files.
 
 Name the file RunServer.cmd and click save.
 
 You should now see a script file in that directory. Run the script and wait for the server to fully boot.
 
 # Running SASv2
 
 Build the project and go to the SASv2 executable. In order to build the project, you will need to include all of the dependencies.
 
 Launch the executable, enter n for "Load server info from file?" option and fill in the required information for the initialization. 
 
 You will need to enter a handle for the server, the directory at which it resides
 
 After the initialization process is complete, every time you launch SASv2 you can enter y for "Load server info from file?" to automatically load your previously entered server information.
 
 # Existing Commands
 
 start global timer - this starts the global timer that triggers all events inside the application. Only use this to restart after stopping the global timer.
 
 stop global timer - this will stop the global timer which events will not trigger until restarted.
 
 check timer - Check to see if timer is active or not.
 
 restart "Server Handle" - this triggers the restart server event which will start an 11 minute countdown to restart. First minute notifies the server that a restart was triggered. Second minute notifies the server that there will be a restart in 10 minutes. At 11 minutes the server will be restarted.
 
 Note: The server handle is the name that you assigned it when starting this program. NOT the name of your ark server.
 
 update - triggers the update server event for all servers in the list.
 
 bm all: - This will broadcast a message to all servers.
 
 Exit - will close SASv2 down completely.
 
 
 # Future Plans
 
 This application is 100% CLI, I will be importing all classes and methods into a GUI in the near future.
 
 
 
