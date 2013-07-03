using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Xml;
using umbraco.cms.businesslogic.web;
using umbraco.MacroEngines;
using umbraco.NodeFactory;

namespace MWatson.Vebra.Interface
{
    public partial class Branches : System.Web.UI.UserControl, umbraco.editorControls.userControlGrapper.IUsercontrolDataEditor
    {
        public string umbracoValue;
        List<XmlDocument> branchList = new List<XmlDocument>();

        protected void Page_Load(object sender, EventArgs e)
        {
            Authentication VebraInterface = new Authentication();
            string branches = umbracoValue;

            if (VebraInterface.InterfaceValid)
            {
                branchList = VebraInterface.GetBranchXmlList();

                if (!Page.IsPostBack)
                {
                    bool changed = false;
                    XmlDocument branchXml = VebraInterface.GetBranchesXml();
                    Document homeDocument = new Document(VebraInterface.HomeNode.Id);
                    XmlNodeList branchNodes = branchXml.GetElementsByTagName("branch");

                    foreach (XmlNode node in branchNodes)
                    {
                        string name = null;
                        string branchid = null;

                        XmlElement branchElement = (XmlElement)node;
                        name = branchElement.GetElementsByTagName("name")[0].InnerText;
                        branchid = branchElement.GetElementsByTagName("branchid")[0].InnerText;

                        if (String.IsNullOrEmpty(branches))
                        {
                            branches += (String.IsNullOrEmpty(name) ? "null" : name.Replace("~", "")) + "~" + branchid + "~0~valid";
                            changed = true;
                        }
                        else
                        {
                            if (!branches.Contains("~" + branchid + "~"))
                            {
                                branches += "," + (String.IsNullOrEmpty(name) ? "null" : name.Replace("~", "")) + "~" + branchid + "~0~valid";
                                changed = true;
                            }
                        }
                    }
                    if (changed)
                    {
                        umbracoValue = branches;
                    }
                }
                else
                {
                    umbracoValue = StoredValue.Value;
                }

                RenderView();
            }
        }

        private void RenderView()
        {
            string[] branchSplit = umbracoValue.Split(',');

            BranchPanel.Controls.Clear();

            HtmlGenericControl table = new HtmlGenericControl("table");
            table.Attributes.Add("class", "branch-table");
            BranchPanel.Controls.Add(table);

            HtmlGenericControl headerRow = new HtmlGenericControl("tr");
            table.Controls.Add(headerRow);

            HtmlGenericControl column1 = new HtmlGenericControl("th");
            column1.InnerText = "Branch";
            headerRow.Controls.Add(column1);

            HtmlGenericControl column2 = new HtmlGenericControl("th");
            column2.Attributes.Add("class", "center");
            column2.InnerText = "Get Properties from this branch";
            headerRow.Controls.Add(column2);

            foreach (string branch in branchSplit)
            {
                string[] branchValues = branch.Split('~');

                HtmlGenericControl row = new HtmlGenericControl("tr");
                table.Controls.Add(row);

                HtmlGenericControl col1 = new HtmlGenericControl("td");
                row.Controls.Add(col1);

                HtmlGenericControl p1 = new HtmlGenericControl("p");
                col1.Controls.Add(p1);

                HtmlGenericControl strong = new HtmlGenericControl("strong");
                strong.InnerText = branchValues[0];
                p1.Controls.Add(strong);

                foreach (XmlDocument branchXml in branchList)
                {
                    if (branchXml.GetElementsByTagName("BranchID")[0].InnerText == branchValues[1])
                    {
                        if (branchValues[0] == "null")
                        {
                            strong.InnerText = branchXml.GetElementsByTagName("town")[0].InnerText;
                        }

                        HtmlGenericControl p2 = new HtmlGenericControl("p");
                        col1.Controls.Add(p2);

                        if (branchXml.GetElementsByTagName("street")[0] != null)
                        {
                            HtmlGenericControl span1 = new HtmlGenericControl("span");
                            span1.InnerText = branchXml.GetElementsByTagName("street")[0].InnerText;
                            p2.Controls.Add(span1);

                            Literal br = new Literal();
                            br.Text = "<br/>";
                            p2.Controls.Add(br);
                        }

                        if (branchXml.GetElementsByTagName("town")[0] != null)
                        {
                            HtmlGenericControl span2 = new HtmlGenericControl("span");
                            span2.InnerText = branchXml.GetElementsByTagName("town")[0].InnerText;
                            p2.Controls.Add(span2);

                            Literal br = new Literal();
                            br.Text = "<br/>";
                            p2.Controls.Add(br);
                        }

                        if (branchXml.GetElementsByTagName("county")[0] != null)
                        {
                            HtmlGenericControl span3 = new HtmlGenericControl("span");
                            span3.InnerText = branchXml.GetElementsByTagName("county")[0].InnerText;
                            p2.Controls.Add(span3);

                            Literal br = new Literal();
                            br.Text = "<br/>";
                            p2.Controls.Add(br);
                        }

                        if (branchXml.GetElementsByTagName("postcode")[0] != null)
                        {
                            HtmlGenericControl span4 = new HtmlGenericControl("span");
                            span4.InnerText = branchXml.GetElementsByTagName("postcode")[0].InnerText;
                            p2.Controls.Add(span4);

                            Literal br = new Literal();
                            br.Text = "<br/>";
                            p2.Controls.Add(br);
                        }

                        if (branchXml.GetElementsByTagName("phone")[0] != null)
                        {
                            HtmlGenericControl span5 = new HtmlGenericControl("span");
                            span5.InnerText = branchXml.GetElementsByTagName("phone")[0].InnerText;
                            p2.Controls.Add(span5);

                            Literal br = new Literal();
                            br.Text = "<br/>";
                            p2.Controls.Add(br);
                        }

                        if (branchXml.GetElementsByTagName("email")[0] != null)
                        {
                            HtmlGenericControl span6 = new HtmlGenericControl("span");
                            span6.InnerText = branchXml.GetElementsByTagName("email")[0].InnerText;
                            p2.Controls.Add(span6);

                            Literal br = new Literal();
                            br.Text = "<br/>";
                            p2.Controls.Add(br);
                        }

                        if (branchValues[3] != "valid")
                        {
                            HtmlGenericControl p3 = new HtmlGenericControl("p");
                            p3.Attributes.Add("class", "warning");
                            p3.InnerText = "There was an issue with properties from this branch. Please contact your Administrator";
                            col1.Controls.Add(p3);
                        }

                    }
                }

                HtmlGenericControl col2 = new HtmlGenericControl("td");
                col2.Attributes.Add("class", "center");
                row.Controls.Add(col2);

                CheckBox showBranch = new CheckBox();
                showBranch.ID = branchValues[1];
                showBranch.Checked = (branchValues[2] == "1");
                showBranch.CheckedChanged += showBranch_CheckedChanged;
                showBranch.AutoPostBack = true;
                col2.Controls.Add(showBranch);
            }

            StoredValue.Value = umbracoValue;
        }

        void showBranch_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox selectedBranch = (CheckBox)sender;
            if (selectedBranch.Checked)
            {
                umbracoValue = umbracoValue.Replace("~" + selectedBranch.ID + "~0", "~" + selectedBranch.ID + "~1");
            }
            else
            {
                umbracoValue = umbracoValue.Replace("~" + selectedBranch.ID + "~1", "~" + selectedBranch.ID + "~0");
            }

            umbracoValue = umbracoValue.Replace("not-valid", "valid");

            RenderView();
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