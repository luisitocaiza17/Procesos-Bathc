using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace SW.Common
{
    [DataContract]
    public class InfraestructureFault
    {
        string _AttentionCode = "";
        [DataMember]
        public string AttentionCode
        {
            get { return _AttentionCode; }
            set { _AttentionCode = value; }
        }
    }
}
