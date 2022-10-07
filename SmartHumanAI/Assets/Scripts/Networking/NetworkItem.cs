using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Assets.Scripts.Networking
{
    [Serializable]
    public class NetworkItem
    {
        public int msg_type;
        public string msg;

        public NetworkItem(int msg_type, string p_msg_body)
        {
            this.msg_type = msg_type;
            this.msg = p_msg_body;
        }
    }
}
