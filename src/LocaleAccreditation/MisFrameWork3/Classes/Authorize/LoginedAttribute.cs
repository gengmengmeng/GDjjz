using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using MisFrameWork3.Classes.Membership;

namespace MisFrameWork3.Classes.Authorize
{
    public class LoginedAttribute: AuthorizeAttribute
    {
        string mOperateId = null;
        public string OperateId
        {
            get{return mOperateId;}
            set{mOperateId=value;}
        }
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            if (Membership.Membership.CurrentUser==null)
            {
                if (filterContext.ActionDescriptor.ActionName.StartsWith("Json"))
                {
                    ContentResult cr = new ContentResult();
                    cr.Content = "{sucess:false,message='登陆信息已过期，请登陆系统.'}";
                    cr.ContentType = "application/json";
                    filterContext.Result = cr;
                }
                else
                    filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Home", action = "Index", message = "登陆信息已过期，请登陆系统." }));
                return;
            }
            else
            {
                if (!String.IsNullOrEmpty(mOperateId))
                {
                    if (!Membership.Membership.CurrentUser.HaveAuthority(OperateId))
                    {
                        ContentResult cr = new ContentResult();
                        cr.Content = "您没有该业务的操作权限！";
                        //cr.ContentType = "application/json";
                        filterContext.Result = cr;
                    }
                }
            }
        }
    }
}