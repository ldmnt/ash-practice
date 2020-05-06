using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ash_practice
{
    class Program
    {
        private static readonly int FRAMERATE = 60;
        private static readonly string PROC_NAME = "AShortHike";

        static void Main(string[] args)
        {
            if (Process.GetProcessesByName(PROC_NAME).Length > 0)
            {
                var process = Process.GetProcessesByName(PROC_NAME)[0];
                Console.WriteLine("Found process with id : " + process.Id.ToString());
                var gameState = new GameState(process);

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                long frameInterval = (long) (1.0d / FRAMERATE * 1000);
                while (true)
                {
                    var nextFrameTime = stopwatch.ElapsedMilliseconds + frameInterval;
                    gameState.Update();
                    long elapsed = stopwatch.ElapsedMilliseconds;
                    if (elapsed < nextFrameTime)
                    {
                        Thread.Sleep((int) (nextFrameTime - elapsed));
                    }
                }
            }
        }
    }
}
