using FormsByAir.SDK;
using FormsByAir.SDK.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Timers;

namespace FormsByAirConnect.Service
{
    public partial class FormsByAirConnect : ServiceBase
    {
        public FormsByAirConnect()
        {
            InitializeComponent();
        }

        private Timer timer = new Timer();
        private FormsByAirClient formsbyair;

        protected override void OnStart(string[] args)
        {
            //Debugger.Launch();

            formsbyair = new FormsByAirClient(ConfigurationManager.AppSettings["apiKey"]);

            timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            timer.Interval = Convert.ToInt32(ConfigurationManager.AppSettings["intervalSeconds"]) * 1000;
            timer.Enabled = true;
            timer.Start();
        }

        protected override void OnStop()
        {
            timer.Stop();
        }

        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //TODO: Will need to "lock" documentDelivery while processing to allow mult-threaded operation
            timer.Stop();

            try

            {
                List<FileDelivery> files = null;
                var deliveries = formsbyair.GetPendingDocumentDeliveries();

                foreach (var delivery in deliveries)
                {
                    try
                    {
                        var document = formsbyair.GetDocument(delivery.DocumentId);

                        files = new List<FileDelivery>();
                        var fileDeliveries = formsbyair.GetFileDeliveriesForDocument(delivery.DocumentId);
                        if (fileDeliveries != null && fileDeliveries.Any())
                        {
                            foreach (var fileDelivery in fileDeliveries)
                            {
                                files.Add(formsbyair.GetFile(fileDelivery));
                            }                            
                        }

                        //TODO: Upgrade this to DI framework when/if needs become more complex
                        var connectorName = ConfigurationManager.AppSettings["connector"];
                        var connectorNamespace = "FormsByAir.Connectors." + connectorName;                        
                        var type = Type.GetType(connectorNamespace + "." + connectorName + "Client, " + connectorNamespace);
                        var connector = (BaseClient)Activator.CreateInstance(type, formsbyair, delivery.SubscriptionId);

                        connector.Deliver(document.Form, files);
                        formsbyair.SetDelivered(delivery.DocumentDeliveryId);
                        EventLog.WriteEntry("FormsByAirConnect", "Processed " + delivery.DocumentDeliveryId, EventLogEntryType.Information);
                    }
                    catch (Exception ex)
                    {
                        EventLog.WriteEntry("FormsByAirConnect", delivery.DocumentDeliveryId + ex.Message + ex.StackTrace, EventLogEntryType.Error);
                        formsbyair.LogDeliveryException(delivery.DocumentDeliveryId, ex.Message, ex.StackTrace);
                    }
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("FormsByAirConnect", ex.Message + ex.StackTrace, EventLogEntryType.Error);
            }

            timer.Start();
        }

    }
}