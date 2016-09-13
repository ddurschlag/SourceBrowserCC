using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SourceBrowser.Tracing
{
    public class Message
    {
        public enum Level
        {
            Debug,
            Warning,
            Error,
            Info
        }

        public Level Type { get; set; }
        public string Text { get; set; }
    }
}
