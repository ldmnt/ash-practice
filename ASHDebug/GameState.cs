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
        private static readonly int[] PLAYER = { 0x1293454, 0x64, 0x54, 0x44 };
        private static readonly int[] PLAYER_VELOCITY_OFFSETS = { 0x10, 0x8, 0x34, 0x11C };
        private static readonly int[] PLAYER_POSITION_OFFSET_OFFSETS = { 0x8, 0x1C, 0x1C, 0x4, 0x24 };
        private static readonly int[] PLAYER_POSITION_OFFSETS = { 0x8, 0x1C, 0x1C, 0x4, 0x20, 0x10 };
        private static readonly int MAX_FEATHERS = 0x254;
        private static readonly int IS_GROUNDED = 0x44;
        private static readonly int IS_CLIMBING = 0x214;
        private static readonly int IS_FACING_WALL = 0x250;     // probably more of a "is facing something"
        private static readonly int PULLING_AGAINST = 0xD8;     // pointer that is only non null when pulling against a fish
        private static readonly int GLIDE_START_COUNTDOWN = 0x238;  // counts down trying to glide, glide at 0, then keeps going negative until reset
        private static readonly int WATER_Y = 0x210;    // water surface altitude, -1000 when not in water

        public Vector Position;
        public Vector Velocity;
        public float HorizontalSpeed;

        public bool IsGrounded { get; private set; }
        public bool IsClimbing { get; private set; }
        public bool IsFacingWall { get; private set; }
        public bool PullingAgainst { get; private set; }
        public float GlideStartCountdown { get; private set; }
        public float WaterY { get; private set; }

        public int MaxFeathers { get; private set; }

        public TimeSpan FlightDuration { get; private set; }
        public TimeSpan GlideDuration { get; private set; }
        public float MaxHorizontalSpeed { get; private set; }
        public float MaxAltitude { get; private set; }

        private Stopwatch FlightStopwatch = new Stopwatch();
        private Stopwatch GlideStopwatch = new Stopwatch();
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

        public bool IsFlying
        {
            get
            {
                return IsFlyingOrClimbing && !IsClimbing;
            }
        }

        public bool IsFlyingOrClimbing
        {
            get
            {
                return !IsGrounded && !IsInWater;
            }
        }

        public bool IsGliding
        {
            get
            {
                return GlideStartCountdown <= 0.0 && !IsFacingWall && !IsSwimming && !PullingAgainst;
            }
        }

        public bool IsSwimming
        {
            get
            {
                return !IsGrounded && IsInWater && !IsClimbing;
            }
        }

        public bool IsInWater
        {
            get
            {
                return WaterY >= 0;
            }
        }

        public void Update()
        {
            bool oldIsFlyingOrClimbing = IsFlyingOrClimbing;

            int player = Memory.ReadPointerChain<int>(BaseAddress, PLAYER);

            IsGrounded = Memory.ReadPointer<bool>(player, IS_GROUNDED);
            IsClimbing = Memory.ReadPointer<bool>(player, IS_CLIMBING);
            IsFacingWall = Memory.ReadPointer<bool>(player, IS_FACING_WALL);
            GlideStartCountdown = Memory.ReadPointer<float>(player, GLIDE_START_COUNTDOWN);
            WaterY = Memory.ReadPointer<float>(player, WATER_Y);
            PullingAgainst = (Memory.ReadPointer<int>(player, PULLING_AGAINST) != 0);

            Velocity = Memory.ReadPointerChain<Vector>(player, PLAYER_VELOCITY_OFFSETS);
            HorizontalSpeed = (float) Math.Sqrt(Math.Pow(Velocity.X, 2) + Math.Pow(Velocity.Z, 2));

            int position_pointer = Memory.ReadPointerChain<int>(player, PLAYER_POSITION_OFFSETS);
            int position_offset = 48 * Memory.ReadPointerChain<int>(player, PLAYER_POSITION_OFFSET_OFFSETS);
            Position = Memory.ReadPointer<Vector>(position_pointer, position_offset);

            MaxFeathers = Memory.ReadPointer<int>(player, MAX_FEATHERS);

            if (oldIsFlyingOrClimbing != IsFlyingOrClimbing && !FlightIsOnCooldown)
            {
                if (IsFlyingOrClimbing)
                {
                    FlightStopwatch.Reset();
                    GlideStopwatch.Reset();
                    MaxAltitude = Position.Y;
                    MaxHorizontalSpeed = HorizontalSpeed;
                    FlightStopwatch.Start();
                }
                else if (FlightStopwatch.IsRunning)
                {
                    FlightStopwatch.Stop();
                    GlideStopwatch.Stop();
                    LastFlightEnd = (int)Clock.ElapsedMilliseconds;
                }
            }

            if (FlightStopwatch.IsRunning)
            {
                if (MaxAltitude < Position.Y)
                {
                    MaxAltitude = Position.Y;
                }
                if (MaxHorizontalSpeed < HorizontalSpeed)
                {
                    MaxHorizontalSpeed = HorizontalSpeed;
                }
                if (IsGliding && !GlideStopwatch.IsRunning)
                {
                    GlideStopwatch.Start();
                }
                if (!IsGliding && GlideStopwatch.IsRunning)
                {
                    GlideStopwatch.Stop();
                }
                FlightDuration = new TimeSpan(FlightStopwatch.ElapsedMilliseconds * 10000);
                GlideDuration = new TimeSpan(GlideStopwatch.ElapsedMilliseconds * 10000);
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
