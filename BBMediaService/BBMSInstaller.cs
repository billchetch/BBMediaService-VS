using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chetch.Services;
using System.Configuration.Install;
using System.ComponentModel;

namespace BBMediaService
{

    [RunInstaller(true)]
    public class BBMSInstaller : ServiceInstaller
    {
        public BBMSInstaller() : base("BBMediaService",
                                    "Bulan Baru Media Service",
                                    "Runs a Chetch Messaging client and an ADM instance for media comms to the Arduino Board")
        {
            //empty
        }
    }
}
