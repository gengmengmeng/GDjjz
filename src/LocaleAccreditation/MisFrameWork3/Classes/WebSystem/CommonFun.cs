using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace AutoUpdateWeb.Class.WebSystem
{
    public class CommonFun
    {
        static char[] byte2stringchar = "0123456789ABCDEF".ToCharArray();
        public static string Byte2String(byte[] buffer, int start, int length)
        {
            char[] data = new char[length + length];
            for (int i1 = start, i2 = 0; i1 < length + start; i1++)
            {
                data[i2++] = byte2stringchar[buffer[i1] >> 4];
                data[i2++] = byte2stringchar[buffer[i1] & 0xf];
            }
            return new string(data);
        }
        public static string Byte2String(byte[] buffer)
        {
            return Byte2String(buffer, 0, buffer.Length);
        }

        public static byte[] String2Byte(string str)
        {
            char[] data = str.ToUpper().ToCharArray();
            byte[] result = new byte[data.Length / 2];
            for (int i1 = 0, i2 = 0; i2 < result.Length; i2++)
            {
                result[i2] = (byte)(((data[i1++] - 55) << 4) & (data[i1++] - 55));
            }
            return result;
        }

        private static string planFloder;
        public static string PlanFloder
        {
            get
            {
                if (string.IsNullOrEmpty(planFloder))
                {
                    string p = ConfigurationManager.AppSettings["plans"];
                    if (p.IndexOf(':') < 0)
                        p = HttpContext.Current.Server.MapPath("~/") + p;
                    planFloder = p;
                }
                return planFloder;
            }
        }

        /*public static string GetPlanJson(string plan)
        {
            PlanFloder
        }*/
    }
}