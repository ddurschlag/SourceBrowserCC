using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SourceBrowser.Tracing
{
    public class Collector : IDisposable
    {
        private static object Lock = new object();
        private static Collector Active = null;

        public static void Register(Message m)
        {
            Collector c = Active;
            if (c == null)
                return;
            c._Messages.Add(m);
        }

        private List<Message> _Messages = new List<Message>();
        private bool Done = false;

        public Collector()
        {
            lock (Lock)
            {
                if (Active != null)
                    throw new Exception("Another collector is already active");
                Active = this;
            }
        }

        public void Dispose()
        {
            lock (Lock)
            {
                if (Active == this)
                    Active = null;
                Done = true;
            }
        }

        public IEnumerable<Message> Messages
        {
            get
            {
                if (!Done)
                    throw new Exception("Collection is not complete");
                return _Messages;
            }
        }
    }
}
