//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Beltone.Services.Fix.DataLayer
{
    using System;
    using System.Collections.Generic;
    
    public partial class SessionsHistory
    {
        public long RecordID { get; set; }
        public System.Guid SessionKey { get; set; }
        public string RequestType { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string QueueIP { get; set; }
        public string QueueName { get; set; }
        public string QueuePath { get; set; }
        public System.DateTime ConnectionDateTime { get; set; }
        public bool IsTestRequested { get; set; }
        public Nullable<System.DateTime> TestReqDateTime { get; set; }
        public string ReqTestKey { get; set; }
        public bool IsTestResponded { get; set; }
        public Nullable<System.DateTime> TestResDateTime { get; set; }
        public string ResTestKey { get; set; }
        public bool IsValidTestKey { get; set; }
        public bool RequestedSubscription { get; set; }
        public bool IsOnline { get; set; }
        public bool IsSubscribed { get; set; }
        public string SubscriptionDateTimeString { get; set; }
        public Nullable<System.DateTime> SubscriptionDateTime { get; set; }
        public string Note { get; set; }
        public string ErrCode { get; set; }
        public string ErrMsg { get; set; }
        public bool IsUnsubscribed { get; set; }
        public Nullable<System.DateTime> UnsubscriptionDateTime0 { get; set; }
        public bool NewQueue { get; set; }
        public bool FlushUpdatesOffline { get; set; }
        public bool IsSessionFaulted { get; set; }
        public Nullable<System.DateTime> SessionFaultDateTime { get; set; }
        public Nullable<System.Guid> ResubSessionKey { get; set; }
    }
}
