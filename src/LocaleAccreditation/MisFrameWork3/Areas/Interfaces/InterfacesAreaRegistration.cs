using System.Web.Mvc;

namespace MisFrameWork3.Areas.Interfaces
{
    public class InterfacesAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "Interfaces";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "Interfaces_default",
                "Interfaces/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}