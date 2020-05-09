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
            Window.Dispatcher.BeginInvoke((Action)(() => Window.UpdateData()));
        }
    }

    public partial class MainWindow : Window
    {
        public static readonly int LOW_FRAMERATE = 10;

        private DebugEngine Engine;
        private short FrameCountdown = 1;
        private short RefreshPeriod;    // number of debug frames between each refresh

        public MainWindow()
        {
            InitializeComponent();
            Engine = new DebugEngine(this);
            RefreshPeriod = (short) ((float) DebugEngine.FRAMERATE / LOW_FRAMERATE);
        }

        private void AttachButtonClick(object sender, RoutedEventArgs e)
        {
            if (Engine.Attach())
            {
                Engine.Start();
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
            FlightTime.Text = gameState.FlightDuration.ToString("ss'.'fff");
            GlideTime.Text = gameState.GlideDuration.ToString("ss'.'fff");
            TimeSpan setupTime = gameState.FlightDuration - gameState.GlideDuration;
            SetupTime.Text = setupTime.ToString("ss'.'fff");

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
    }
}
