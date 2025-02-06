// Example service implementation in TopShelfProject

using System;
using System.IO;
using System.Timers;
using Topshelf;


namespace TopShelfProject
{
    public class TimerService
    {
        private readonly System.Timers.Timer _timer; // Tell the compiler you're using System.Timers.Timer

        public TimerService()
        {
            // Set up a timer to trigger every 5 seconds
            _timer = new System.Timers.Timer(5000); // Timer interval in milliseconds (5 seconds)
            _timer.Elapsed += PerformAction; // Define what action to perform on timer trigger
            _timer.AutoReset = true; // Enable repeated events
        }

        public void Start()
        {
            Console.WriteLine("Service starting...");
            _timer.Start();
        }

        public void Stop()
        {
            Console.WriteLine("Service stopping...");
            _timer.Stop();
        }

        private void PerformAction(object sender, ElapsedEventArgs e)
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestClientServiceLog.txt");

            File.AppendAllText(filePath, $"{DateTime.Now}: Service is running.{Environment.NewLine}");

            Console.WriteLine("Current time written to file.");
        }
    }
}
