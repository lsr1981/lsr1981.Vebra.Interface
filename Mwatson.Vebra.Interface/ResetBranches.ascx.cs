using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using umbraco.BusinessLogic;
using umbraco.cms.businesslogic.web;

namespace MWatson.Vebra.Interface
{
    public partial class ResetBranches : System.Web.UI.UserControl, umbraco.editorControls.userControlGrapper.IUsercontrolDataEditor
    {
        public string umbracoValue;

        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void BranchReset_Click(object sender, EventArgs e)
        {
            Authentication VebraInterface = new Authentication();

            if (VebraInterface.InterfaceValid)
            {
                VebraInterface.ResetCachedXmlDocuments();

                Document homeDocument = new Document(VebraInterface.HomeNode.Id);
                homeDocument.getProperty("branches").Value = "";
                homeDocument.Publish(User.GetCurrent());
                HttpContext.Current.Response.Redirect(HttpContext.Current.Request.RawUrl);
            }
        }

        public object value
        {
            get
            {
                return umbracoValue;
            }
            set
            {
                umbracoValue = value.ToString();
            }
        }
    }
}