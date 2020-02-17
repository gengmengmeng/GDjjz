using System.Web.Mvc;

namespace MisFrameWork3.Areas.Card_QZ
{
    public class Card_QZAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "Card_QZ";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "Card_QZ_default",
                "Card_QZ/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}