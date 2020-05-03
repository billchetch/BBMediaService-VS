using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chetch.Services;
using Chetch.Arduino;
using Chetch.Arduino.Infrared;
using Chetch.Messaging;
using System.Diagnostics;

namespace BBMediaService
{
    class BBMediaService : ADMService
    {
        IRGenericTransmitter _irt1;
        private IRDB _irdb;

        public BBMediaService() : base("BBMS", "BBMSClient", "BBMediaService", "BBMediaServiceLog")
        {
            SupportedBoards = Properties.Settings.Default.SupportedBoards;
            try
            {
                Tracing?.TraceEvent(TraceEventType.Information, 0, "Connecting to IR database...");
                _irdb = IRDB.Create(Properties.Settings.Default, IRDB.IREncoding.HEX);
                Tracing?.TraceEvent(TraceEventType.Information, 0, "Connected to IR database");
            }
            catch (Exception e)
            {
                Tracing?.TraceEvent(TraceEventType.Error, 0, e.Message);
            }
        }

        override protected void AddADMDevices(ADMMessage message)
        {
            int irTransmitPin = message.GetInt("IRTransmitPin");
            _irt1 = new IRGenericTransmitter("irt1", "Generic IR Transmitter", 5, irTransmitPin, _irdb);
            ADM.AddDevice(_irt1);
        }

        public override void AddCommandHelp(List<string> commandHelp)
        {
            base.AddCommandHelp(commandHelp);

            commandHelp.Add("list-transmitters:  Lists all IR transmitters in the IR database");
            commandHelp.Add("set-transmitter:  Set generic transmitter to use commands for <database id>");
        }

        override public bool HandleCommand(Connection cnn, Message message, String cmd, List<Object> args, Message response)
        {
            switch (cmd)
            {
                case "list-transmitters":
                    var devs = _irdb.SelectDevices();
                    var l = devs.Select(i => String.Format("{0}: {1} - {2} - {3}", i.ID, i["device_name"], i["device_type"], i["manufacturer"])).ToList();
                    response.AddValue("Transmitters", l);
                    response.AddValue("GenericTransmitter", _irt1.Name);
                    return true;

                case "set-transmitter":
                    if (args.Count == 0)
                    {
                        throw new Exception("No transmitter id provided");
                    }
                    var id = System.Convert.ToInt64(args[0]);
                    foreach (var dev in _irdb.SelectDevices())
                    {
                        if (dev.ID == id)
                        {
                            _irt1.Name = dev["device_name"].ToString();
                            break;
                        }
                    }
                    return true;
            }

            return base.HandleCommand(cnn, message, cmd, args, response);
        }
    } // End class
}
