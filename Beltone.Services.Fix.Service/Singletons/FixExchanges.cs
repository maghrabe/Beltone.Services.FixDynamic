using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beltone.Services.Fix.DataLayer;
using Beltone.Services.Fix.Entities.Entities;

namespace Beltone.Services.Fix.Service.Singletons
{
    public static class FixExchangesInfo
    {

        static FixExchange[] m_exchanges = null;
        static GroupSession[] m_todayActiveSessions = null;
        static DayOff[] _DaysOff = null;
        static List<string> _marketsOff = null;
        static object _lockObj = new object();

        public static void Initialize()
        {
            lock (_lockObj)
            {
                DatabaseMethods db = new DatabaseMethods();
                m_exchanges = db.GetFixExchangeInfo();
                m_todayActiveSessions = db.GetAllGroupsSessions();
                RefreshActiveSessions();
                db = null;
            }
        }

        public static void RefreshActiveSessions()
        {
            lock (_lockObj)
            {
                _marketsOff = new List<string>();
                DatabaseMethods db = new DatabaseMethods();
                _DaysOff = db.GetAllDaysOff();
                foreach (var day in _DaysOff)
                    if (!_marketsOff.Contains(day.ExchangeID))
                        _marketsOff.Add(day.ExchangeID);
            }
        }

        internal static bool IsTodayActiveGroupSession(string exchangeID, string groupID)
        {
            lock (_lockObj)
            {
                GroupSession session = m_todayActiveSessions.Single(b => b.ExchangeID == exchangeID && b.GroupID == groupID);
                return session.SessionStartTime.TimeOfDay < DateTime.Now.TimeOfDay
                    && session.SessionEndTime.TimeOfDay > DateTime.Now.TimeOfDay
                    && !_marketsOff.Contains(exchangeID);
            }
        }
    }
}
