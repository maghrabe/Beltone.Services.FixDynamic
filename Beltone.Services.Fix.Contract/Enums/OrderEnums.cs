using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Beltone.Services.Fix.Contract.Enums
{
    //public enum OrderSide { Buy = 1, Sell = 2, SellShot =3}
    //public enum OrderType { Market = 1, Limit = 2, Stop = 3, StopLimit = 4 }
    public enum FixMsgsReplicationDicretion { In = 1, Out = 2, Both = 3 }

    [DataContract]
    public enum HandleInstruction { [EnumMember] No_Broker_Invention = 1, [EnumMember] Broker_Invention = 2,[EnumMember] Manual_Best_Exec = 3 }
    //public enum ExecutionType { New, PartiallyFilled, Filled, Canceled, Replace, PendingCancel, Rejected, Suspended, PendingNew, Expired, Restated, PendingReplace }
    //public enum OrderStatus { New, PartiallyFilled, Filled, Canceled, Replaced, Rejected, Suspended, Expired, PendingReplace }
    //public enum RejectionReason { BrokerOption = 0, UnknowSecurityCode = 1, ExchangeClosed = 2, OrderExceedsLimit = 3, UnknownOrder = 5, DuplicateOrder = 6 }
}
    