using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Beltone.Services.Fix.Entities
{
    public enum FixSessionStatus

    {
        Connected = 1,
        SessionStarted = 2,
        Disconnected = 3,
        DataError = 4,
        ConnectionClosed = 6,
        ServerFailure = 7,
        ConnectionFailure = 8,
        RecievingUpdates = 9
            /////////////////////////////
    }

    public enum OrderActions
    {
        New = 1,
        Modify = 2,
        Cancel = 3
    }

    public enum McsdSourceMessage
    {
        McsdAccepted = 1,
        McsdTimedOut = 2,
        McsdRejected = 3
    }
}

