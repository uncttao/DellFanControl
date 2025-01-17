using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace DellFanControl.DellFanControl
{

    class DellFanCtrl
    {
        private IntPtr hDriver;

        private const uint DELL_SMM_IO_FAN1 = 0;
        private const uint DELL_SMM_IO_FAN2 = 1;

        private const uint DELL_SMM_IO_GET_SENSOR_TEMP = 0x10a3;
        private const uint DELL_SMM_IO_SENSOR_CPU = 0; // Probably Core 1
        private const uint DELL_SMM_IO_SENSOR_GPU = 8; // ?? how many sensors

        private const uint DELL_SMM_IO_SET_FAN_LV = 0x01a3;

        private const uint DELL_SMM_IO_FAN_LV0 = 0;
        private const uint DELL_SMM_IO_FAN_LV1 = 1;
        private const uint DELL_SMM_IO_FAN_LV2 = 2;

        private const uint DELL_SMM_IO_DISABLE_FAN_CTL1 = 0x30a3;
        private const uint DELL_SMM_IO_ENABLE_FAN_CTL1 = 0x31a3;
        private const uint DELL_SMM_IO_DISABLE_FAN_CTL2 = 0x34a3;
        private const uint DELL_SMM_IO_ENABLE_FAN_CTL2 = 0x35a3;
        private const uint DELL_SMM_IO_NO_ARG = 0x0;

        private void Init(DellFanControlApplicationContext context)
        {
            hDriver = Interop.CreateFile(
                @"\\.\BZHDELLSMMIO",
                Interop.GENERIC_READ | Interop.GENERIC_WRITE,
                0,
                IntPtr.Zero,
                Interop.OPEN_EXISTING,
                Interop.FILE_ATTRIBUTE_NORMAL,
                IntPtr.Zero
            );

            IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

            if (hDriver == INVALID_HANDLE_VALUE)
            {
                Console.WriteLine("notRunning");
                BDSID_InstallDriver();

                Process process = new Process();
                if (Environment.Is64BitOperatingSystem)
                {
                    process.StartInfo.FileName = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\lib\\wind64.exe";
                }
                else
                {
                    process.StartInfo.FileName = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\lib\\wind32.exe";
                }
                process.StartInfo.Arguments = "/L BZHDELLSMMIO";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                while (!process.StandardOutput.EndOfStream)
                {
                    // TODO: Catch wind64.exe errors by parsing line
                    process.StandardOutput.ReadLine();
                }

                hDriver = Interop.CreateFile(@"\\.\BZHDELLSMMIO",
                    Interop.GENERIC_READ | Interop.GENERIC_WRITE,
                    Interop.FILE_SHARE_READ | Interop.FILE_SHARE_WRITE,
                    IntPtr.Zero,
                    Interop.OPEN_EXISTING,
                    Interop.FILE_ATTRIBUTE_NORMAL,
                    IntPtr.Zero);

                // As long as the driver is not signed and started with wind64.exe we do not need this 
                // BDSID_StartDriver();
            }

            if (context.config["FanOneActive"] == 1)
            {
                dell_smm_io(DELL_SMM_IO_DISABLE_FAN_CTL1, DELL_SMM_IO_NO_ARG);
            }

            if (context.config["FanTwoActive"] == 1)
            {
                dell_smm_io(DELL_SMM_IO_DISABLE_FAN_CTL2, DELL_SMM_IO_NO_ARG);
            }

            // Console.WriteLine(Interop.GetLastError());
        }

        public DellFanCtrl(DellFanControlApplicationContext context)
        {
            Init(context);

            int fanOneLevel = 0;
            int fanTwoLevel = 0;
            int fanDelayTime1 = 0;
            int fanDelayTime2 = 0;

            while (context.driverRunning)
            {
                Thread.Sleep(context.config["pollingInterval"]);

                if (context.nextAction == (int)Global.ACTION.EXIT)
                {
                    context.nextAction = (int)Global.ACTION.WAIT;
                    dell_smm_io(DELL_SMM_IO_ENABLE_FAN_CTL1, DELL_SMM_IO_NO_ARG);
                    dell_smm_io(DELL_SMM_IO_ENABLE_FAN_CTL2, DELL_SMM_IO_NO_ARG);
                    break;
                }

                if (context.nextAction == (int)Global.ACTION.ENABLE)
                {
                    context.nextAction = (int)Global.ACTION.NONE;
                    fanOneLevel = 0;
                    fanTwoLevel = 0;
                    fanDelayTime1 = 0;
                    fanDelayTime2 = 0;
                    Init(context);
                }

                if (context.nextAction == (int)Global.ACTION.RESUME)
                {
                    context.nextAction = (int)Global.ACTION.NONE;
                    fanOneLevel = 0;
                    fanTwoLevel = 0;
                    fanDelayTime1 = 0;
                    fanDelayTime2 = 0;
                    Init(context);
                }

                switch (context.nextAction)
                {
                    case (int)Global.ACTION.DISABLE:
                        context.nextAction = (int)Global.ACTION.WAIT;
                        fanOneLevel = 0;
                        fanTwoLevel = 0;
                        fanDelayTime1 = 0;
                        fanDelayTime2 = 0;
                        dell_smm_io(DELL_SMM_IO_ENABLE_FAN_CTL1, DELL_SMM_IO_NO_ARG);
                        dell_smm_io(DELL_SMM_IO_ENABLE_FAN_CTL2, DELL_SMM_IO_NO_ARG);
                        continue;
                    case (int)Global.ACTION.SUSPEND:
                        context.nextAction = (int)Global.ACTION.WAIT;
                        fanOneLevel = 0;
                        fanTwoLevel = 0;
                        fanDelayTime1 = 0;
                        fanDelayTime2 = 0;
                        dell_smm_io(DELL_SMM_IO_ENABLE_FAN_CTL1, DELL_SMM_IO_NO_ARG);
                        dell_smm_io(DELL_SMM_IO_ENABLE_FAN_CTL2, DELL_SMM_IO_NO_ARG);
                        continue;
                }

                var tempCPU = dell_smm_io_get_cpu_temperature();
                var tempGPU = dell_smm_io_get_gpu_temperature();

                context.trayIcon.Text = "CPU: " + tempCPU.ToString() + "�C" + Environment.NewLine + "GPU: " + tempGPU.ToString() + "�C";

                if (context.nextAction == (int)Global.ACTION.WAIT)
                {
                    continue;
                }

                fanDelayTime1--;
                fanDelayTime2--;

                if (fanDelayTime1 <= 0)
                {
                    fanDelayTime1 = 0;
                }

                if (fanDelayTime2 <= 0)
                {
                    fanDelayTime2 = 0;
                }

                // Fan one

                if (tempCPU < context.config["FanOneCPUTemperatureThresholdZero"] && tempGPU < context.config["FanOneGPUTemperatureThresholdZero"])
                {
                    fanOneLevel = 0;
                }

                if (tempCPU >= context.config["FanOneCPUTemperatureThresholdOne"] || tempGPU >= context.config["FanOneGPUTemperatureThresholdOne"])
                {
                    fanOneLevel = 1;
                }

                if (tempCPU >= context.config["FanOneCPUTemperatureThresholdTwo"] || tempGPU >= context.config["FanOneGPUTemperatureThresholdTwo"])
                {
                    fanOneLevel = 2;
                }

                // Fan two

                if (tempCPU < context.config["FanTwoCPUTemperatureThresholdZero"] && tempGPU < context.config["FanTwoGPUTemperatureThresholdZero"])
                {
                    fanTwoLevel = 0;
                }

                if (tempCPU >= context.config["FanTwoCPUTemperatureThresholdOne"] || tempGPU >= context.config["FanTwoGPUTemperatureThresholdOne"])
                {
                    fanTwoLevel = 1;
                }

                if (tempCPU >= context.config["FanTwoCPUTemperatureThresholdTwo"] || tempGPU >= context.config["FanTwoGPUTemperatureThresholdTwo"])
                {
                    fanTwoLevel = 2;
                }

                // Set the fan speed

                // Fan 1

                switch (fanOneLevel)
                {
                    case 0 when fanDelayTime1 == 0:
                        dell_smm_io_set_fan_lv(DELL_SMM_IO_FAN1, DELL_SMM_IO_FAN_LV0);
                        break;
                    case 1 when fanDelayTime2 == 0:
                        fanDelayTime1 = context.config["minCooldownTime"];
                        dell_smm_io_set_fan_lv(DELL_SMM_IO_FAN1, DELL_SMM_IO_FAN_LV1);
                        break;
                    case 2:
                        fanDelayTime2 = context.config["minCooldownTime"];
                        dell_smm_io_set_fan_lv(DELL_SMM_IO_FAN1, DELL_SMM_IO_FAN_LV2);
                        break;
                }

                // Fan 2

                switch (fanTwoLevel)
                {
                    case 0 when fanDelayTime1 == 0:
                        dell_smm_io_set_fan_lv(DELL_SMM_IO_FAN2, DELL_SMM_IO_FAN_LV0);
                        break;
                    case 1 when fanDelayTime2 == 0:
                        fanDelayTime1 = context.config["minCooldownTime"];
                        dell_smm_io_set_fan_lv(DELL_SMM_IO_FAN2, DELL_SMM_IO_FAN_LV1);
                        break;
                    case 2:
                        fanDelayTime2 = context.config["minCooldownTime"];
                        dell_smm_io_set_fan_lv(DELL_SMM_IO_FAN2, DELL_SMM_IO_FAN_LV2);
                        break;
                }
            }

            Interop.CloseHandle(hDriver);
            BDSID_Shutdown();
        }

        private void BDSID_InstallDriver()
        {
            BDSID_RemoveDriver();

            IntPtr hService;

            IntPtr hSCManager = Interop.OpenSCManager(null, null, (uint)Interop.SCM_ACCESS.SC_MANAGER_ALL_ACCESS);
            if (hSCManager != IntPtr.Zero)
            {
                hService = Interop.CreateService(
                    hSCManager,
                    "BZHDELLSMMIO",
                    "BZHDELLSMMIO",
                    Interop.SERVICE_ALL_ACCESS,
                    Interop.SERVICE_KERNEL_DRIVER,
                    Interop.SERVICE_DEMAND_START,
                    Interop.SERVICE_ERROR_NORMAL,
                    getDriverPath(),
                    null,
                    null,
                    null,
                    null,
                    null
                );

                Interop.CloseServiceHandle(hSCManager);

                if (hService == IntPtr.Zero)
                {
                    Console.WriteLine("hService is null");
                    Debug.Print("hService is null");
                    return;
                }
            }
            else
            {
                Console.WriteLine("hSCManager is null");
                Debug.Print("hSCManager is null");
                return;
            }

            Interop.CloseServiceHandle(hService);
        }

        private uint dell_smm_io_get_cpu_temperature()
        {
            return dell_smm_io(DELL_SMM_IO_GET_SENSOR_TEMP, DELL_SMM_IO_SENSOR_CPU);
        }

        private uint dell_smm_io_get_gpu_temperature()
        {
            return dell_smm_io(DELL_SMM_IO_GET_SENSOR_TEMP, DELL_SMM_IO_SENSOR_GPU);
        }

        private void dell_smm_io_set_fan_lv(uint fan_no, uint lv)
        {
            uint arg = (lv << 8) | fan_no;
            dell_smm_io(DELL_SMM_IO_SET_FAN_LV, arg);
        }

        private uint dell_smm_io(uint cmd, uint data)
        {
            Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)1;

            Interop.SMBIOS_PKG cam = new Interop.SMBIOS_PKG
            {
                cmd = cmd,
                data = data,
                stat1 = 0,
                stat2 = 0
            };

            uint IOCTL_BZH_DELL_SMM_RWREG = Interop.CTL_CODE(Interop.FILE_DEVICE_BZH_DELL_SMM, Interop.BZH_DELL_SMM_IOCTL_KEY, 0, 0);

            uint result_size = 0;

            bool status_dic = Interop.DeviceIoControl(hDriver,
                IOCTL_BZH_DELL_SMM_RWREG,
                ref cam,
                (uint)Marshal.SizeOf(cam),
                ref cam,
                (uint)Marshal.SizeOf(cam),
                ref result_size,
                IntPtr.Zero);

            if (status_dic == false)
            {
                Console.WriteLine(Interop.GetLastError());
                return 0;
            }
            else
            {
                uint foo = cam.cmd;

                return foo;
            }
        }

        private void BDSID_RemoveDriver()
        {
            Interop.QUERY_SERVICE_CONFIG pServiceConfig = new Interop.QUERY_SERVICE_CONFIG();

            BDSID_StopDriver();

            IntPtr hSCManager = Interop.OpenSCManager(null, null, (uint)Interop.SCM_ACCESS.SC_MANAGER_ALL_ACCESS);

            if (hSCManager != IntPtr.Zero)
            {
                return;
            }

            IntPtr hService = Interop.OpenService(hSCManager, "BZHDELLSMMIO", Interop.SERVICE_ALL_ACCESS);
            Interop.CloseServiceHandle(hSCManager);

            if (hService != IntPtr.Zero)
            {
                return;
            }

            Interop.QueryServiceConfig(hService, IntPtr.Zero, 0, out var dwBytesNeeded);

            if (Interop.GetLastError() == Interop.ERROR_INSUFFICIENT_BUFFER)
            {

                var bResult = Interop.QueryServiceConfig(hService, pServiceConfig, dwBytesNeeded, out dwBytesNeeded);

                if (!bResult)
                {
                    Interop.CloseServiceHandle(hService);
                    return;
                }

                // If service is set to load automatically, don't delete it!
                if (pServiceConfig.dwStartType == Interop.SERVICE_DEMAND_START)
                {
                    Interop.DeleteService(hService);
                }
            }

            Interop.CloseServiceHandle(hService);
        }

        private void BDSID_StopDriver()
        {

            Interop.SERVICE_STATUS serviceStatus = new Interop.SERVICE_STATUS();

            IntPtr hSCManager = Interop.OpenSCManager(null, null, (uint)Interop.SCM_ACCESS.SC_MANAGER_ALL_ACCESS);

            if (hSCManager == IntPtr.Zero) return;
            IntPtr hService = Interop.OpenService(hSCManager, "BZHDELLSMMIO", Interop.SERVICE_ALL_ACCESS);

            Interop.CloseServiceHandle(hSCManager);

            if (hService == IntPtr.Zero) return;
            Interop.ControlService(hService, Interop.SERVICE_CONTROL.STOP, ref serviceStatus);

            Interop.CloseServiceHandle(hService);
        }

        private void BDSID_Shutdown()
        {
            IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

            if (hDriver != INVALID_HANDLE_VALUE)
            {
                Interop.CloseHandle(hDriver);
            }

            BDSID_RemoveDriver();
        }

        private string getDriverPath()
        {
            if (Environment.Is64BitOperatingSystem)
            {
                return System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\lib\\bzh_dell_smm_io_x64.sys";
            }
            else
            {
                return System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\lib\\bzh_dell_smm_io_x64.sys";
            }

        }

    }

}