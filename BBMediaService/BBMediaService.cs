using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Chetch.Arduino;
using Chetch.Arduino.Infrared;

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
                //Connect to arduino board
                String supportedBoards = Properties.Settings.Default.SupportedBoards;
                var dmgr = ArduinoDeviceManager.Connect(supportedBoards);
                if (dmgr == null) throw new Exception("Unable to connect to " + supportedBoards);

                //database settings
                String server = Properties.Settings.Default.DBServer;
                String username = Properties.Settings.Default.DBUsername;
                String password = Properties.Settings.Default.DBPassword;
                password = Chetch.Utilities.BasicEncryption.Decrypt(password, "dbpasswd");

                //Infrared stuff
                var irdb = new IRDB(server, Properties.Settings.Default.IRDBName, username, password);
                int transmitPin = 9; //(this is for mega ...TODO: derive from board
                var homeTheater1 = new IRLGHomeTheater(3, transmitPin, irdb);
                var homeTheater2 = new IRLGHomeTheater(4, transmitPin, irdb);
                var tv = new IRSamsungTV(5, transmitPin, irdb);
                
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
