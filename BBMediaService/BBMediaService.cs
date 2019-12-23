using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace BBMediaService
{
    public partial class BBMediaService : ServiceBase
    {
        public BBMediaService()
        {
            InitializeComponent();

            //specify log info
            eventLog.Source = "BBMediaService Class";
            eventLog.Log = "BB Media Service";
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                eventLog.WriteEntry("Serivce started", EventLogEntryType.Information);
            } catch (Exception e)
            {
                eventLog.WriteEntry(e.Message, EventLogEntryType.Error);
            }
        }

        protected override void OnStop()
        {
            eventLog.WriteEntry("Serivce stopped", EventLogEntryType.Information);
        }
    }
}
