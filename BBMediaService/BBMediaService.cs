using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chetch.Services;
using Chetch.Arduino;
using Chetch.Arduino.Devices.Infrared;
using Chetch.Messaging;
using System.Diagnostics;

namespace BBMediaService
{
    class BBMediaService : ADMService
    {
        IRGenericTransmitter _irt;
        IRGenericReceiver _irr;

        IRSamsungTV _sstv;
        IRLGHomeTheater _lght1;
        IRLGHomeTheater _lght2;

        private IRDB _irdb;

        public BBMediaService() : base("BBMS", "BBMSClient", "BBMediaService", "BBMediaServiceLog")
        {
            SupportedBoards = ArduinoDeviceManager.DEFAULT_BOARD_SET;
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

        override protected void AddADMDevices(ArduinoDeviceManager adm, ADMMessage message)
        {
            _irt = new IRGenericTransmitter("irt", "IRT", 3, ArduinoPin.BOARD_SPECIFIED, _irdb);
            adm.AddDevice(_irt);

            _irr = new IRGenericReceiver("irr", "IRR", 4, _irdb);
            adm.AddDevice(_irr);

            _sstv = new IRSamsungTV("sstv", 5, ArduinoPin.BOARD_SPECIFIED, _irdb);
            adm.AddDevice(_sstv);

            _lght1 = new IRLGHomeTheater("lght1", 6, ArduinoPin.BOARD_SPECIFIED, _irdb);
            adm.AddDevice(_lght1);

            _lght2 = new IRLGHomeTheater("lght2", 7, ArduinoPin.BOARD_SPECIFIED, _irdb);
            adm.AddDevice(_lght2);
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
                                _irt.DeviceName = name;
                                response.AddValue("GenericTransmitter", _irt.DeviceName);
                            }
                            else
                            {
                                _irr.DeviceName = name;
                                response.AddValue("GenericReceiver", _irr.DeviceName);
                            }
                            break;
                        }
                    }
                    return true;

                case "start-recording":
                    if (!_irr.IsConnected) throw new Exception(String.Format("Command {0} cannot be executed because receiver is not connected", cmd));

                    if (args.Count == 0)
                    {
                        throw new Exception("No command name provided");
                    }
                    if (!_irr.IsInDB)
                    {
                        throw new Exception(String.Format("Generic Receiver has name {0} which is not in database {1}", _irr1.Name, _irdb.DBName));
                    }

                    _irr.ExecuteCommand("Start", args.ToList());
                    response.AddValue("IRCommand", _irr.IRCommandName);
                    return true;

                case "stop-recording":
                case "list-ircodes":
                    if (cmd == "stop-recording")
                    {
                        _irr.ExecuteCommand("Stop");
                    }
                    response.AddValue("IRCommand", _irr.IRCommandName);
                    response.AddValue("IRCodes", _irr.IRCodes.Select(i => i.ToString()).ToList());
                    response.AddValue("UnknownIRCodes", _irr.UnknownIRCodes.Select(i => i.ToString()).ToList());
                    return true;

                case "save-ircodes":
                    if (!_irr.IsInDB)
                    {
                        throw new Exception(String.Format("{0} is not in database {1}", _irr.DeviceName, _irdb.DBName));
                    }

                    if (_irr.UnknownIRCodes.Count == 1 && args.Count > 0)
                    {
                        _irr.processUnknownCode(args[0].ToString());
                        response.AddValue("IRCommand", _irr.IRCommandName);
                        response.AddValue("IRCodes", _irr.IRCodes.Select(i => i.ToString()).ToList());
                        return true;
                    }

                    _irr.ExecuteCommand("Save");
                    response.AddValue("IRCommand", _irr.IRCommandName);
                    response.AddValue("IRCodes", _irr.IRCodes.Select(i => i.ToString()).ToList());
                    return true;
            }

            return base.HandleCommand(cnn, message, cmd, args, response);
        }
    } // End class
}
