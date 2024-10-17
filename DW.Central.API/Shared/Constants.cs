using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DW.Central.API.Shared
{
    internal class Constants
    {
        protected internal readonly static int MaxCounterValue = 32767;
        protected internal readonly static string CUSTOMWEBPARTTYPE = "97ad35a5-ad54-4921-b28b-8e1919ad5596";
        protected internal readonly static string APPBOOKINGTITLE = "AppBooking";
        protected internal readonly static string APPBOOKINGDESCRIPTION = "Online booking system with the flexibility to book your events";
        protected internal readonly static string MSGraphScopeURL = @"https://graph.microsoft.com/.default";
        protected internal readonly static string MSGraphGetMemberListGroup = @"https://graph.microsoft.com/v1.0/groups/groupId/members";
        protected internal readonly static string MSGraphGetAllSites = @"https://graph.microsoft.com/v1.0/sites";
        protected internal readonly static string MSGraphGetSiteLists = @"https://graph.microsoft.com/v1.0/sites/siteId/lists";
        protected internal readonly static string MSGraphGetSiteSubSites = @"https://graph.microsoft.com/v1.0/sites/siteId/sites";
        protected internal readonly static string MSGraphGetListColumns = @"https://graph.microsoft.com/v1.0/sites/siteId/lists/listId/columns";
        protected internal readonly static string MSGraphGetSitePages = @"https://graph.microsoft.com/beta/sites/siteId/pages";
        protected internal readonly static string MSGraphGetPagesWebparts = @"https://graph.microsoft.com/v1.0/sites/siteId/pages/sitePageId/microsoft.graph.sitePage/webParts";
        //protected internal readonly static string MSGraphGetPagesWebpart = @"https://graph.microsoft.com/beta/sites/siteId/pages/sitePageId/webParts/webPartId";
        protected internal readonly static string MSGraphGetPagesWebpart = @"https://graph.microsoft.com/v1.0/sites/siteId/pages/sitePageId/microsoft.graph.sitepage/webparts";
        protected internal readonly static string MSGraphGetEventListItems = @"https://graph.microsoft.com/v1.0/sites/siteId/lists/listId/items?expand=fields(select=EventDate,Title,PumaTeamsMeetingUrl)";
    }
}
