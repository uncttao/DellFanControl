using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using Microsoft.Win32;

namespace DellFanControl.DellFanControl
{

    public static class Global
    {

        public enum ACTION
        {
            NONE = 0,
            ENABLE = 2,
            DISABLE = 3,
            SUSPEND = 5,
            RESUME = 7,
            EXIT = 9,
            WAIT = 10
        }

    }

    static class Program
    {

        private static DellFanControlApplicationContext appContext;

        [STAThread]
        public static void Main()
        {
            // confirm warning (just on time)
            if (!File.Exists(".confirmed"))
            {
                var confirmResult = MessageBox.Show(
                    string.Join(
                        Environment.NewLine,
                        "This programm takes over the DELL fan conrol",
                        "and disables the internal thermal fan management!",
                        "",
                        "Use at your own risk and control the temperatures",
                        "with an external tool.",
                        "",
                        "The driver will ONLY be unloaded on a hard reset!"
                    ),
                    "Caution!",
                    MessageBoxButtons.OKCancel
                );

                if (confirmResult == DialogResult.Cancel)
                {
                    Quit();
                    return;
                }

                File.Create(".confirmed");
            }

            // all other exceptions
            Application.ThreadException += OnThreadException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            // see https://stackoverflow.com/a/10579614, must be called before Appplication.Run();
            AppDomain.CurrentDomain.ProcessExit += OnApplicationExit;
            Application.ApplicationExit += OnApplicationExit;
            SystemEvents.SessionEnding += OnSessionEnding;

            // see https://stackoverflow.com/a/406473
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            appContext = new DellFanControlApplicationContext();
            Application.Run(appContext);
        }

        public static void Quit()
        {
            Application.Exit();
        }

        private static void OnApplicationExit(object sender, EventArgs e)
        {
            // Remove custom fan control on Logout/Shutdown
            appContext.nextAction = (int)Global.ACTION.DISABLE;
            appContext.driverRunning = false;
            Thread.Sleep(1500);
        }

