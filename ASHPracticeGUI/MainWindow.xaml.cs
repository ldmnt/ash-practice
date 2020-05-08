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
        private static readonly int FRAMERATE = 60;

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
        private DebugEngine Engine;

        public MainWindow()
        {
            InitializeComponent();
            Engine = new DebugEngine(this);
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
            GroundedValue.Text = gameState.IsGrounded.ToString();
            LastFlightValue.Text = gameState.FlightDuration.ToString("ss'.'fff");
            CooldownIndicator.Visibility = gameState.FlightIsOnCooldown ? Visibility.Visible : Visibility.Hidden;
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
