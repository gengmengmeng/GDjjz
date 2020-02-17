using System.Web.Mvc;

namespace MisFrameWork3.Areas.Card_Init
{
    public class Card_InitAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "Card_Init";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "Card_Init_default",
                "Card_Init/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}