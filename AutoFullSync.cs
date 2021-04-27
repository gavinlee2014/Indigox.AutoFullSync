using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Indigox.AutoFullSync
{
    public partial class AutoFullSync : ServiceBase
    {
        public AutoFullSync()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                TaskTimer.Instance.Start();
            }
            catch (Exception e)
            {
                EventLog.WriteEntry("Start get error:" + e.Message + "\r\n" + e.StackTrace, EventLogEntryType.Error);
            }
        }

        protected override void OnStop()
        {
            try
            {
                TaskTimer.Instance.Stop();
            }
            catch (Exception e)
            {
                EventLog.WriteEntry("Stop get error:" + e.Message + "\r\n" + e.StackTrace, EventLogEntryType.Error);
            }
        }
    }
}
