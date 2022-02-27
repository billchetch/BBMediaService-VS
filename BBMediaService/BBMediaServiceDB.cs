using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chetch.Arduino2;
using Chetch.Database;

namespace BBMediaService
{
    public class BBMediaServiceDB : ADMServiceDB
    {
        static public new BBMediaServiceDB Create(System.Configuration.ApplicationSettingsBase settings, String dbnameKey = null)
        {
            BBMediaServiceDB db = dbnameKey != null ? DB.Create<BBMediaServiceDB>(settings, dbnameKey) : DB.Create<BBMediaServiceDB>(settings);
            return db;
        }
    }
}
