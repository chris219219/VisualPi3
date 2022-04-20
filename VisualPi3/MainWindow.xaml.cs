using Microsoft.UI.Xaml;
using Microsoft.UI.Dispatching;
using System;
using System.Threading.Tasks;

namespace VisualPi3
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Title = "Visual Pi 3";
            Closed += OnClose;

            _program = new();
            _program.Initialize();

            ButtonSave.IsEnabled = false;
            TextBoxGroupAmount.PlaceholderText = _program.GroupAmount.ToString();
            TextBoxThreads.PlaceholderText = _program.Threads.ToString();

            _timer = new DispatcherTimer();
            _timer.Tick += UpdatePerSec;
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Start();

            TextBoxLog.Text += "> Visual Pi 3 log started\n";
        }

        private readonly HexPi _program;
        private readonly DispatcherTimer _timer;

        private async void ButtonStartPause_Click(object sender, RoutedEventArgs e)
        {
            switch (_program.Paused)
            {
                case true:
                    ButtonStartPause.IsEnabled = false;
                    DisableOnStart();
                    ButtonStartPause.Content = "Pause";
                    long iteration = _program.Iteration;
                    await Task.Run(_program.Start);
                    TextBoxLog.Text += "> Started at iteration " + iteration + "\n";
                    ButtonStartPause.IsEnabled = true;
                    break;

                case false:
                    ButtonStartPause.IsEnabled = false;
                    ButtonStartPause.Content = "Start";
                    await Task.Run(_program.Pause);
                    EnableOnPause();
                    TextBoxLog.Text += "> Paused at iteration " + _program.Iteration + "\n";
                    ButtonStartPause.IsEnabled = true;
                    break;
            }
        }

        private async void ButtonLoad_Click(object sender, RoutedEventArgs e)
        {
            ButtonLoad.IsEnabled = false;
            bool success = await Task.Run(_program.ReadSavedState);
            if (success)
            {
                TextBoxLog.Text += "> Saved state successfully loaded\n";
                TextBoxGroupAmount.PlaceholderText = _program.GroupAmount.ToString();
                TextBoxThreads.PlaceholderText = _program.Threads.ToString();
            }
            else
                TextBoxLog.Text += "> Saved state could not be loaded\n";
        }

        private async void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            ButtonSave.IsEnabled = false;
            await Task.Run(_program.SaveState);
            TextBoxLog.Text += "> Saved current state\n";
            ButtonSave.IsEnabled = true;
        }

        private async void ButtonSetParams_Click(object sender, RoutedEventArgs e)
        {
            ButtonSetParams.IsEnabled = false;

            bool success = true;

            success &= int.TryParse(TextBoxGroupAmount.Text, out int groupAmount);
            if (!success)
            {
                TextBoxLog.Text += "> Entered Group Amount is not valid\n";
                ButtonSetParams.IsEnabled = true;
                return;
            }
                
            success &= int.TryParse(TextBoxThreads.Text, out int threads);
            if (!success)
            {
                TextBoxLog.Text += "> Entered Threads is not valid\n";
                ButtonSetParams.IsEnabled = true;
                return;
            }

            success &= await Task.Run(() => _program.SetGroupAmountAndThreads(groupAmount, threads));
            if (!success)
            {
                TextBoxLog.Text += "> Group Amount must be divisible by threads\n";
                ButtonSetParams.IsEnabled = true;
                return;
            }

            TextBoxGroupAmount.Text = string.Empty;
            TextBoxGroupAmount.PlaceholderText = _program.GroupAmount.ToString();
            TextBoxThreads.Text = string.Empty;
            TextBoxThreads.PlaceholderText = _program.Threads.ToString();
            TextBoxLog.Text += "> Group Amount = " + _program.GroupAmount + ", Threads = " + _program.Threads + "\n";

            ButtonSetParams.IsEnabled = true;
        }

        private void ButtonDisplay_Click(object sender, RoutedEventArgs e)
        {
            ButtonDisplay.IsEnabled = false;
            TextBoxDisplayHex.Text = _program.PiHex;
            ButtonDisplay.IsEnabled = true;
        }

        private async void ButtonClearSaved_Click(object sender, RoutedEventArgs e)
        {
            ButtonClearSaved.IsEnabled = false;
            await Task.Run(_program.ClearState);
            TextBoxLog.Text += "> Saved state cleared\n";
            ButtonClearSaved.IsEnabled = true;
        }

        private void DisableOnStart()
        {
            ButtonLoad.IsEnabled = false;
            ButtonSave.IsEnabled = false;
            ButtonSetParams.IsEnabled = false;
            ButtonDisplay.IsEnabled = false;
            ButtonClearSaved.IsEnabled = false;
        }

        private void EnableOnPause()
        {
            ButtonSave.IsEnabled = true;
            ButtonSetParams.IsEnabled = true;
            ButtonDisplay.IsEnabled = true;
            ButtonClearSaved.IsEnabled = true;
        }

        private void UpdatePerSec(object sender, object e)
        {
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                TextBlockHexDigits.Text = _program.Iteration.ToString();
                TextBlockDecDigits.Text = ((long)(_program.Iteration * 1.2)).ToString();
            });
        }

        private void OnClose(object sender, WindowEventArgs e)
        {
            _program.Stop();
        }
    }
}
