using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Pipes;
using Chetch.Arduino;
using Chetch.Arduino.Infrared;
using Chetch.Utilities;
using Chetch.Services;

namespace BBMediaService
{
    public partial class BBMediaService : ArduinoService
    {
        public BBMediaService() : base("BBMS")
        {
            InitializeComponent();
            ServiceName = GetType().Name;
            Log.Source = "BB Media Service Class";
            Log.Log = "BB Media Service";
            SupportedBoards = ArduinoDeviceManager.ARDUINO_MEGA_2560;
        }

        override protected void OnMessageReceived(NamedPipeManager.Message message)
        {
            Log.WriteInfo("Received: " + message.Value);
        }
    }
}
