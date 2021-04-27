using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Indigox.AutoFullSync
{
    class TaskTimer
    {
        private static TaskTimer instance = new TaskTimer();
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly System.Timers.Timer timer = new System.Timers.Timer();
        private TaskTimer()
        {
            timer.Enabled = true;
            timer.Interval = 1 * 60 * 1000;//执行间隔时间,单位为毫秒  
            timer.Elapsed += new ElapsedEventHandler(Timer1_Elapsed);
        }

        private void Timer1_Elapsed(object sender, ElapsedEventArgs e)
        {

            if ((!HasSynced()) && IsInTime(e.SignalTime.Hour, e.SignalTime.Minute, e.SignalTime.Second)) {

                _ = SendSyncRequestAsync();
                SaveRecord();
            }
        }

        private async Task SendSyncRequestAsync()
        {
            CancellationTokenSource token = new CancellationTokenSource();
            try
            {
                string sysID = ConfigurationManager.AppSettings["SYS_ID"];
                string commandHost = ConfigurationManager.AppSettings["COMMAND_HOST"];
                //string commandPath = "/UUM/_remoting/call?_remotingcommand=batch&r=" + DateTime.Now.Ticks;

                //HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("CALL"),
                //    commandHost.TrimEnd('/') + commandPath)
                //{
                //    Content = new StringContent("[{\"ID\":\"FullSyncCommand_1\","
                //    + "\"Name\":\"FullSyncCommand\","
                //    + "\"Method\":\"Execute\","
                //    + "\"Properties\":{\"SystemID\":"+sysID+"}]")
                //};

                string commandPath = "/FullSyncHandler.ashx?sysID=" + sysID;

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get,
                    commandHost.TrimEnd('/') + commandPath);

                HttpClient client = new HttpClient();
                client.Timeout = TimeSpan.FromMilliseconds(5 * 60 * 1000);
                Log("send request", EventLogEntryType.Information);

                HttpResponseMessage res = await client.SendAsync(request);
                if (!res.IsSuccessStatusCode)
                {
                    Log("response err:" + res.ReasonPhrase + " " + res.StatusCode, EventLogEntryType.Error);
                }
                else
                {
                    Log("sync success", EventLogEntryType.Information);
                }
            }
            catch (TaskCanceledException ex)
            {
                if (ex.CancellationToken == token.Token)
                {
                    Log(ex.ToString(), EventLogEntryType.Error);
                }
                else
                {
                    // a web request timeout
                }
            }
            catch (Exception e)
            {
                Log(e.ToString(), EventLogEntryType.Error);
            }
            
        }

        private bool IsInTime(int hour, int minute, int second)
        {
            string syncTime = ConfigurationManager.AppSettings["SYNC_TIME_HOUR"];
            if (hour == Convert.ToInt32(syncTime))
            {
                return true;
            }
            return false;
        }

        private bool HasSynced()
        {
            long lastSyncTimeTick = LoadRecord();
            if (lastSyncTimeTick == 0)
            {
                return false;
            }
            DateTime lastSyncTime = new DateTime(lastSyncTimeTick);
            if (lastSyncTime.ToString("yyyy-MM-dd").Equals(DateTime.Now.ToString("yyyy-MM-dd")))
            {
                return true;
            }
            return false;
        }

        private void Log(string message, EventLogEntryType type)
        {
            //if (logger != null)
            //{
            //    logger.WriteEntry(message, type);
            //}
            switch (type)
            {
                case EventLogEntryType.Error:
                    log.Error(message);
                    return;
                case EventLogEntryType.Warning:
                    log.Warn(message);
                    return;
                case EventLogEntryType.Information:
                    log.Info(message);
                    return;
                default:
                    log.Debug(message);
                    return;
;
            }
        }

        private void SaveRecord()
        {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string filePath = Path.Combine(path, "LastExcute.log");
            File.WriteAllText(filePath, Convert.ToString(DateTime.Now.Ticks));
        }

        private long LoadRecord()
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "LastExcute.log");
            if (!File.Exists(path))
            {
                return 0;
            }
            string[] contents = File.ReadAllLines(path);
            for (int i = 0; i < contents.Length; i++)
            {
                if (!String.IsNullOrEmpty(contents[i]))
                {
                    try
                    {
                        return Convert.ToInt64(contents[i]);
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
            }
            return 0;
        }

        public static TaskTimer Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new TaskTimer();
                }
                return instance;
            }
        }

        public void Start()
        {
            timer.Start();
        }

        public void Stop()
        {
            timer.Stop();
        }
    }
}
