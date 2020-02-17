using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

using MisFrameWork.core;
using MisFrameWork.core.db;
using MisFrameWork.core.db.Support;
using MisFrameWork3.Classes.Membership;

namespace MisFrameWork3.Classes.DbListener
{
    public class BeforeInsertOrUpdateUserInfo : IDbOperateListener
    {
        private string m_FieldName = "CREATE_BY";
        public string FieldName
        {
            get { return m_FieldName; }
            set { m_FieldName = value; }
        }
        #region IDbOperateListener 成员

        public bool Execute(IDataBaseUtility dbu, Session session, ITableInfo tableInfo, System.Collections.IDictionary record, Condition where)
        {
            if (Membership.Membership.CurrentUser != null)
                record[m_FieldName] = Membership.Membership.CurrentUser.UserId;
            return true;
        }

        #endregion
    }
}
