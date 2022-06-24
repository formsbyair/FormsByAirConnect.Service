using System.ServiceProcess;

namespace FormsByAirConnect.Service
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new FormsByAirConnect()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
