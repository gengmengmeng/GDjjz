using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Data.Common;

using MisFrameWork.core;
using MisFrameWork.core.db;
using MisFrameWork.core.db.Support;

namespace MisFrameWork3.Classes.DbListener
{
    public class AfterInsertGetAutoGenerateId : IDbOperateListener
    {
        private string fieldName = null;

        public string FieldName
        {
            get { return fieldName; }
            set { fieldName = value; }
        }
        
        #region IDbOperateListener 成员

        public bool Execute(IDataBaseUtility dbu, Session session, ITableInfo tableInfo, IDictionary record, Condition where)
        {
            if (record.Contains("f.CloumnName"))
                return true;
            foreach (string k in tableInfo.PrimaryFields.Keys)
            {
                IFieldInfo f = tableInfo.FieldsByName[k];
                if (f.CloumnName.Equals(FieldName))
                {
                    record[f.CloumnName] = dbu.Query(session,"select " + f.CloumnName + " from " + tableInfo.TableName + " order by " + f.CloumnName + " desc", 0, 2)[0][f.CloumnName];
                    //record[f.CloumnName] = dbu.Query(session, tableInfo.TableName, Condition.Empty, f.CloumnName, f.CloumnName + " desc", null, 0, 2)[0][f.CloumnName];
                }
            }
            return true;
        }

        #endregion
    }
}
