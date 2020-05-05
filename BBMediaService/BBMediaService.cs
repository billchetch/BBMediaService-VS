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
        IRGenericReceiver _irr1;

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
                throw e;
            }
        }

        override protected void AddADMDevices(ADMMessage message)
        {
            int irTransmitPin = message.GetInt("IRTransmitPin");
            int irReceivePin = message.GetInt("IRReceivePin");

            _irt1 = new IRGenericTransmitter("irt1", "Generic IR Transmitter", 5, irTransmitPin, _irdb);
            ADM.AddDevice(_irt1);

            _irr1 = new IRGenericReceiver("irr1", "Generic IR Receiver", irReceivePin, _irdb);
            ADM.AddDevice(_irr1);
        }

        public override void AddCommandHelp(List<string> commandHelp)
        {
            base.AddCommandHelp(commandHelp);

            commandHelp.Add("list-transmitters:  Lists all IR transmitters in the IR database");
            commandHelp.Add("set-transmitter:  Set generic transmitter to use commands for <database id>");
            commandHelp.Add("set-receiver:  Set generic receiver to save commands for <database id>");
            commandHelp.Add("start-recording:  Start recording IR input for <command>");
            commandHelp.Add("stop-recording:  Stop recording IR input");
            commandHelp.Add("list-ircodes:  List IR codes so far processed");
            commandHelp.Add("save-ircodes:  Save recorded IR codes (can add <uknown?> to process unkonwn commands)");
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
                case "set-receiver":
                    if (args.Count == 0)
                    {
                        throw new Exception("No database id provided");
                    }
                    var id = System.Convert.ToInt64(args[0]);

                    foreach (var dev in _irdb.SelectDevices())
                    {
                        if (dev.ID == id)
                        {
                            var name = dev["device_name"].ToString();
                            if (cmd == "set-transmitter")
                            {
                                _irt1.Name = name;
                                response.AddValue("GenericTransmitter", _irt1.Name);
                            }
                            else
                            {
                                _irr1.Name = name;
                                response.AddValue("GenericReceiver", _irr1.Name);
                            }
                            break;
                        }
                    }
                    return true;

                case "start-recording":
                    if (args.Count == 0)
                    {
                        throw new Exception("No command name provided");
                    }
                    if (!_irr1.IsInDB)
                    {
                        throw new Exception(String.Format("Generic Receiver has name {0} which is not in database {1}", _irr1.Name, _irdb.DBName));
                    }

                    _irr1.ExecuteCommand("Start", args.ToList());
                    response.AddValue("IRCommand", _irr1.IRCommandName);
                    return true;

                case "stop-recording":
                case "list-ircodes":
                    if (cmd == "stop-recording")
                    {
                        _irr1.ExecuteCommand("Stop");
                    }
                    response.AddValue("IRCommand", _irr1.IRCommandName);
                    response.AddValue("IRCodes", _irr1.IRCodes.Select(i => i.ToString()).ToList());
                    response.AddValue("UnknownIRCodes", _irr1.UnknownIRCodes.Select(i => i.ToString()).ToList());
                    return true;

                case "save-ircodes":
                    if (!_irr1.IsInDB)
                    {
                        throw new Exception(String.Format("{0} is not in database {1}", _irr1.Name, _irdb.DBName));
                    }

                    if (_irr1.UnknownIRCodes.Count == 1 && args.Count > 0)
                    {
                        _irr1.processUnknownCode(args[0].ToString());
                        response.AddValue("IRCommand", _irr1.IRCommandName);
                        response.AddValue("IRCodes", _irr1.IRCodes.Select(i => i.ToString()).ToList());
                        return true;
                    }

                    _irr1.ExecuteCommand("Save");
                    response.AddValue("IRCommand", _irr1.IRCommandName);
                    response.AddValue("IRCodes", _irr1.IRCodes.Select(i => i.ToString()).ToList());
                    return true;
            }

            return base.HandleCommand(cnn, message, cmd, args, response);
        }
    } // End class
}
