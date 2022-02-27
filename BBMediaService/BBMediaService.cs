using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chetch.Services;
using Chetch.Arduino2;
using Chetch.Arduino2.Devices.Infrared;
using Chetch.Messaging;
using System.Diagnostics;

namespace BBMediaService
{
    public class BBMediaService : ADMService
    {
        new public class MessageSchema : Chetch.Messaging.MessageSchema
        {
            public const String COMMAND_LIST_TRANSMITTERS = "list-transmitters";
            public const String COMMAND_SET_TRANSMITTER = "set-transmitter";
            public const String COMMAND_SET_RECEIVER = "set-receiver";
            public const String COMMAND_START_RECORDING = "start-recording";
            public const String COMMAND_STOP_RECORDING = "stop-recording";
            public const String COMMAND_LIST_IRCODES = "list-ircodes";
            public const String COMMAND_SAVE_IRCODES = "save-ircodes";
        }

        ArduinoDeviceManager _adm;
        IRGenericTransmitter _irt;
        IRGenericReceiver _irr;

        IRSamsungTV _sstv;
        IRLGHomeTheater _lght1;
        IRLGHomeTheater _lght2;
        
        
        private IRDB _irdb;

        public BBMediaService(bool test = false) : base("BBMedia", test ? null : "BBMSClient", "BBMediaService", test ? null : "BBMediaServiceLog")
        {
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

        protected override bool CreateADMs()
        {
            Tracing?.TraceEvent(TraceEventType.Information, 0, "Adding ADM and devices...");
            _adm = ArduinoDeviceManager.Create(ArduinoSerialConnection.BOARD_UNO, 115200, 64, 64);

            //Add generic devices ... can be used for testing or aqcuiring new ir codes
            _irr = new IRGenericReceiver("irr", "IRR", 8, _irdb);

            //Add specific devices
            _sstv = new IRSamsungTV("sstv", 4, _irdb);
            _lght1 = new IRLGHomeTheater("lght1", 5, _irdb);
            _lght2 = new IRLGHomeTheater("lght2", 6, _irdb);
            _adm.AddDevices(_irr, _sstv, _lght1, _lght2);

            AddADM(_adm);

            return true;
        }

        public override void AddCommandHelp()
        {
            base.AddCommandHelp();

            AddCommandHelp(MessageSchema.COMMAND_LIST_TRANSMITTERS, "Lists all IR transmitters in the IR database");
            AddCommandHelp(MessageSchema.COMMAND_SET_TRANSMITTER, "Set generic transmitter to use commands for <database id>");
            AddCommandHelp(MessageSchema.COMMAND_SET_RECEIVER, "Set generic receiver to save commands for <database id>");
            AddCommandHelp(MessageSchema.COMMAND_START_RECORDING, "Start recording IR input for <command>");
            AddCommandHelp(MessageSchema.COMMAND_STOP_RECORDING, "Stop recording IR input");
            AddCommandHelp(MessageSchema.COMMAND_LIST_IRCODES, "List IR codes so far processed");
            AddCommandHelp(MessageSchema.COMMAND_SAVE_IRCODES, "Save recorded IR codes (can add <uknown?> to process unkonwn commands)");
        }

        override public bool HandleCommand(Connection cnn, Message message, String cmd, List<Object> args, Message response)
        {
            switch (cmd)
            {
                case MessageSchema.COMMAND_LIST_TRANSMITTERS:
                    var devs = _irdb.SelectDevices();
                    var l = devs.Select(i => String.Format("{0}: {1} - {2} - {3}", i.ID, i["device_name"], i["device_type"], i["manufacturer"])).ToList();
                    response.AddValue("Transmitters", l);
                    return true;

                case MessageSchema.COMMAND_SET_TRANSMITTER:
                case MessageSchema.COMMAND_SET_RECEIVER:
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
                            if (cmd == MessageSchema.COMMAND_SET_TRANSMITTER)
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

                case MessageSchema.COMMAND_START_RECORDING:
                    if (!_irr.IsReady) throw new Exception(String.Format("Command {0} cannot be executed because receiver is not ready", cmd));

                    if (args.Count == 0)
                    {
                        throw new Exception("No command name provided");
                    }
                    if (!_irr.IsInDB)
                    {
                        throw new Exception(String.Format("Generic Receiver has name {0} which is not in database {1}", _irr.DeviceName, _irdb.DBName));
                    }

                    _irr.ExecuteCommand("Start", args[0]);
                    response.AddValue("IRCommand", _irr.IRCommandName);
                    return true;

                case MessageSchema.COMMAND_STOP_RECORDING:
                case MessageSchema.COMMAND_LIST_IRCODES:
                    if (cmd == MessageSchema.COMMAND_STOP_RECORDING)
                    {
                        _irr.ExecuteCommand("Stop");
                    }
                    response.AddValue("IRCommand", _irr.IRCommandName);
                    response.AddValue("IRCodes", _irr.IRCodes.Select(i => i.ToString()).ToList());
                    response.AddValue("UnknownIRCodes", _irr.UnknownIRCodes.Select(i => i.ToString()).ToList());
                    return true;

                case MessageSchema.COMMAND_SAVE_IRCODES:
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
