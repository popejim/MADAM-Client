using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Madam_Client
{
    [Serializable]
    public class Users
    {
        public string accounttype { get; set; }
        public string description { get; set; }
        public string domain { get; set; }
        public string fullName { get; set; }
        public string LocalAccount { get; set; }
        public string name { get; set; }
        public string PasswordExpire { get; set; }
        public string SID { get; set; }
        public string SidType { get; set; }
        public string Status { get; set; }
    }
}
