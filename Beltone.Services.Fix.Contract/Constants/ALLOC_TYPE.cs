using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Beltone.Services.Fix.Contract.Constants
{
    [DataContract(Namespace = "Beltone.MCDR")]
    public sealed class ALLOC_TYPE
    {
        [DataMember]
        public const string REGULAR = "REGULAR";
        [DataMember]
        public const string SAMEDAY = "SAMEDAY";
        [DataMember]
        public const string SAMEDAYPLUS = "SAMEDAYPLUS";
        [DataMember]
        public const string OMNIBUS = "OMNIBUS";
        [DataMember]
        public const string NONE = "NONE";
    }
}
