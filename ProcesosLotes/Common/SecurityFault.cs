using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace SW.Common
{
    public enum SessionEvents
    {
        None = -1,
        SessionExpired = 0,
        SessionKilled = 1,
        RequestNotValid = 2,
        OnManteinance = 3
    }


    [DataContract]
    public class SecurityFault
    {
        SessionEvents _SessionEvent = SessionEvents.None;

        public SecurityFault(SessionEvents SessionEvent, string Message)
        {
            _SessionEvent = SessionEvent;
            _Message = Message;
        }

        [DataMember]
        public SessionEvents SessionEvent
        {
            get { return _SessionEvent; }
            set { _SessionEvent = value; }
        }

        string _Message = "";
        [DataMember]
        public string Message
        {
            get { return _Message; }
            set { _Message = value; }
        }
    }
}