        private static void OnSessionEnding(object sender, SessionEndingEventArgs e)
        {
            // Remove custom fan control on Logout/Shutdown
            appContext.nextAction = (int)Global.ACTION.DISABLE;
            Thread.Sleep(1500);
        }

        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            appContext.nextAction = (int)Global.ACTION.DISABLE;
            appContext.driverRunning = false;
            Thread.Sleep(1500);
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            appContext.nextAction = (int)Global.ACTION.DISABLE;
            appContext.driverRunning = false;
            Thread.Sleep(1500);
        }

    }

    public class DellFanControlApplicationContext : ApplicationContext
    {

        public readonly NotifyIcon trayIcon;
        public int nextAction;
        public bool driverRunning = true;

        public readonly Dictionary<string, int> config = new Dictionary<string, int>()
        {
            {"pollingInterval", 1000}, // milliseconds
            {"minCooldownTime", 120}, // seconds
            {"FanOneActive", 1},
            {"FanOneCPUTemperatureThresholdZero", 45},
            {"FanOneCPUTemperatureThresholdOne", 50},
            {"FanOneCPUTemperatureThresholdTwo", 65},
            {"FanOneGPUemperatureThresholdZero", 45},
            {"FanOneGPUemperatureThresholdOne", 50},
            {"FanOneGPUTemperatureThresholdTwo", 65},
            {"FanTwoActive", 1},
            {"FanTwoCPUTemperatureThresholdZero", 45},
            {"FanTwoCPUTemperatureThresholdOne", 50},
            {"FanTwoCPUTemperatureThresholdTwo", 65},
            {"FanTwoGPUTemperatureThresholdZero", 45},
            {"FanTwoGPUTemperatureThresholdOne", 50},
            {"FanTwoGPUTemperatureThresholdTwo", 65},
        };

        public DellFanControlApplicationContext()
        {
            SystemEvents.PowerModeChanged += OnPowerModeChanged;

            // Initialize tray icon
            trayIcon = new NotifyIcon()
            {
                Icon = Properties.Resources.AppIcon,
                ContextMenu = new ContextMenu(new[] {
                    new MenuItem ("Enable Custom Fan Control", ContextMenuActionEnableFanControl),
                    new MenuItem ("Disable Custom Fan Control", ContextMenuActionDisableFanControl),
                    new MenuItem ("Exit", Exit),
                }),
                Visible = true
            };

            // Read config.xml
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\config.xml");
                config["pollingInterval"] = int.Parse(doc.DocumentElement?.Attributes["pollingInterval"].Value ?? throw new ApplicationException());
                config["minCooldownTime"] = int.Parse(doc.DocumentElement?.Attributes["minCooldownTime"].Value);
                config["fanOneActive"] = int.Parse(doc.DocumentElement.SelectSingleNode("/DellFanCtrl/FanOne")?.Attributes?["active"].Value ?? throw new ApplicationException());
                config["fanTwoActive"] = int.Parse(doc.DocumentElement.SelectSingleNode("/DellFanCtrl/FanTwo")?.Attributes?["active"].Value ?? throw new ApplicationException());
                config["FanOneCPUTemperatureThresholdZero"] = int.Parse(doc.DocumentElement.SelectSingleNode("/DellFanCtrl/FanOne/TemperatureThresholdZero")?.Attributes?["CPU"].Value ?? throw new ApplicationException());
                config["FanOneCPUTemperatureThresholdOne"] = int.Parse(doc.DocumentElement.SelectSingleNode("/DellFanCtrl/FanOne/TemperatureThresholdOne")?.Attributes?["CPU"].Value ?? throw new ApplicationException());
                config["FanOneCPUTemperatureThresholdTwo"] = int.Parse(doc.DocumentElement.SelectSingleNode("/DellFanCtrl/FanOne/TemperatureThresholdTwo")?.Attributes?["CPU"].Value ?? throw new ApplicationException());
                config["FanOneGPUTemperatureThresholdZero"] = int.Parse(doc.DocumentElement.SelectSingleNode("/DellFanCtrl/FanOne/TemperatureThresholdZero")?.Attributes?["GPU"].Value ?? throw new ApplicationException());
                config["FanOneGPUTemperatureThresholdOne"] = int.Parse(doc.DocumentElement.SelectSingleNode("/DellFanCtrl/FanOne/TemperatureThresholdOne")?.Attributes?["GPU"].Value ?? throw new ApplicationException());
                config["FanOneGPUTemperatureThresholdTwo"] = int.Parse(doc.DocumentElement.SelectSingleNode("/DellFanCtrl/FanOne/TemperatureThresholdTwo")?.Attributes?["GPU"].Value ?? throw new ApplicationException());
                config["FanTwoCPUTemperatureThresholdZero"] = int.Parse(doc.DocumentElement.SelectSingleNode("/DellFanCtrl/FanTwo/TemperatureThresholdZero")?.Attributes?["CPU"].Value ?? throw new ApplicationException());
                config["FanTwoCPUTemperatureThresholdOne"] = int.Parse(doc.DocumentElement.SelectSingleNode("/DellFanCtrl/FanTwo/TemperatureThresholdOne")?.Attributes?["CPU"].Value ?? throw new ApplicationException());
                config["FanTwoCPUTemperatureThresholdTwo"] = int.Parse(doc.DocumentElement.SelectSingleNode("/DellFanCtrl/FanTwo/TemperatureThresholdTwo")?.Attributes?["CPU"].Value ?? throw new ApplicationException());
                config["FanTwoGPUTemperatureThresholdZero"] = int.Parse(doc.DocumentElement.SelectSingleNode("/DellFanCtrl/FanTwo/TemperatureThresholdZero")?.Attributes?["GPU"].Value ?? throw new ApplicationException());
                config["FanTwoGPUTemperatureThresholdOne"] = int.Parse(doc.DocumentElement.SelectSingleNode("/DellFanCtrl/FanTwo/TemperatureThresholdOne")?.Attributes?["GPU"].Value ?? throw new ApplicationException());
                config["FanTwoGPUTemperatureThresholdTwo"] = int.Parse(doc.DocumentElement.SelectSingleNode("/DellFanCtrl/FanTwo/TemperatureThresholdTwo")?.Attributes?["GPU"].Value ?? throw new ApplicationException());
            }
            catch (Exception)
            {
                trayIcon.BalloonTipText = "Could not load config.xml, using default values.";
                trayIcon.ShowBalloonTip(5000);
            }
            
            // Start driver
            Thread notifyThread = new Thread(
                delegate()
                {
                    DellFanCtrl _ = new DellFanCtrl(this);
                }) {IsBackground = true};
            notifyThread.Start();
        }

        private void ContextMenuActionEnableFanControl(object sender, EventArgs e)
        {
            nextAction = (int)Global.ACTION.ENABLE;
        }

        private void ContextMenuActionDisableFanControl(object sender, EventArgs e)
        {
            nextAction = (int)Global.ACTION.DISABLE;
        }

        private void Exit(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            Program.Quit();
        }

        private void OnPowerModeChanged(object s, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Resume:
                    nextAction = (int)Global.ACTION.RESUME;
                    break;
                case PowerModes.Suspend:
                    nextAction = (int)Global.ACTION.SUSPEND;
                    break;
                case PowerModes.StatusChange:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

    }

}