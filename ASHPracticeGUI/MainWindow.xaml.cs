using ASHDebug;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ASHPracticeGUI
{
    public class DebugEngine
    {
        private static readonly string PROC_NAME = "AShortHike";
        public static readonly int FRAMERATE = 60;

        public GameState GameState;
        private Process GameProcess;
        private DebugLoop DebugLoop;
        private Thread Thread;
        private MainWindow Window;
        private bool IsAttached;

        public DebugEngine(MainWindow window)
        {
            Window = window;
            IsAttached = false;
        }

        public bool Attach()
        {
            if (Thread != null)
            {
                Thread.Abort();
            }

            GameProcess = ProcessUtil.FindByName(PROC_NAME);
            IsAttached = (GameProcess != null);
            if (IsAttached)
            {
                GameState = new GameState(GameProcess);
                DebugLoop = new DebugLoop(FRAMERATE, GameState, AfterUpdate);
                Thread = new Thread(new ThreadStart(DebugLoop.Run));
            }
            return IsAttached;
        }

        public void Start()
        {
            Thread?.Start();
        }

        public void Stop()
        {
            Thread?.Abort();
        }

        public void AfterUpdate()
        {
            Window.TimeTrial.Update();
            Window.Dispatcher.BeginInvoke((Action)(() => Window.UpdateData()));
        }
    }

    public partial class MainWindow : Window
    {
        public static readonly int LOW_FRAMERATE = 10;

        public TimeTrial TimeTrial;

        private DebugEngine Engine;
        private short FrameCountdown = 1;
        private short RefreshPeriod;    // number of debug frames between each refresh

        public MainWindow()
        {
            InitializeComponent();
            Engine = new DebugEngine(this);
            RefreshPeriod = (short)((float)DebugEngine.FRAMERATE / LOW_FRAMERATE);
        }

        private void AttachButtonClick(object sender, RoutedEventArgs e)
        {
            if (Engine.Attach())
            {
                Engine.Start();
                TimeTrial = new TimeTrial(Engine.GameState);
                ButtonSetStart.IsEnabled = true;
                ButtonSetEnd.IsEnabled = true;
                ButtonResetAll.IsEnabled = true;
            }
            else
            {
                MessageBox.Show("Could not find a running A Short Hike process...");
            }
        }

        public void UpdateData()
        {
            var gameState = Engine.GameState;
            Grounded.Visibility = gameState.IsGrounded ? Visibility.Visible : Visibility.Hidden;
            Climbing.Visibility = gameState.IsClimbing ? Visibility.Visible : Visibility.Hidden;
            Gliding.Visibility = gameState.IsGliding ? Visibility.Visible : Visibility.Hidden;
            Swimming.Visibility = gameState.IsSwimming ? Visibility.Visible : Visibility.Hidden;

            FlightCooldown.Visibility = gameState.FlightIsOnCooldown ? Visibility.Visible : Visibility.Hidden;
            FlightTime.Text = gameState.FlightDuration.ToString("ss','fff");
            GlideTime.Text = gameState.GlideDuration.ToString("ss','fff");
            TimeSpan setupTime = gameState.FlightDuration - gameState.GlideDuration;
            SetupTime.Text = setupTime.ToString("ss','fff");

            CurrentTime.Text = TimeTrial.CurrentTime.ToString("ss','fff");
            TimeSpan? delta = TimeTrial.Delta;
            if (delta == null)
            {
                Delta.Text = "";
            }
            else
            {
                string sign = delta > TimeSpan.Zero ? "+" : "-";
                Delta.Text = sign + delta?.ToString("ss','fff");
            }
            BestTime.Text = TimeTrial.BestTime?.ToString("ss','fff");

            FrameCountdown -= 1;
            if (FrameCountdown <= 0)
            {
                FrameCountdown = RefreshPeriod;

                // values with low refresh rate for readability
                MaxAltitude.Text = gameState.MaxAltitude.ToString("F2");
                MaxHorSpeed.Text = gameState.MaxHorizontalSpeed.ToString("F2");

                PositionX.Text = gameState.Position.X.ToString("F2");
                PositionY.Text = gameState.Position.Z.ToString("F2");
                Altitude.Text = gameState.Position.Y.ToString("F2");

                HorizontalSpeed.Text = gameState.HorizontalSpeed.ToString("F2");
                VerticalSpeed.Text = gameState.Velocity.Y.ToString("F2");
            }
        }

        private void OnWindowClosed(object sender, CancelEventArgs e)
        {
            Engine.Stop();
        }

        private void OnTopCheckBoxChecked(object sender, RoutedEventArgs e)
        {
            this.Topmost = true;
        }

        private void OnTopCheckBoxUnchecked(object sender, RoutedEventArgs e)
        {
            this.Topmost = false;
        }

        private void ButtonSetStart_Click(object sender, RoutedEventArgs e)
        {
            TimeTrial.StartPosition = Engine.GameState.Position;
            ButtonSetStart.Content = "ack";
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            timer.Start();
            timer.Tick += (sender_, args) =>
            {
                timer.Stop();
                ButtonSetStart.Content = "set start";
            };
        }

        private void ButtonSetEnd_Click(object sender, RoutedEventArgs e)
        {
            TimeTrial.EndPosition = Engine.GameState.Position;
            ButtonSetEnd.Content = "ack";
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            timer.Start();
            timer.Tick += (sender_, args) =>
            {
                timer.Stop();
                ButtonSetEnd.Content = "set end";
            };
        }

        private string FormatVector(ASHDebug.Vector v)
        {
            return String.Format(
                "[{0}, {1}, {2}]",
                v.X.ToString("F0"),
                v.Z.ToString("F0"),
                v.Y.ToString("F0"));
        }

        private void ButtonResetAll_Click(object sender, RoutedEventArgs e)
        {
            TimeTrial = new TimeTrial(Engine.GameState);
            ButtonResetAll.Content = "ack";
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            timer.Start();
            timer.Tick += (sender_, args) =>
            {
                timer.Stop();
                ButtonResetAll.Content = "reset all";
            };
        }
    }

    public class TimeTrial
    {
        public static float TRIGGER_DISTANCE = 2.0f;

        public TimeSpan CurrentTime { get; private set; }
        public TimeSpan? BestTime { get; private set; }
        public TimeSpan? Delta { get; private set; }

        private GameState GameState;
        private Stopwatch Timer = new Stopwatch();
        private byte TimeTrialState = 2;   // 1: at start, 2: at end, 0: neither

        private bool StartSet = false;
        private bool EndSet = false;
        private ASHDebug.Vector _startPositon;
        private ASHDebug.Vector _endPositon;

        public ASHDebug.Vector StartPosition
        {
            get => _startPositon;
            set
            {
                _startPositon = value;
                HardReset();
                StartSet = true;
            }
        }

        public ASHDebug.Vector EndPosition
        {
            get => _endPositon;
            set
            {
                _endPositon = value;
                TimeTrialState = 2;
                HardReset();
                EndSet = true;
            }
        }

        public TimeTrial(GameState gameState)
        {
            GameState = gameState;
        }

        public void Update()
        {
            if (!StartSet || !EndSet) { return; }

            byte newState = CurrentState(GameState.Position);
            if (TimeTrialState != 1 && newState == 1)
            {
                Reset();
            } 
            if (TimeTrialState != 2 && newState == 2 && Timer.IsRunning)
            {
                Stop();
            }
            if (TimeTrialState == 1 && newState == 0)
            {
                Start();
            }
            TimeTrialState = newState;

            CurrentTime = new TimeSpan(Timer.ElapsedMilliseconds * 10000);
        }

        private void Start()
        {
            Timer.Start();
        }

        private void Stop()
        {
            Timer.Stop();
            CurrentTime = new TimeSpan(Timer.ElapsedMilliseconds * 10000);
            Delta = BestTime == null ? null : CurrentTime - BestTime;
            if (BestTime == null || BestTime > CurrentTime)
            {
                BestTime = CurrentTime;
            }
        }

        private void Reset()
        {
            Delta = null;
            Timer.Reset();
        }

        private void ResetBest()
        {
            BestTime = null;
        }

        public void HardReset()
        {
            Reset();
            ResetBest();
        }

        private byte CurrentState(ASHDebug.Vector position)
        {
            if (position.Sub(StartPosition).Length() < TRIGGER_DISTANCE)
            {
                return 1;
            }
            if (position.Sub(EndPosition).Length() < TRIGGER_DISTANCE)
            {
                return 2;
            }
            return 0;
        }
    }
}
