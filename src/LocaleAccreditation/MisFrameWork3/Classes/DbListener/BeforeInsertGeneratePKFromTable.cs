using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Text.RegularExpressions;

using MisFrameWork.core;
using MisFrameWork.core.db;
using MisFrameWork.core.db.Support;

namespace MisFrameWork3.Classes.DbListener
{
    /// <summary>
    /// 主要以设置Rule属性为主。
    /// Rule的规则：{日期格式}或[记录的字段]或(序号的长度)
    /// 比如想要设置一种“A+单位名称+输入日期+5位序号”可以这样设置
    /// A[LRDW]{yyyyMMdd}(5)
    /// </summary>
    public class BeforeInsertGeneratePKFromTable : IDbOperateListener
    {
        private string sysTableName = "S_SYS_GENERATE_INFO";
        private string generateRule = "{yyyyMM}";
        public string SysTableName
        {
            get { return sysTableName; }
            set { sysTableName = value; }
        }
        #region IDbOperateListener 成员
        public string Rule
        {
            get { return generateRule; }
            set { generateRule = value; }
        }

        protected string GenerateKeyInfo(System.Collections.IDictionary record)
        {
            string result = this.Rule;
            DateTime now = DateTime.Now;
            Regex r = new Regex(@"\{(.*?)\}");
            MatchCollection ms = r.Matches(result);
            if (ms.Count>0)
            {
                for (int i = 0; i < ms.Count; i++)
                {
                    result = result.Replace("{" + ms[i].Groups[1].Value + "}", now.ToString(ms[i].Groups[1].Value));
                }
            }
            r = new Regex(@"\[(.*?)\]");
            ms = r.Matches(result);
            if (ms.Count > 0)
            {
                for (int i = 0; i < ms.Count; i++)
                {
                    result = result.Replace("[" + ms[i].Groups[1].Value + "]", (string)record[ms[i].Groups[1].Value]);
                }
            }
            return result;
        }

        public bool Execute(IDataBaseUtility dbu, Session session, ITableInfo tableInfo, System.Collections.IDictionary record, Condition where)
        {
            Condition ct = new Condition();
            ct.SubConditions.Add(new Condition("and", "TN", "=", tableInfo.TableName));
            string keyInfo = this.GenerateKeyInfo(record);
            ct.SubConditions.Add(new Condition("and","KI","=",keyInfo));
            IList<UnCaseSenseHashTable> rs = dbu.Query(session,this.SysTableName, ct, "*", null);
            decimal seq = 1;
            if (rs.Count==0)
            {
                UnCaseSenseHashTable newrs = new UnCaseSenseHashTable();
                newrs["TN"] = tableInfo.TableName;
                newrs["KI"] = keyInfo;
                newrs["SEQ"] = 2;
                dbu.InsertRecord(session, this.SysTableName, newrs);
            }
            else
            {
                seq = rs[0].GetDecimalValue("SEQ");
                rs[0]["SEQ"] = seq + 1;
                dbu.UpdateRecord(session, this.SysTableName, rs[0],false);
            }

            Regex r = new Regex(@"\((\d?)\)");
            MatchCollection ms = r.Matches(keyInfo);
            if (ms.Count > 0)
            {
                for (int i = 0; i < ms.Count; i++)
                {
                    keyInfo = keyInfo.Replace("(" + ms[i].Groups[1].Value + ")", seq.ToString().PadLeft(int.Parse("0"+ms[i].Groups[1].Value),'0'));
                }
            }
            foreach (string k in tableInfo.PrimaryFields.Keys){
                record[tableInfo.PrimaryFields[k].CloumnName] = keyInfo;
            }
            return true;
        }

        #endregion
    }
}
