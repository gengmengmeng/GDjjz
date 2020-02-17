    using System.Web.Mvc;

namespace MisFrameWork3.Areas.WhiteCard
{
    public class WhiteCardAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "WhiteCard";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "WhiteCard_default",
                "WhiteCard/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}