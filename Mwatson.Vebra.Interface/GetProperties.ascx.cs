using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace MWatson.Vebra.Interface
{
    public partial class PopulateProperties : System.Web.UI.UserControl, umbraco.editorControls.userControlGrapper.IUsercontrolDataEditor
    {
        public string umbracoValue;

        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void GetProperties_Click(object sender, EventArgs e)
        {
            Authentication VebraInterface = new Authentication();
            VebraInterface.UpdateProperties();
            VebraInterface.CreateProperties(VebraInterface.GetPropertiesXmlFromPropertyList());
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