using ConfigurationLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDNS_IP_Notice_Tool
{
    public class Config : ConfigBase
    {
        public string UserID { get; set; }
        public string Password { get; set; }

        public Config() : base("Config")
        {

        }
        public override ConfigBase GetDefault()
        {
            return new Config { UserID="",Password=""};
        }
    }
}
