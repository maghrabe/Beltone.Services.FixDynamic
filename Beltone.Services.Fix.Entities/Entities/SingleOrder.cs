using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beltone.Services.Fix.Entities.Constants;

namespace Beltone.Services.Fix.Entities.Entities
{
    public class SingleOrder : IComparable<SingleOrder>
    {

        public Dictionary<string, object> Data { get; set; }
        static string[] _columns = null;

        static SingleOrder()
        {
            _columns = typeof(SingleOrderProperties).GetFields().Where(x => x.GetValue(x).ToString() != SingleOrderProperties.TableName).Select(x => x.GetValue(x).ToString()).ToArray();
        }

        public SingleOrder()
        {
            Data = new Dictionary<string, object>();
            foreach (string colName in _columns)
                Data.Add(colName, null);
        }

        public object this[string propName]
        {
            get { return Data[propName]; }
            set { Data[propName] = value; }
        }


        public static int operator +(SingleOrder ord1, SingleOrder ord2)
        {
            return (int)ord1[SingleOrderProperties.CurrentQuantity] + (int)ord2[SingleOrderProperties.CurrentQuantity];
        }

        public static int operator -(SingleOrder ord1, SingleOrder ord2)
        {
            return (int)ord1[SingleOrderProperties.CurrentQuantity] - (int)ord2[SingleOrderProperties.CurrentQuantity];
        }

        public static bool operator >(SingleOrder ord1, SingleOrder ord2)
        {
            return ord1.CompareTo(ord2) > 0;
        }

        public static bool operator <(SingleOrder ord1, SingleOrder ord2)
        {
            return ord1.CompareTo(ord2) < 0;
        }

        public static bool operator >=(SingleOrder ord1, SingleOrder ord2)
        {
            return ord1.CompareTo(ord2) >= 0;
        }

        public static bool operator <=(SingleOrder ord1, SingleOrder ord2)
        {
            return ord1.CompareTo(ord2) <= 0;
        }

        //public override bool Equals(object other)
        //{
        //    AllocationOrder order = other as AllocationOrder;
        //    if (order == null)
        //        return false;
        //    return (long)this[PROPS_ALLOC_TABLE.OrderID] == (long)((AllocationOrder)other)[PROPS_ALLOC_TABLE.OrderID];
        //}

        //public static bool operator ==(AllocationOrder ord1, AllocationOrder ord2)
        //{
        //    return ord1.Equals(ord2);
        //}

        //public static bool operator !=(AllocationOrder ord1, AllocationOrder ord2)
        //{
        //    return !ord1.Equals(ord2);
        //}

        /// <summary>
        /// compare order's value (Qty * Price)
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(SingleOrder other)
        {
            decimal thisValue = (int)this[SingleOrderProperties.CurrentQuantity];
            decimal otherValue = (int)other[SingleOrderProperties.CurrentQuantity];
            if (thisValue > otherValue)
                return 1;
            if (thisValue < otherValue)
                return -1;
            return 0;
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        /// <summary>
        /// returns order id in order to help hashcode calculations
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("Order [{0}]", this[SingleOrderProperties.OrderID]);
        }

    }
}




//public class SingleOrder
//{
//    public long OrderID { get; set; }
//    /// <summary>
//    /// Order ID if Requester
//    /// </summary>
//    public Guid RequesterOrderID { get; set; }
//    /// <summary>
//    /// Generated ID By Quick Fix Library as Ref For Order
//    /// </summary>
//    public string ClOrderID { get; set; }
//    /// <summary>
//    /// Gets The ClOrderID Value When Performing An Edit Operation
//    /// </summary>
//    public string OrigClOrdID { get; set; }
//    /// <summary>
//    /// Returned By Bourse as a Unique Order ID
//    /// </summary>
//    public string BourseOrderID { get; set; }
//    public int ClientID { get; set; }
//    public string SecurityCode { get; set; }
//    public int OriginalQuantity { get; set; }
//    public int CurrentQuantity { get; set; }
//    public int ExecutedQuantity { get; set; }
//    public int RemainingQuantity { get; set; }
//    public double OriginalPrice { get; set; }
//    public double CurrentPrice { get; set; }
//    public string CustodyID { get; set; }
//    public string OrderType { get; set; }
//    public string OrderSide { get; set; }
//    public string OrderStatus { get; set; }
//    public string ExecType { get; set; }
//    public string TimeInForce { get; set; }
//    public string Note { get; set; }
//    public bool HasSystemError { get; set; }
//    //public bool IsSuspended { get; set; }
//    //public bool IsRejected { get; set; }
//    //public bool IsExecuted { get; set; }
//    public string ErrorMessage { get; set; }
//    public DateTime PlacementDateTime { get; set; }
//    public DateTime ModifiedDateTime { get; set; }
//}