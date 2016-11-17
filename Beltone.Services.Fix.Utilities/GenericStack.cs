using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Data.Odbc;
using System.Data.OleDb;

using System.Collections;

namespace Beltone.Services.Fix.Utilities
{



    public class GenericStack<T>
    {
        Stack<T> m_stack = null;

        public GenericStack(int capacity, bool autoHandleThrottle)
        {
            m_stack = new Stack<T>(capacity);
        }


        public void Push(T stackItem)
        {
            if (stackItem == null)
            {
                throw new ArgumentNullException();
            }
            {
                m_stack.Push(stackItem);
            }
        }

        public T Pop()
        {
            if (m_stack.Count > 0)
            {
                return m_stack.Pop();
            }
            else
            {
                return default(T);
            }
        }

        public int Count { get { return m_stack.Count; } }

    }
}
