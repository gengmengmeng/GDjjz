using System.Web.Mvc;

namespace MisFrameWork3.Areas.ZZJLCX
{
    public class ZZJLCXAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "ZZJLCX";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "ZZJLCX_default",
                "ZZJLCX/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}