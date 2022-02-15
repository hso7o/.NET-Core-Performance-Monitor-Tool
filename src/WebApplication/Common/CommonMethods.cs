using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication.Common
{
    public static class CommonMethods
    {
        // Converting DateTIme to a string that Http request will accept
        public static String convertDateTime(DateTime d)
        {
            return d.Year.ToString("D4") + "-" +
                d.Month.ToString("D2") + "-" +
                d.Day.ToString("D2") + "T" +
                d.Hour.ToString("D2") + "%3A" +
                d.Minute.ToString("D2") + "%3A" +
                d.Second.ToString("D2") + "." +
                d.Millisecond.ToString("D3");
        }
    }
}
