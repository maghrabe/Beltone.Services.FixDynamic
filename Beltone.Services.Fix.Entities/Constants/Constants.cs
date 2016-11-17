using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Beltone.Services.Fix.Entities.Constants
{
    public class ConStrongKeys
    {
        public const string FixDbCon = "FixOrdersDBConnectionString";
    }

    public class SingleOrderProperties
    {
        public const string TableName = "Orders";
        public const string OrderID = "OrderID";
        public const string ClOrderID = "ClOrderID";
        public const string OrigClOrdID = "OrigClOrdID";
        public const string RequesterOrderID = "RequesterOrderID";
        public const string ParentOrderID = "ParentOrderID";
        public const string IntegrationOrderID = "IntegrationOrderID";
        public const string BourseOrderID = "BourseOrderID";
        public const string ClientID = "ClientID";
        public const string MarketID = "MarketID";
        public const string ExchangeID = "ExchangeID";
        public const string GroupID = "GroupID";
        public const string SecurityCode = "SecurityCode";
        public const string OriginalQuantity = "OriginalQuantity";
        public const string CurrentQuantity = "CurrentQuantity";
        public const string ExecutedQuantity = "ExecutedQuantity";
        public const string LastExecQuantity = "LastExecQuantity";
        public const string RemainingQuantity = "RemainingQuantity";
        public const string OriginalPrice = "OriginalPrice";
        public const string CurrentPrice = "CurrentPrice";
        public const string LastExecPrice = "LastExecPrice";
        public const string AvgPrice = "AvgPrice";
        public const string CustodyID = "CustodyID";
        public const string OriginalOrderType = "OriginalOrderType";
        public const string OrderType = "OrderType";
        public const string OrderSide = "OrderSide";
        public const string OriginalOrderStatus = "OriginalOrderStatus";
        public const string OrderStatus = "OrderStatus";
        public const string ExecType = "ExecutionType";
        public const string OriginalTimeInForce = "OriginalTimeInForce";
        public const string TimeInForce = "TimeInForce";
        public const string Note = "Note";
        public const string Note2 = "Note2";
        public const string Note3 = "Note3";
        public const string Note4 = "Note4";
        public const string Note5 = "Note5";
        public const string HasSystemError = "HasSystemError";
        public const string ErrorMessage = "ErrorMessage";
        public const string IsRefused = "IsRefused";
        public const string IsSuspended = "IsSuspended";
        public const string IsRejected = "IsRejected";
        public const string IsExecuted = "IsExecuted";
        public const string IsExpired = "IsExpired";
        public const string IsActive = "IsActive";
        public const string IsCompleted = "IsCompleted";
        public const string IsCanceled = "IsCanceled";
        public const string IsPending = "IsPending";
        public const string IsAcceptedByBourse = "IsAcceptedByBourse";
        public const string PlacementDateTime = "PlacementDateTime";
        public const string ModifiedDateTime = "ModifiedDateTime";
        public const string OrderRecievedDateTime = "OrderRecievedDateTime";
        public const string OrderConfirmDateTime = "OrderConfirmDateTime";
        public const string OrderCreatedBySysDateTime = "OrderCreatedBySysDateTime";
        public const string ExpirationDate = "ExpirationDate";
        public const string ExpirationDateTime = "ExpirationDateTime";
        public const string OrderViaID = "OrderViaID";
        public const string OrderSystemType = "OrderSystemType";
        public const string SourceIP = "SourceIP";
        public const string DestinationIP = "DestinationIP";
        public const string CurrencyID = "CurrencyID";
        public const string FX_Rate = "FX_Rate";
        public const string CancellationReason = "CancellationReason";
        public const string RejectionReason = "RejectionReason";
        public const string SuspensionReason = "SuspensionReason";
        public const string TraderMind = "TraderMind";
        public const string IsConditionedPrice = "IsConditionedPrice";
        public const string ConditionedPrice = "ConditionedPrice";
        public const string OriginalHandleInst = "OriginalHandleInst";
        public const string HandleInst = "HandleInst";
        public const string AON = "AON";
        public const string OriginalAON = "OriginalAON";
        public const string MinQty = "MinQty";
        public const string OriginalMinQty = "OriginalMinQty";
        public const string IsPendingMcsd = "IsPendingMcsd";

        public const string RequestedQty = "RequestedQty";
        public const string RequestedPrice = "RequestedPrice";

        public const string RequestedAON = "RequestedAON";
        public const string RequestedMinQty = "RequestedMinQty";

        public const string RequestedTimeInForce = "RequestedTimeInForce";
        public const string RequestedOrderType = "RequestedOrderType";
        //  public const string Alloc_McsdQuantity = "Alloc_McsdQuantity";

        public const string AllocType = "AllocType";
        public const string AllocUnifiedCode = "AllocUnifiedCode";
        public const string IsMcsdAllocRequired = "IsMcsdAllocRequired";
        public const string IsMcsdTimedOut = "IsMcsdTimedOut";
        public const string McsdrAllocQty = "McsdrAllocQty";
        public const string McsdrExpiryDate = "McsdrExpiryDate";
        public const string ActionOnAllocResponse = "ActionOnAllocResponse";
    }

    public class SingleOrdDetailsProps
    {
        public const string TableName = "Orders_Details";
        public const string OrderID = "OrderID";
        public const string ClOrderID = "ClOrderID";
        public const string OrigClOrdID = "OrigClOrdID";
        public const string CurrentQuantity = "CurrentQuantity";
        public const string ExecutedQuantity = "ExecutedQuantity";
        public const string LastExecQuantity = "LastExecQuantity";
        public const string RemainingQuantity = "RemainingQuantity";
        public const string RequestedQuantity = "RequestedQuantity";
        public const string CurrentPrice = "CurrentPrice";
        public const string ExecPrice = "ExecPrice";
        public const string AvgPrice = "AvgPrice";
        public const string RequestedPrice = "RequestedPrice";
        public const string OrderType = "OrderType";
        public const string RequestedOrderType = "RequestedOrderType";
        public const string OrderStatus = "OrderStatus";
        public const string ExecType = "ExecutionType";
        public const string TimeInForce = "TimeInForce";
        public const string RequestedTimeInForce = "RequestedTimeInForce";
        public const string Note = "Note";
        public const string Note2 = "Note2";
        public const string Note3 = "Note3";
        public const string HasSystemError = "HasSystemError";
        public const string IsRefusedRequest = "IsRefusedRequest";
        public const string IsUserRequest = "IsUserRequest";
        public const string IsResponse = "IsResponse";
        public const string IsNewOrderRequest = "IsNewOrderRequest";
        public const string IsNewOrderResponse = "IsNewOrderResponse";
        public const string IsCancelRequest = "IsCancelRequest";
        public const string IsCancelResponse = "IsCancelResponse";
        public const string IsModifyRequest = "IsModifyRequest";
        public const string IsModifyResponse = "IsModifyResponse";
        public const string ErrorMessage = "ErrorMessage";
        public const string DateTime = "DateTime";
        public const string ExecutionID = "ExecutionID";
        public const string ExecutionDate = "ExecutionDate";
        public const string ExecutionRecievedDateTime = "ExecutionRecievedDateTime";
        public const string FX_Rate = "FX_Rate";
        public const string OrderSystemType = "OrderSystemType";
        public const string OrderViaID = "OrderViaID";
        public const string SourceIP = "SourceIP";
        public const string DestinationIP = "DestinationIP";
        public const string ExecutionMsgType = "ExecutionMsgType";
        public const string ExecutionMsg = "ExecutionMsg";
        public const string CancellationReason = "CancellationReason";
        public const string RejectionReason = "RejectionReason";
        public const string SuspensionReason = "SuspensionReason";
        public const string HandleInst = "HandleInst";
        public const string AON = "AON";
        public const string MinQty = "MinQty";
        public const string RequestedMinQty = "RequestedMinQty";
        public const string IsMcdrRec = "IsMcdrRec";
    }

    public class ORD_STATUS
    {
        public const string PendingNew = "PendingNew";
        public const string New = "New";
        public const string PendingReplace = "PendingReplace";
        public const string Replaced = "Replaced";
        public const string PendingCancel = "PendingCancel";
        public const string Canceled = "Canceled";
        public const string PartialFilled = "PartialFilled";
        public const string Filled = "Filled";
        public const string Rejected = "Rejected";
        public const string Suspended = "Suspended";
        public const string Expired = "Expired";

    }

    public class EXEC_TYP
    {
        public const string PendingNew = "PendingNew";
        public const string New = "New";
        public const string PendingReplace = "PendingReplace";
        public const string Replace = "Replace";
        public const string PendingCancel = "PendingCancel";
        public const string Canceled = "Canceled";
        public const string PartialFill = "PartialFill";
        public const string Fill = "Fill";
        public const string Rejected = "Rejected";
        public const string Suspended = "Suspended";
        public const string Expired = "Expired";
        public const string Restated = "Restated";

    }

    public class ORDER_TYP
    {

        public const string New = "New";
        public const string Modify = "Modify";
        public const string Cancel = "Cancel";
    }

    public class ActionOnAllocResponse
    {
        public const string SendNewOrder = "SendNewOrder";
        public const string ModifyOrder = "ModifyOrder";
        public const string CancelOrder = "CancelOrder";
        public const string DoNothing = "DoNothing";

    }
}