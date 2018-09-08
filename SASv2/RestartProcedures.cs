using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASv2
{
    class RestartProcedures
    {
        public static void BroadCastArkUpdateRestartTimer(ArkServerInfo Server, string Reason)
        {
            //Notifies players that a server restart is coming and gives the reason.
            RCONCommands.ServerRestartTriggered(Server, Reason);

            System.Timers.Timer restartTimer = new System.Timers.Timer();
            restartTimer.Interval = 60000;
            restartTimer.Elapsed += (sender, e) => BroadCastArkUpdateRestartNotification(sender, e, Server);

            restartTimer.Start();
        }
        private static void BroadCastArkUpdateRestartNotification(object sender, EventArgs e, ArkServerInfo Server)
        {
            int ticks = 0;

            ticks = Server.nOfTicks;

            int minutesTillRestart = GlobalVariables.presetTimeToRestart - ticks;
            System.Timers.Timer timer = (System.Timers.Timer)sender;

            if (minutesTillRestart == 10)
                RCONCommands.GlobalNotification(Server, GlobalVariables.serverRestartNotification(minutesTillRestart));
            if (minutesTillRestart < 6 && minutesTillRestart > 0)
                RCONCommands.GlobalNotification(Server, GlobalVariables.serverRestartNotification(minutesTillRestart));
            //if (minutesTillRestart == 1)
            //    Methods.BackupServerFiles(Server);
            if (minutesTillRestart == 0)
            {
                RCONCommands.GlobalNotification(Server, GlobalVariables.serverRestartNotification(minutesTillRestart));
                timer.Close();
                Server.nOfTicks = 0;

                Methods.ArkUpdateShutdownProcedure(Server);
            }
            if (minutesTillRestart != 0)
            {
                Server.nOfTicks++;
            }

        }
        public static void BroadCastModUpdateRestartTimer(ArkServerInfo Server, string Reason)
        {
            //Notifies players that a server restart is coming and gives the reason.
            RCONCommands.ServerRestartTriggered(Server, Reason);

            System.Timers.Timer restartTimer = new System.Timers.Timer();
            restartTimer.Interval = 60000;
            restartTimer.Elapsed += (sender, e) => BroadCastModUpdateRestartNotification(sender, e, Server);

            restartTimer.Start();
        }
        private static void BroadCastModUpdateRestartNotification(object sender, EventArgs e, ArkServerInfo Server)
        {
            int ticks = 0;
            System.Timers.Timer timer = (System.Timers.Timer)sender;
            ticks = Server.nOfTicks;

            int minutesTillRestart = GlobalVariables.presetTimeToRestart - ticks;
            if (minutesTillRestart == 10)
                RCONCommands.GlobalNotification(Server, GlobalVariables.serverRestartNotification(minutesTillRestart));
            if (minutesTillRestart < 6 && minutesTillRestart > 0)
                RCONCommands.GlobalNotification(Server, GlobalVariables.serverRestartNotification(minutesTillRestart));
            //if (minutesTillRestart == 1)
            //    Methods.BackupServerFiles(Server);
            if (minutesTillRestart == 0)
            {
                RCONCommands.GlobalNotification(Server, GlobalVariables.serverRestartNotification(minutesTillRestart));
                timer.Close();

                Server.nOfTicks = 0;

                Methods.ModUpdateShutdownProcedure(Server);
            }
            if (minutesTillRestart != 0)
            {
                Server.nOfTicks++;
            }

        }
    }
}
