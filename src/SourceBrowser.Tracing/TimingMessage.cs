using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SourceBrowser.Tracing
{
    public class TimingMessage : IDisposable
    {
        public TimingMessage(string name)
        {
            Name = name;
            sw = new System.Diagnostics.Stopwatch();
            sw.Start();
        }

        private string Name { get; set; }
        private System.Diagnostics.Stopwatch sw;

        public void Dispose()
        {
            sw.Stop();
            Collector.Register(new Message
            {
                Type = Message.Level.Debug,
                Text = string.Format("{0}\t{1}\tms", Name, sw.ElapsedMilliseconds)
            });
        }
    }
}
