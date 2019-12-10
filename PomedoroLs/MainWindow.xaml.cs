using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;

namespace PomedoroLs
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly ViewModel vm;

        public MainWindow()
        {
            vm = new ViewModel();
            DataContext = vm;
            InitializeComponent();
        }

        void button_Click(object sender, RoutedEventArgs e)
        {
            vm.Start();
        }

        void button1_Click(object sender, RoutedEventArgs e)
        {
            vm.Status();
        }
    }

    public class ViewModel : INotifyPropertyChanged
    {
        static double _totalSecondsToWait;
        static int _elapsedSeconds;
        static bool restarting;
        readonly int balloonTime = 15000;
        readonly object lck = new object();
        SolidColorBrush background;
        Task currentTask;
        int progress;
        double progressFraction;
        public ObservableCollection<string> LogItems { get; set; } = new ObservableCollection<string>();

        public double ProgressFraction
        {
            get => progressFraction;
            set
            {
                progressFraction = value;
                OnPropertyChanged();
            }
        }

        public int Progress
        {
            get => progress;
            set
            {
                progress = value;
                OnPropertyChanged();
            }
        }

        public int PomedoroMins { get; set; } = 25;
        public int PomedoroPause { get; set; } = 5;

        public SolidColorBrush Background
        {
            get => background;
            set
            {
                background = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Start()
        {
            if (currentTask != null)
                restarting = true;

            currentTask = Task.Factory.StartNew(
                () =>
                {
                    Thread.CurrentThread.Name = "Pomedoro thread";
                    lock (lck)
                    {
                        _totalSecondsToWait = TimeSpan.FromMinutes(PomedoroMins).TotalSeconds;

                        restarting = false;

                        while (_elapsedSeconds - _totalSecondsToWait != 0 && !restarting)
                        {
                            Background = Brushes.Red;
                            var format = "Starting new Pomedoro " + TimeSpan.FromMinutes(PomedoroMins);
                            Wc(format);
                            Baloon("Attention", format, balloonTime);

                            for (var i = 0; i < _totalSecondsToWait; i++)
                            {
                                Thread.Sleep(500);
                                if (restarting)
                                {
                                    format = "Breaking this pomedoro";
                                    Wc(format);
                                    Baloon("Stopping", format, balloonTime);
                                    break;
                                }

                                Thread.Sleep(500);

                                _elapsedSeconds = i;

                                Application.Current.Dispatcher.BeginInvoke(new Action(
                                    () =>
                                    {
                                        Progress = (int) (_elapsedSeconds / _totalSecondsToWait * 100);
                                        ProgressFraction = _elapsedSeconds / _totalSecondsToWait;
                                    }));
                            }

                            if (!restarting)
                            {
                                Wc($"{TimeSpan.FromMinutes(PomedoroMins)} has passed, take a break.");
                                Baloon(TimeSpan.FromMinutes(PomedoroMins) + " has passed", "Pause Time!!", balloonTime);
                                Background = Brushes.Green;
                                var pauseSec = TimeSpan.FromMinutes(PomedoroPause).TotalSeconds;
                                for (var i = 0; i < pauseSec; i++)
                                {
                                    if (restarting)
                                        break;

                                    var i1 = i;
                                    Application.Current.Dispatcher.BeginInvoke(new Action
                                        (() => { Progress = (int) (i1 / pauseSec * 100); }));
                                    Thread.Sleep(1000);
                                }
                            }
                        }

                        Wc("Pomedoro ended");
                    }
                });
        }

        void Wc(string format)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => { LogItems.Add($"{DateTime.Now}: {format}"); }));
        }

        void Baloon(string balloonTipTitle, string balloonTipText, int balloonTime)
        {
            Task.Factory.StartNew(
                () =>
                {
                    var notification = new NotifyIcon
                    {
                        Visible = true,
                        Icon = SystemIcons.Exclamation,
                        BalloonTipIcon = ToolTipIcon.Info,
                        BalloonTipTitle = balloonTipTitle,
                        BalloonTipText = balloonTipText
                    };

                    // Display
                    notification.ShowBalloonTip(balloonTime);

                    // This will let the balloon close after it's timeout
                    Thread.Sleep(balloonTime);

                    // The notification should be disposed when you don't need it anymore,
                    // but doing so will immediately close the balloon if it's visible.
                    notification.Dispose();
                });
        }

        public void Status()
        {
            Task.Factory.StartNew(
                () =>
                {
                    var balloonTipText = "Time left: " + TimeSpan.FromSeconds(_totalSecondsToWait - _elapsedSeconds);
                    Wc(balloonTipText);
                    Baloon("Status", balloonTipText, balloonTime);
                });
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}