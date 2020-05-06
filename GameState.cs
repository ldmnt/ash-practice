using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ash_practice
{
    class GameState
    {
        private static string UNITYPLAYER_MODULE = "UnityPlayer.dll";
        private static readonly int[] PLAYER = { 0x109ac54, 0x38, 0x34, 0x3C, 0x44 };
        private static readonly int MAX_FEATHERS = 0x234;
        private static readonly int IS_GROUNDED = 0x44;

        public int MaxFeathers { get; private set; }
        public bool IsGrounded { get; private set; }
        public TimeSpan LastFlightTime { get; private set; }

        private Stopwatch Stopwatch = new Stopwatch();

        private Memory Memory;
        private int BaseAddress;

        public GameState(Process process)
        {
            var handle = WinAPI.OpenProcess(WinAPI.PROCESS_WM_READ, false, process.Id);
            Memory = new Memory(handle);

            var unityPlayer = process.Modules.Cast<ProcessModule>().SingleOrDefault(
                m => string.Equals(m.ModuleName, UNITYPLAYER_MODULE, StringComparison.OrdinalIgnoreCase)
            );
            BaseAddress = (int) (unityPlayer?.BaseAddress ?? IntPtr.Zero);
            Console.WriteLine("UnityPlayer.dll base address : 0x" + Convert.ToString(BaseAddress, 16));
        }

        public void Update()
        {
            int player = Memory.ReadPointerChain<int>(BaseAddress, PLAYER);

            var newMaxFeathers = Memory.ReadPointer<int>(player, MAX_FEATHERS);
            if (newMaxFeathers != MaxFeathers)
            {
                Console.WriteLine("Max feathers changed to " + newMaxFeathers.ToString());
            }
            MaxFeathers = newMaxFeathers;

            var newIsGrounded = Memory.ReadPointer<bool>(player, IS_GROUNDED);
            if (newIsGrounded != IsGrounded)
            {
                if (!newIsGrounded)
                {
                    Stopwatch.Reset();
                    Stopwatch.Start();
                }
                else
                {
                    Stopwatch.Stop();
                    LastFlightTime = new TimeSpan(Stopwatch.ElapsedMilliseconds * 10000);
                    Console.WriteLine("Last flight : " + LastFlightTime.ToString("ss'.'fff"));
                }
                Console.WriteLine("Grounded changed to " + newIsGrounded.ToString());
            }
            IsGrounded = newIsGrounded;
        }
    }
}
