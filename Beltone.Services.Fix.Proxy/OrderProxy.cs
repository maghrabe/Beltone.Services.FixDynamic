using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beltone.Services.Fix.Contract;
using System.ServiceModel;
using Beltone.Services.Fix.Contract.Entities.RequestMessages;

namespace Beltone.Services.Fix.Proxy
{
    public class OrdersProxy : ClientBase<IOrders>,IOrders
    {
         public OrdersProxy()
        {
        }

         public OrdersProxy(string Configuration)
            : base(Configuration)
        {
        }

        #region IOrder Members

        public void Handle(Beltone.Services.Fix.Contract.Interfaces.IRequestMessage msg)
        {
            this.Channel.Handle(msg);
        }

        #endregion


    }
}
