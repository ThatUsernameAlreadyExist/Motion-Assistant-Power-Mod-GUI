using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PmGui
{
    public static class PowerEfficiency
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_POWER_THROTTLING_STATE
        {
            public uint Version;
            public uint ControlMask;
            public uint StateMask;
        }

        private const uint PROCESS_POWER_THROTTLING_CURRENT_VERSION = 1;
        private const uint PROCESS_POWER_THROTTLING_EXECUTION_SPEED = 0x1;

        private const int ProcessPowerThrottling = 4;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetProcessInformation(IntPtr hProcess,
                                                         int ProcessInformationClass,
                                                         ref PROCESS_POWER_THROTTLING_STATE ProcessInformation,
                                                         int ProcessInformationSize);

        public static void SetEcoQoS(bool enableEcoQoS)
        {
            try
            {
                var throttlingState = new PROCESS_POWER_THROTTLING_STATE
                {
                    Version = PROCESS_POWER_THROTTLING_CURRENT_VERSION,
                    ControlMask = PROCESS_POWER_THROTTLING_EXECUTION_SPEED,
                    StateMask = enableEcoQoS ? PROCESS_POWER_THROTTLING_EXECUTION_SPEED : 0
                };

                int size = Marshal.SizeOf(typeof(PROCESS_POWER_THROTTLING_STATE));

                bool result = SetProcessInformation(
                    Process.GetCurrentProcess().Handle,
                    ProcessPowerThrottling,
                    ref throttlingState,
                    size
                );

                if (!result)
                {
                    int error = Marshal.GetLastWin32Error();
                    Debug.WriteLine($"Can't set EcoQoS: {error}");
                }

                Process.GetCurrentProcess().PriorityClass = enableEcoQoS
                    ? ProcessPriorityClass.Idle
                    : ProcessPriorityClass.Normal;

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error set EcoQoSS: {ex.Message}");
            }
        }
    }
}