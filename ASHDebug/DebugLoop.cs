using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ASHDebug
{
    public class DebugLoop
    {
        private long FrameInterval;
        private Stopwatch Stopwatch = new Stopwatch();
        private GameState GameState;
        private Action AfterUpdate;

        public DebugLoop(int framerate, GameState gameState, Action afterUpdate = null)
        {
            FrameInterval = (long) (1.0d / framerate * 1000);
            GameState = gameState;
            AfterUpdate = afterUpdate;
        }

        public void Run()
        {
            Stopwatch.Start();
            while (true)
            {
                var nextFrameTime = Stopwatch.ElapsedMilliseconds + FrameInterval;
                GameState.Update();
                AfterUpdate?.Invoke();
                long elapsed = Stopwatch.ElapsedMilliseconds;
                if (elapsed < nextFrameTime)
                {
                    Thread.Sleep((int) (nextFrameTime - elapsed));
                }
            }
        }
    }
}
