using System.Web.Mvc;

namespace MisFrameWork3.Areas.Card_Xcff
{
    public class Card_XcffAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "Card_Xcff";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "Card_Xcff_default",
                "Card_Xcff/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}