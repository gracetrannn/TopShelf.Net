// See https://aka.ms/new-console-template for more information


using System;
using Topshelf;
using TopShelfProject;
namespace MyConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(config =>
            {
                config.Service<TimerService>(service =>
                {
                    service.ConstructUsing(name => new TimerService());
                    service.WhenStarted(tc => tc.Start());
                    service.WhenStopped(tc => tc.Stop());
                });

                config.RunAsLocalSystem(); // Run the service as the local system user

                // Configure metadata about the service
                config.SetServiceName("TestClientService");
                config.SetDisplayName("Test Client Service");
                config.SetDescription("A service that writes the current time to a file every 5 seconds using .NET 8.");
            });
        }
    }
}
/*
using System;
    using Topshelf;

    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(configure =>
            {
                configure.Service<MyService>(service =>
                {
                    service.ConstructUsing(name => new MyService());
                    service.WhenStarted(s => { s.Start(); return true; });
                    service.WhenStopped(s => { s.Stop(); return true; });
                });

                configure.RunAsLocalSystem(); // Run the service with system privileges
                configure.SetServiceName("MyWindowsService"); // Set the service name
                configure.SetDisplayName("My Windows Service"); // Set the display name
                configure.SetDescription("This is a sample .NET 8 Windows Service using TopShelf."); // Set the description
            });
        }
    }


*/
/*
 * 
Additional Implementation Notes
- Logging with NLog (or any other logging framework)
- Configuration Management
- Debugging Techniques
*/
