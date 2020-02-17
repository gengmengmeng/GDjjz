using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

using MisFrameWork.core;
using MisFrameWork.core.db;
using MisFrameWork.core.db.Support;
namespace MisFrameWork3.Classes.DbListener
{
    /// <summary>
    /// Oracle 专用，因为Oracle里没有自增类型的字段，因此，如果需要使用自动特性，应该使用
    /// Sequence 实现，这个类就是在插入数据之前，判断有没有调置主键的值，如果没有就读取
    /// 名称为“SEQ_【表名】”的Sequence的值作为主键值。
    /// </summary>
    public class BeforeInsertLoadOracleSequence : IDbOperateListener
    {
        public BeforeInsertLoadOracleSequence() { }
        public BeforeInsertLoadOracleSequence(string sequenceName) : this()
        {
            this.sequenceName = sequenceName;
        }

        private string sequenceName = null;

        public string SequenceName
        {
            get { return sequenceName; }
            set { sequenceName = value; }
        }

        private string affectField = "ID";

        public string AffectField
        {
            get { return affectField; }
            set { affectField = value; }
        }

        private bool autoAffect = false;

        /// <summary>
        /// 自动处理ID名相同的主键
        /// </summary>
        public bool AutoAffect
        {
            get { return autoAffect; }
            set{ autoAffect=value;}
        }
        
        #region IDbOperateListener 成员

        public bool Execute(IDataBaseUtility dbu, Session session, ITableInfo tableInfo, System.Collections.IDictionary record, Condition where)
        {
            if (!this.AutoAffect)
            {
                string sqn = this.SequenceName;
                if (!record.Contains(affectField) || record[affectField]==null || "".Equals(record[affectField]))
                {
                    if (sqn == null || "".Equals(sqn))
                        sqn = "SEQ_" + tableInfo.TableName;
                    record[affectField] = dbu.Query(session, "dual", Condition.Empty, sqn + ".NextVal as val", null)[0]["VAL"];
                }
            }
            else
            {
                if (tableInfo.PrimaryFields.ContainsKey(this.AffectField)){//是主键，并且是数值型才处理
                    IFieldInfo fi = tableInfo.PrimaryFields[this.AffectField];
                    if ((fi.DbType == System.Data.DbType.Int32)||(fi.DbType == System.Data.DbType.Int64)||(fi.DbType == System.Data.DbType.UInt32)||(fi.DbType == System.Data.DbType.UInt64))
                    {
                        if (!record.Contains(affectField) || record[affectField]==null || "".Equals(record[affectField]))
                        {
                            string sqn = this.SequenceName;
                            if (sqn == null || "".Equals(sqn))
                                sqn = "SEQ_" + tableInfo.TableName;
                            record[affectField] = dbu.Query(session, "dual", Condition.Empty, sqn + ".NextVal as val", null)[0]["VAL"];
                        }
                    }
                }
            }
            return true;
        }
        #endregion
    }
}
