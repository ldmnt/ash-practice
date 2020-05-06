using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASHDebug
{
    public class GameState
    {
        private static readonly int FLIGHT_COOLDOWN = 1000;

        private static readonly string UNITYPLAYER_MODULE = "UnityPlayer.dll";
        private static readonly int[] PLAYER = { 0x109ac54, 0x38, 0x34, 0x3C, 0x44 };
        private static readonly int MAX_FEATHERS = 0x234;
        private static readonly int IS_GROUNDED = 0x44;

        public int MaxFeathers { get; private set; }
        public bool IsGrounded { get; private set; }
        public TimeSpan FlightDuration { get; private set; }

        private Stopwatch FlightStopwatch = new Stopwatch();
        private Stopwatch Clock = new Stopwatch();

        private Memory Memory;
        private int BaseAddress;

        private int LastFlightEnd = 0;

        public GameState(Process process)
        {
            var handle = WinAPI.OpenProcess(WinAPI.PROCESS_WM_READ, false, process.Id);
            Memory = new Memory(handle);

            var unityPlayer = process.Modules.Cast<ProcessModule>().SingleOrDefault(
                m => string.Equals(m.ModuleName, UNITYPLAYER_MODULE, StringComparison.OrdinalIgnoreCase)
            );
            BaseAddress = (int)(unityPlayer?.BaseAddress ?? IntPtr.Zero);
            Clock.Start();
        }

        public void Update()
        {
            int player = Memory.ReadPointerChain<int>(BaseAddress, PLAYER);

            MaxFeathers = Memory.ReadPointer<int>(player, MAX_FEATHERS);

            var newIsGrounded = Memory.ReadPointer<bool>(player, IS_GROUNDED);
            if (newIsGrounded != IsGrounded && !FlightIsOnCooldown)
            {
                if (!newIsGrounded)
                {
                    FlightStopwatch.Reset();
                    FlightStopwatch.Start();
                }
                else if (FlightStopwatch.IsRunning)
                {
                    FlightStopwatch.Stop();
                    LastFlightEnd = (int) Clock.ElapsedMilliseconds;
                }
            }
            IsGrounded = newIsGrounded;

            if (FlightStopwatch.IsRunning)
            {
                FlightDuration = new TimeSpan(FlightStopwatch.ElapsedMilliseconds * 10000);
            }
        }

        public bool FlightIsOnCooldown
        {
            get {
                return Clock.ElapsedMilliseconds <= LastFlightEnd + FLIGHT_COOLDOWN;
            }
        }
    }
}
