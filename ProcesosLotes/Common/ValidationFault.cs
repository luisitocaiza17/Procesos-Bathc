using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace SW.Common
{
    [DataContract]
    public class ValidationFault
    {
        public ValidationFault(int Code, string Message, string TracePath)
        {
            this.Code = Code;
            this.Message = Message;
            this.TracePath = TracePath;
        }

        int _Code = int.MinValue;

        [DataMember]
        public int Code
        {
            get { return _Code; }
            set { _Code = value; }
        }

        string _Message = "";
        [DataMember]
        public string Message
        {
            get { return _Message; }
            set { _Message = value; }
        }

        string _TracePath = "";
        [DataMember]
        public string TracePath
        {
            get { return _TracePath; }
            set { _TracePath = value; }
        }

    }
}
