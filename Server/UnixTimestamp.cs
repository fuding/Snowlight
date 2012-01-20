using System;

namespace Snowlight
{
    public static class UnixTimestamp
    {
        public static double GetCurrent()
        {
            TimeSpan ts = new TimeSpan(DateTime.Now.Ticks - new DateTime(1970, 1, 1, 0, 0, 0).Ticks);
            return Convert.ToInt32(ts.TotalSeconds);
        }

        public static DateTime GetDateTimeFromUnixTimestamp(double Timestamp)
        {
            DateTime DT = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            DT = DT.AddSeconds(Timestamp);
            return DT;
        }
    }
}
