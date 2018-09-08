using SteamACFReader;
using SteamKit2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SASv2
{
    class VersionCheck
    {
        abstract class SteamInterface
        {
            public abstract int GetGameInformation(uint appid);
            public abstract bool VerifySteamPath(string ExecutablePath);
            public abstract int GetGameBuildVersion(string ApplicationPath);
            public abstract void UpdateGame(string UpdateFile, bool ShowOutput);
        }
        class SteamKit : IDisposable
        {
            private SteamUser _User;
            private SteamApps _Apps;
            private SteamClient _Client;
            private CallbackManager _CManager;
            private AutoResetEvent _ResetEvent;

            public bool Ready;
            public bool Failed;
            private bool _ThreadRunning;

            #region Disposal
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    // Stop thread if it is running
                    if (_ThreadRunning) _ThreadRunning = false;
                    Ready = false;

                    // Disconnect from Steam3
                    _User.LogOff();
                    if (_Client.IsConnected) _Client.Disconnect();
                }
            }
            #endregion Disposal

            #region Create Thread
            public static ThreadPair SpawnThread(AutoResetEvent r)
            {
                var thisThread = new SteamKit(r);
                Thread tThread = new Thread(() => thisThread.RunThread())
                {
                    IsBackground = true
                };

                tThread.Start();
                return new ThreadPair(tThread, thisThread);
            }
            #endregion Create Thread

            #region Thread Setup
            public SteamKit(AutoResetEvent r)
            {
                this._ResetEvent = r;

                this.Ready = false;
                this.Failed = false;
                this._ThreadRunning = true;

                this._Client = new SteamClient();
                this._CManager = new CallbackManager(this._Client);

                this._User = this._Client.GetHandler<SteamUser>();
                this._Apps = this._Client.GetHandler<SteamApps>();

                this.SubscribeCallbacks();
                this._Client.Connect();
            }

            public void RunThread()
            {
                while (this._ThreadRunning)
                {
                    try
                    {
                        this._CManager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
                    } catch
                    {
                    }
                    
                }
            }

            public void StopThread()
            {
                this._ThreadRunning = false;
            }

            public void SubscribeCallbacks()
            {
                this._CManager.Subscribe<SteamUser.LoggedOnCallback>(LogOnCallback);
                this._CManager.Subscribe<SteamClient.ConnectedCallback>(ConnectedCallback);
                this._CManager.Subscribe<SteamClient.DisconnectedCallback>(DisconnectedCallback);
            }
            #endregion Thread Setup

            #region Steam3 Callbacks
            private void ConnectedCallback(SteamClient.ConnectedCallback connected)
            {
                //_Parent.Log.ConsolePrint(LogLevel.Debug, "Connected to Steam3, Authenticating as anonymous user");
                _User.LogOnAnonymous();
            }

            private void DisconnectedCallback(SteamClient.DisconnectedCallback disconnected)
            {
                //_Parent.Log.ConsolePrint(LogLevel.Debug, "Disconnected from Steam3");
                _ResetEvent.Set();
                _ThreadRunning = false;
            }

            private void LogOnCallback(SteamUser.LoggedOnCallback loggedOn)
            {
                if (loggedOn.Result != EResult.OK)
                {
                    //_Parent.Log.ConsolePrint(LogLevel.Error, "Unable to connect to Steam3. Error: {0}", loggedOn.Result);
                    _ThreadRunning = false;

                    Failed = true;
                    _ResetEvent.Set();
                    return;
                }

                //_Parent.Log.ConsolePrint(LogLevel.Debug, "Logged in anonymously to Steam3");
                _ResetEvent.Set();
                Ready = true;
            }
            #endregion Steam3 Callbacks

            public delegate void AppCallback(SteamApps.PICSProductInfoCallback.PICSProductInfo returnData);
            public void RequestAppInfo(uint appid, AppCallback callback)
            {
                // Callback for Application Information
                Action<SteamApps.PICSProductInfoCallback> AppCallback = (appinfo) =>
                {
                    var returnData = appinfo.Apps.Where(x => x.Key == appid);
                    if (returnData.Count() == 1)
                    {
                        // Return successful data
                        callback(returnData.First().Value);
                        return;
                    }

                    callback(null);
                };

                // Callback for Token
                Action<SteamApps.PICSTokensCallback> TokenCallback = (apptoken) =>
                {
                    // Check if our token was returned
                    var ourToken = apptoken.AppTokens.Where(x => x.Key == appid);
                    if (ourToken.Count() != 1) return;

                    // Use our token to request the app information
                    var Request = new SteamApps.PICSRequest(appid)
                    {
                        AccessToken = ourToken.First().Value,
                        Public = false
                    };

                    _CManager.Subscribe(_Apps.PICSGetProductInfo(new List<SteamApps.PICSRequest>() { Request }, new List<SteamApps.PICSRequest>()), AppCallback);
                };

                // Fire Token Callback
                _CManager.Subscribe(_Apps.PICSGetAccessTokens(new List<uint>() { appid }, new List<uint>()), TokenCallback);
            }
        }
        class ThreadPair : IDisposable
        {
            public Thread tThread;
            public SteamKit tClass;

            public ThreadPair(Thread t, SteamKit c)
            {
                tThread = t;
                tClass = c;
            }

            #region Disposal
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    tClass.Dispose();
                }
            }
            #endregion Disposal
        }
        public static int GetGameInformation(uint appid)
        {
            var WaitHandle = new AutoResetEvent(false);
            using (var Steam3 = SteamKit.SpawnThread(WaitHandle))
            {
                // Wait for Steam3 to be ready
                WaitHandle.WaitOne();
                WaitHandle.Reset();

                // Prepare request to Steam3
                if (Steam3.tClass.Ready && !Steam3.tClass.Failed)
                {
                    var returndata = -1;
                    Steam3.tClass.RequestAppInfo(appid, (x) => {
                        KeyValue appinfo = x.KeyValues;
                        KeyValue DepotSection = appinfo.Children.Where(c => c.Name == "depots").FirstOrDefault();

                        // Retrieve Public Branch
                        KeyValue branches = DepotSection["branches"];
                        KeyValue node = branches["public"];

                        if (node != KeyValue.Invalid)
                        {
                            KeyValue buildid = node["buildid"];
                            if (buildid != KeyValue.Invalid)
                            {
                                //_Parent.Log.ConsolePrint(LogLevel.Debug, "Retrieved Buildid from Steam3: {0}", buildid.Value);
                                returndata = Convert.ToInt32(buildid.Value);
                            }
                        }

                        // Clear wait handle
                        WaitHandle.Set();
                    });

                    // Wait for Callback to finish
                    WaitHandle.WaitOne();
                    return returndata;
                }
            }

            return -1;
        }
        public static string GetGameBuildID(ArkServerInfo Server)
        {
            AcfReader appManifestReader = new AcfReader(Server.GameAppManifestACF);
            appManifestReader.ACFFileToStruct();
            appManifestReader.CheckIntegrity();
            ACF_Struct GameWorkshopACF = appManifestReader.ACFFileToStruct();

            return GameWorkshopACF.SubACF["AppState"].SubItems["buildid"];
        }
    }
}
