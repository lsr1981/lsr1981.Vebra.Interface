using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Xml;
using umbraco.BusinessLogic;
using umbraco.cms.businesslogic.datatype;
using umbraco.cms.businesslogic.media;
using umbraco.cms.businesslogic.web;
using umbraco.interfaces;
using umbraco.NodeFactory;

namespace MWatson.Vebra.Interface
{
    public class Authentication
    {
        private string username;
        private string password;
        private string dataFeedID;
        private string APIVersion = "4";
        private string tokenPath = "/App_Data/Token/token.txt";
        private string branchPath = "/App_Data/Vebra/Branch/branch.xml";
        private string branchesPath = "/App_Data/Vebra/Branch/Branches/";
        private string propertiesPath = "/App_Data/Vebra/Branch/Branches/Properties/";
        private string filesPath = "/App_Data/Vebra/Branch/Branches/Files/";
        private string token;
        private string baseUrl;
        private string branchUrl;
        private bool tokenValid = false;
        private Node homeNode = new Node(-1);
        private Node propertiesNode = new Node(-1);
        private Node rentalsNode = new Node(-1);
        private Node commercialNode = new Node(-1);
        private string error;

        public Node HomeNode
        {
            set
            {
                value = homeNode;
            }
            get
            {
                return homeNode;
            }
        }

        public Node PropertiesNode
        {
            set
            {
                value = propertiesNode;
            }
            get
            {
                return propertiesNode;
            }
        }

        public Node RentalsNode
        {
            set
            {
                value = rentalsNode;
            }
            get
            {
                return rentalsNode;
            }
        }

        public Node CommercialNode
        {
            set
            {
                value = commercialNode;
            }
            get
            {
                return commercialNode;
            }
        }

        public string Error
        {
            set
            {
                value = error;
            }
            get
            {
                return error;
            }
        }

        public bool InterfaceValid = false;

        public Authentication()
        {
            var node = new Node(-1);
            foreach (Node childNode in node.Children)
            {
                var child = childNode;
                if (child.NodeTypeAlias == "Home")
                {
                    homeNode = child;
                    break;
                }
            }

            foreach (Node childNode in homeNode.Children)
            {
                var child = childNode;
                if (child.NodeTypeAlias == "Properties")
                {
                    if (child.Name.ToLower().Contains("sale"))
                    {
                        propertiesNode = child;
                    }
                    else if (child.Name.ToLower().Contains("rent"))
                    {
                        rentalsNode = child;
                    }
                    else if (child.Name.ToLower().Contains("commercial"))
                    {
                        commercialNode = child;
                    }
                }

                if (propertiesNode.Id != -1 && rentalsNode.Id != -1 && commercialNode.Id != -1)
                {
                    break;
                }
            }

            username = homeNode.GetProperty("username").Value;
            password = homeNode.GetProperty("password").Value;
            dataFeedID = homeNode.GetProperty("dataFeedID").Value;
            APIVersion = homeNode.GetProperty("vebraAPIVersion").Value;

            InterfaceValid = (!String.IsNullOrEmpty(username) && !String.IsNullOrEmpty(password) && !String.IsNullOrEmpty(dataFeedID));

            baseUrl = "http://webservices.vebra.com/export/" + dataFeedID + "/v" + APIVersion;
            branchUrl = baseUrl + "/branch";
        }

        public void ForceGetToken()
        {
            XmlDocument branchesXml = new XmlDocument();

            HttpWebRequest branchUrlRequest = CreateVebraRequest(branchUrl);
            branchesXml = GetXmlFromRequest(branchUrlRequest, branchPath);
        }

        public XmlDocument GetBranchesXml()
        {
            XmlDocument branchesXml = new XmlDocument();

            HttpWebRequest branchUrlRequest = CreateVebraRequest(branchUrl);
            branchesXml = GetXmlFromRequest(branchUrlRequest, branchPath, true);

            return branchesXml;
        }

        public List<XmlDocument> GetBranchXmlList()
        {
            List<XmlDocument> branches = new List<XmlDocument>();
            XmlDocument branchXml = GetBranchesXml();

            XmlNodeList branchNodes = branchXml.GetElementsByTagName("branch");
            foreach (XmlNode node in branchNodes)
            {
                string url = null;
                string branchid = null;

                XmlElement branchElement = (XmlElement)node;
                branchid = branchElement.GetElementsByTagName("branchid")[0].InnerText;
                url = branchElement.GetElementsByTagName("url")[0].InnerText;

                HttpWebRequest branchRequest = CreateVebraRequest(url);
                XmlDocument branch = GetXmlFromRequest(branchRequest, branchesPath + branchid + ".xml", true);
                branches.Add(branch);
            }

            return branches;
        }

        public List<XmlDocument> GetPropertiesXmlList()
        {
            List<XmlDocument> properties = new List<XmlDocument>();
            XmlDocument branchXml = GetBranchesXml();
            string branchConfig = homeNode.GetProperty("branches").Value;

            XmlNodeList branchNodes = branchXml.GetElementsByTagName("branch");
            foreach (XmlNode node in branchNodes)
            {
                XmlElement branchElement = (XmlElement)node;
                if (branchConfig.Contains("~" + branchElement.GetElementsByTagName("branchid")[0].InnerText + "~1"))
                {
                    string url = branchElement.GetElementsByTagName("url")[0].InnerText + "/property";
                    string branchid = branchElement.GetElementsByTagName("branchid")[0].InnerText;

                    HttpWebRequest propertiesRequest = CreateVebraRequest(url);
                    XmlDocument propertiesXml = GetXmlFromRequest(propertiesRequest, branchesPath + branchid + "-properties.xml");
                    properties.Add(propertiesXml);
                }
            }

            return properties;
        }

        public XmlDocument GetChangedPropertiesXmlList()
        {
            ForceGetToken();

            string changedPropertiesPath = propertiesPath + "/changed-properties.xml";

            DateTime lastUpdated = DateTime.Now.AddMonths(-1);

            if (File.Exists(HttpContext.Current.Server.MapPath(changedPropertiesPath)))
            {
                lastUpdated = Helpers.FileLastModified(changedPropertiesPath);
            }

            string url = baseUrl + "/property/" + lastUpdated.ToString("yyyy/MM/dd/HH/mm/ss");

            HttpWebRequest changedPropertiesRequest = CreateVebraRequest(url);
            XmlDocument changedPropertiesXml = new XmlDocument();
            try
            {
                changedPropertiesXml = GetXmlFromRequest(changedPropertiesRequest, changedPropertiesPath);
            }
            catch
            {
                //Most likely a 304 occured. Thats fine, it means there was nothing to change.
            }

            return changedPropertiesXml;
        }

        public void UpdateProperties()
        {
            List<XmlDocument> properties = new List<XmlDocument>();
            XmlDocument property = GetChangedPropertiesXmlList();

            XmlNodeList propertyNodes = property.GetElementsByTagName("property");
            foreach (XmlNode node in propertyNodes)
            {
                XmlElement propertyElement = (XmlElement)node;
                string propid = propertyElement.GetElementsByTagName("propid")[0].InnerText;
                string action = propertyElement.GetElementsByTagName("action")[0].InnerText;
                string lastchanged = propertyElement.GetElementsByTagName("lastchanged")[0].InnerText;
                DateTime lastChangedDate = Convert.ToDateTime(lastchanged);
                string propertyPath = propertiesPath + propid + ".xml";

                List<INode> propertyNodeList = new List<INode>();
                propertyNodeList = propertiesNode.ChildrenAsList.Where(x => x.NodeTypeAlias == "Property" && (x.GetProperty("id").Value == propid)).ToList();

                if (propertyNodeList.Count == 0)
                {
                    propertyNodeList = commercialNode.ChildrenAsList.Where(x => x.NodeTypeAlias == "Property" && (x.GetProperty("id").Value == propid)).ToList();
                }

                if (propertyNodeList.Count == 0)
                {
                    propertyNodeList = rentalsNode.ChildrenAsList.Where(x => x.NodeTypeAlias == "Property" && (x.GetProperty("id").Value == propid)).ToList();
                }

                if (propertyNodeList.Count != 0)
                {
                    try
                    {

                        Document propertyDocument = new Document(propertyNodeList[0].Id);

                        foreach (Document child in propertyDocument.Children)
                        {
                            Media mediaToDelete = new Media(Convert.ToInt32(child.getProperty("filePicker").Value));
                            mediaToDelete.delete();
                            child.delete();
                            umbraco.library.RefreshContent();
                        }

                        propertyDocument.delete();
                        umbraco.library.RefreshContent();

                        if (File.Exists(HttpContext.Current.Server.MapPath(propertyPath)))
                        {
                            File.Delete(HttpContext.Current.Server.MapPath(propertyPath));
                        }

                        if (action.ToLower() == "updated")
                        {
                            string url = propertyElement.GetElementsByTagName("url")[0].InnerText;
                            HttpWebRequest propertyRequest = CreateVebraRequest(url);
                            XmlDocument propertyXml = new XmlDocument();
                            propertyXml = GetXmlFromRequest(propertyRequest, propertyPath);
                            properties.Add(propertyXml);
                        }
                    }

                    catch
                    {
                        //unhandled exception
                    }
                }
            }

            if (properties.Count > 0)
            {
                CreateProperties(properties);
            }
        }

        public List<XmlDocument> GetPropertiesXmlFromPropertyList()
        {
            List<XmlDocument> properties = new List<XmlDocument>();
            List<XmlDocument> propertiesList = GetPropertiesXmlList();

            foreach (XmlDocument property in propertiesList)
            {
                XmlNodeList propertyNodes = property.GetElementsByTagName("property");
                foreach (XmlNode node in propertyNodes)
                {
                    XmlElement propertyElement = (XmlElement)node;
                    string prop_id = propertyElement.GetElementsByTagName("prop_id")[0].InnerText;
                    string url = propertyElement.GetElementsByTagName("url")[0].InnerText;
                    string lastchanged = propertyElement.GetElementsByTagName("lastchanged")[0].InnerText;
                    string propertyPath = propertiesPath + prop_id + ".xml";
                    DateTime lastChangedDate = Convert.ToDateTime(lastchanged);

                    HttpWebRequest propertyRequest = CreateVebraRequest(url);
                    XmlDocument propertyXml = new XmlDocument();

                    if (DateTime.Compare(lastChangedDate, Helpers.FileLastModified(propertyPath)) > 0 || !File.Exists(HttpContext.Current.Server.MapPath(propertyPath)))
                    {
                        propertyXml = GetXmlFromRequest(propertyRequest, propertyPath);
                    }
                    else
                    {
                        propertyXml = GetXmlFromRequest(propertyRequest, propertyPath, true);
                    }

                    properties.Add(propertyXml);
                }
            }

            return properties;
        }

        public void CreateProperties(List<XmlDocument> propertiesToCreate)
        {
            DateTimeFormatInfo ukDtfi = new CultureInfo("en-GB", false).DateTimeFormat;
            DocumentType docType = DocumentType.GetByAlias("Property");
            User user = User.GetUser(0);

            foreach (XmlDocument property in propertiesToCreate)
            {
                XmlNode propertyNode = property.GetElementsByTagName("property")[0];
                XmlNode priceNode = property.SelectNodes("property/price")[0];

                string documentTitle;
                string id = (propertyNode.Attributes["id"] != null && propertyNode.Attributes["id"].Value != null) ? propertyNode.Attributes["id"].Value : "";
                string propertyPath = propertiesPath + id + ".xml";

                string propertyid = (propertyNode.Attributes["propertyid"] != null && propertyNode.Attributes["propertyid"].Value != null) ? propertyNode.Attributes["propertyid"].Value : "";
                string firmid = (propertyNode.Attributes["firmid"] != null && propertyNode.Attributes["firmid"].Value != null) ? propertyNode.Attributes["firmid"].Value : "";
                string branchid = (propertyNode.Attributes["branchid"] != null && propertyNode.Attributes["branchid"].Value != null) ? propertyNode.Attributes["branchid"].Value : "";
                string database = (propertyNode.Attributes["database"] != null && propertyNode.Attributes["database"].Value != null) ? propertyNode.Attributes["database"].Value : "";
                bool featured = (propertyNode.Attributes["featured"] != null) ? propertyNode.Attributes["featured"].Value == "1" : false;
                string reference = (property.SelectNodes("property/reference/agents")[0] != null) ? property.SelectNodes("property/reference/agents")[0].InnerText : "";
                string addressName = (property.SelectNodes("property/address/name")[0] != null) ? property.SelectNodes("property/address/name")[0].InnerText : "";
                string addressStreet = (property.SelectNodes("property/address/street")[0] != null) ? property.SelectNodes("property/address/street")[0].InnerText : "";
                string addressLocality = (property.SelectNodes("property/address/locality")[0] != null) ? property.SelectNodes("property/address/locality")[0].InnerText : "";
                string addressTown = (property.SelectNodes("property/address/town")[0] != null) ? property.SelectNodes("property/address/town")[0].InnerText : "";
                string addressCounty = (property.SelectNodes("property/address/county")[0] != null) ? property.SelectNodes("property/address/county")[0].InnerText : "";
                string addressPostcode = (property.SelectNodes("property/address/postcode")[0] != null) ? property.SelectNodes("property/address/postcode")[0].InnerText : "";
                string addressCustomLocation = (property.SelectNodes("property/address/custom_location")[0] != null) ? property.SelectNodes("property/address/custom_location")[0].InnerText : "";
                string addressDisplay = (property.SelectNodes("property/address/display")[0] != null) ? property.SelectNodes("property/address/display")[0].InnerText : "";
                string price = (priceNode != null) ? priceNode.InnerText : "";
                string qualifier = (priceNode.Attributes["qualifier"] != null && priceNode.Attributes["qualifier"].Value != null) ? priceNode.Attributes["qualifier"].Value : "";
                string currency = (priceNode.Attributes["currency"] != null && priceNode.Attributes["currency"].Value != null) ? priceNode.Attributes["currency"].Value : "";
                string rent = (priceNode.Attributes["rent"] != null && priceNode.Attributes["rent"].Value != null) ? priceNode.Attributes["rent"].Value : "";
                string rm_qualifier = (property.SelectNodes("property/rm_qulifier")[0] != null) ? property.SelectNodes("property/rm_qulifier")[0].InnerText : "";
                string available = (property.SelectNodes("property/available")[0] != null) ? property.SelectNodes("property/available")[0].InnerText : "";
                string uploaded = (property.SelectNodes("property/uploaded")[0] != null) ? property.SelectNodes("property/uploaded")[0].InnerText : "";
                string longitude = (property.SelectNodes("property/longitude")[0] != null) ? property.SelectNodes("property/longitude")[0].InnerText : "";
                string latitude = (property.SelectNodes("property/latitude")[0] != null) ? property.SelectNodes("property/latitude")[0].InnerText : "";
                string easting = (property.SelectNodes("property/easting")[0] != null) ? property.SelectNodes("property/easting")[0].InnerText : "";
                string northing = (property.SelectNodes("property/northing")[0] != null) ? property.SelectNodes("property/northing")[0].InnerText : "";
                string web_status = (property.SelectNodes("property/web_status")[0] != null) ? property.SelectNodes("property/web_status")[0].InnerText : "";
                string custom_status = (property.SelectNodes("property/custom_status")[0] != null) ? property.SelectNodes("property/custom_status")[0].InnerText : "";
                string comm_rent = (property.SelectNodes("property/comm_rent")[0] != null) ? property.SelectNodes("property/comm_rent")[0].InnerText : "";
                string premium = (property.SelectNodes("property/premium")[0] != null) ? property.SelectNodes("property/premium")[0].InnerText : "";
                string service_charge = (property.SelectNodes("property/service_charge")[0] != null) ? property.SelectNodes("property/service_charge")[0].InnerText : "";
                string rateable_value = (property.SelectNodes("property/rateable_value")[0] != null) ? property.SelectNodes("property/rateable_value")[0].InnerText : "";
                string type = (property.SelectNodes("property/type")[0] != null) ? property.SelectNodes("property/type")[0].InnerText : "";
                string furnished = (property.SelectNodes("property/furnished")[0] != null) ? property.SelectNodes("property/furnished")[0].InnerText : "";
                string rm_type = (property.SelectNodes("property/rm_type")[0] != null) ? property.SelectNodes("property/rm_type")[0].InnerText : "";
                string let_bond = (property.SelectNodes("property/let_bond")[0].InnerText != null) ? property.SelectNodes("property/let_bond")[0].InnerText : "";
                string rm_let_type_id = (property.SelectNodes("property/rm_let_type_id")[0] != null) ? property.SelectNodes("property/rm_let_type_id")[0].InnerText : "";
                string bedrooms = (property.SelectNodes("property/bedrooms")[0] != null) ? property.SelectNodes("property/bedrooms")[0].InnerText : "";
                string receptions = (property.SelectNodes("property/receptions")[0] != null) ? property.SelectNodes("property/receptions")[0].InnerText : "";
                string bathrooms = (property.SelectNodes("property/bathrooms")[0] != null) ? property.SelectNodes("property/bathrooms")[0].InnerText : "";
                string userfield1 = (property.SelectNodes("property/userfield1")[0] != null) ? property.SelectNodes("property/userfield1")[0].InnerText : "";
                string userfield2 = (property.SelectNodes("property/userfield2")[0] != null) ? property.SelectNodes("property/userfield2")[0].InnerText : "";
                string solddate = (property.SelectNodes("property/solddate")[0] != null) ? property.SelectNodes("property/solddate")[0].InnerText : "";
                string leaseend = (property.SelectNodes("property/leaseend")[0] != null) ? property.SelectNodes("property/leaseend")[0].InnerText : "";
                string instucted = (property.SelectNodes("property/instucted")[0] != null) ? property.SelectNodes("property/instucted")[0].InnerText : "";
                string soldprice = (property.SelectNodes("property/soldprice")[0] != null) ? property.SelectNodes("property/soldprice")[0].InnerText : "";
                bool garden = (property.SelectNodes("property/garden")[0] != null) ? property.SelectNodes("property/garden")[0].InnerText == "1" : false;
                bool parking = (property.SelectNodes("property/parking")[0] != null) ? property.SelectNodes("property/parking")[0].InnerText == "1" : false;
                string groundrent = (property.SelectNodes("property/groundrent")[0] != null) ? property.SelectNodes("property/groundrent")[0].InnerText : "";
                string commission = (property.SelectNodes("property/commission")[0] != null) ? property.SelectNodes("property/commission")[0].InnerText : "";

                string areaMetric = "";
                string areaImperial = "";

                foreach (XmlNode node in property.SelectNodes("property/area"))
                {
                    if (node.Attributes["measure"].Value == "metric")
                    {
                        areaMetric = "min:" + ((node.SelectNodes("min")[0] != null) ? node.SelectNodes("min")[0].InnerText : "0") + ",max:" + ((node.SelectNodes("min") != null) ? node.SelectNodes("min")[0].InnerText : "0");
                    }
                    else
                    {
                        areaImperial = "min:" + ((node.SelectNodes("min")[0] != null) ? node.SelectNodes("min")[0].InnerText : "0") + ",max:" + ((node.SelectNodes("min") != null) ? node.SelectNodes("min")[0].InnerText : "0");
                    }
                }

                string description = (property.SelectNodes("property/description")[0] != null) ? property.SelectNodes("property/description")[0].InnerText : "";
                string energyEfficiencyCurrent = (property.SelectNodes("property/hip/energy_performance/energy_efficiency/current")[0] != null) ? property.SelectNodes("property/hip/energy_performance/energy_efficiency/current")[0].InnerText : "";
                string energyEfficiencyPotential = (property.SelectNodes("property/hip/energy_performance/energy_efficiency/potential")[0] != null) ? property.SelectNodes("property/hip/energy_performance/energy_efficiency/potential")[0].InnerText : "";
                string environmentalImpactCurrent = (property.SelectNodes("property/hip/energy_performance/environmental_impact/current")[0] != null) ? property.SelectNodes("property/hip/energy_performance/environmental_impact/current")[0].InnerText : "";
                string environmentalImpactPotential = (property.SelectNodes("property/hip/energy_performance/environmental_impact/potential")[0] != null) ? property.SelectNodes("property/hip/energy_performance/environmental_impact/potential")[0].InnerText : "";
                string content = "";

                foreach (XmlNode paragraph in property.SelectNodes("property/paragraphs/paragraph"))
                {
                    if (paragraph.SelectNodes("name")[0] != null)
                    {
                        content += "<h2>" + paragraph.SelectNodes("name")[0].InnerText + "</h2>";
                    }

                    if (paragraph.SelectNodes("text")[0] != null)
                    {
                        content += "<p>" + paragraph.SelectNodes("text")[0].InnerText + "</p>";
                    }

                    if (paragraph.SelectNodes("dimensions/metric")[0] != null || paragraph.SelectNodes("dimensions/imperial")[0] != null || paragraph.SelectNodes("dimensions/mixed")[0] != null)
                    {
                        if (paragraph.SelectNodes("dimensions/metric")[0] != null && !String.IsNullOrEmpty(paragraph.SelectNodes("dimensions/metric")[0].InnerText))
                        {
                            content += "<p class=\"metric\"><span>Metric:</span> " + paragraph.SelectNodes("dimensions/metric")[0].InnerText + "</p>";
                        }

                        if (paragraph.SelectNodes("dimensions/imperial")[0] != null && !String.IsNullOrEmpty(paragraph.SelectNodes("dimensions/imperial")[0].InnerText))
                        {
                            content += "<p class=\"imperial\"><span>Imperial:</span> " + paragraph.SelectNodes("dimensions/imperial")[0].InnerText + "</p>";
                        }

                        if (paragraph.SelectNodes("dimensions/mixed")[0] != null && !String.IsNullOrEmpty(paragraph.SelectNodes("dimensions/mixed")[0].InnerText))
                        {
                            content += "<p class=\"mixed\"><span>Mixed:</span> " + paragraph.SelectNodes("dimensions/mixed")[0].InnerText + "</p>";
                        }
                    }
                }

                string bullets = "";

                bullets += "<ul>";

                foreach (XmlNode bullet in property.SelectNodes("property/bullets/bullet"))
                {
                    bullets += "<li>" + ((bullet != null && !String.IsNullOrEmpty(bullet.InnerText)) ? bullet.InnerText : "") + "</li>";
                }

                bullets += "</ul>";

                bool propertyIsForRent = false;
                bool propertyIsCommercial = false;
                bool propertyIsForSale = true;

                if (rent != "" || available != "01/01/1900" || (let_bond != "" && let_bond != "0") || leaseend != "")
                {
                    propertyIsForRent = true;
                    propertyIsCommercial = false;
                    propertyIsForSale = false;
                }

                if (custom_status != "" || comm_rent != "" || premium != "")
                {
                    propertyIsForRent = false;
                    propertyIsCommercial = true;
                    propertyIsForSale = false;
                }

                string saleType = "for sale";

                if (propertyIsForRent)
                {
                    saleType = "for rent";
                }

                if (propertyIsCommercial)
                {
                    documentTitle = "commerical property available";
                }
                else
                {
                    if (bedrooms != "0")
                    {
                        documentTitle = bedrooms + " bedroom property " + saleType + " (" + propertyid  + ")";
                    }
                    else
                    {
                        documentTitle = "Property " + saleType + " (" + propertyid + ")";
                    }
                }

                if (id != "")
                {
                    bool createProperty = false;
                    bool updateProperty = false;
                    int parentId = propertiesNode.Id;
                    List<INode> propertyNodeList = new List<INode>();

                    //Check if document is already created
                    if (propertyIsForSale)
                    {
                        propertyNodeList = propertiesNode.ChildrenAsList.Where(x => x.NodeTypeAlias == "Property" && (x.GetProperty("id").Value == id)).ToList();
                        if (propertyNodeList.Count() == 0)
                        {
                            createProperty = true;
                            parentId = propertiesNode.Id;
                            if (parentId == -1)
                            {
                                createProperty = false;
                            }
                        }
                    }
                    else if (propertyIsForRent)
                    {
                        propertyNodeList = rentalsNode.ChildrenAsList.Where(x => x.NodeTypeAlias == "Property" && (x.GetProperty("id").Value == id)).ToList();
                        if (propertyNodeList.Count() == 0)
                        {
                            createProperty = true;
                            parentId = rentalsNode.Id;
                            if (parentId == -1)
                            {
                                createProperty = false;
                            }
                        }
                    }
                    else if (propertyIsCommercial)
                    {
                        propertyNodeList = commercialNode.ChildrenAsList.Where(x => x.NodeTypeAlias == "Property" && (x.GetProperty("id").Value == id)).ToList();
                        if (propertyNodeList.Count() == 0)
                        {
                            createProperty = true;
                            parentId = commercialNode.Id;
                            if (parentId == -1)
                            {
                                createProperty = false;
                            }
                        }
                    }

                    //Check creation date.
                    if (!createProperty && !String.IsNullOrEmpty(propertyNodeList[0].Name))
                    {
                        DateTime lastUpdated = propertyNodeList[0].UpdateDate;

                        if (DateTime.Compare(Helpers.FileLastModified(propertyPath), lastUpdated) > 0 || !File.Exists(HttpContext.Current.Server.MapPath(propertyPath)))
                        {
                            createProperty = true;
                            updateProperty = true;
                        }
                    }

                    if (createProperty)
                    {
                        Document propertyDocument;

                        if (updateProperty)
                        {
                            propertyDocument = new Document(propertyNodeList[0].Id);
                        }
                        else
                        {
                            propertyDocument = Document.MakeNew(documentTitle, docType, user, parentId);
                        }

                        propertyDocument.getProperty("agents").Value = reference;
                        propertyDocument.getProperty("description").Value = description;
                        propertyDocument.getProperty("bodyText").Value = content;
                        propertyDocument.getProperty("bullets").Value = bullets;
                        propertyDocument.getProperty("featured").Value = featured;
                        propertyDocument.getProperty("type").Value = type;
                        propertyDocument.getProperty("garden").Value = garden;
                        propertyDocument.getProperty("parking").Value = parking;
                        propertyDocument.getProperty("bedrooms").Value = bedrooms;
                        propertyDocument.getProperty("receptions").Value = receptions;
                        propertyDocument.getProperty("bathrooms").Value = bathrooms;

                        PreValue furnishedTypes = new PreValue();

                        foreach (PreValue preValue in PreValues.GetPreValues(DataTypeDefinition.GetAll().First(x => x.Text == "Furnished Types").DataType.DataTypeDefinitionId).Values)
                        {
                            if (preValue.Value == "Not Specified" && furnished == "3")
                            {
                                furnishedTypes = preValue;
                                break;
                            }
                            else if (preValue.Value == "Furnished" && furnished == "0")
                            {
                                furnishedTypes = preValue;
                                break;
                            }
                            else if (preValue.Value == "Part Furnished" && furnished == "2")
                            {
                                furnishedTypes = preValue;
                                break;
                            }
                            else if (preValue.Value == "Un-Furnished" && furnished == "4")
                            {
                                furnishedTypes = preValue;
                                break;
                            }
                            else if (preValue.Value == "Furnished / Un-Furnished" && furnished == "5")
                            {
                                furnishedTypes = preValue;
                                break;
                            }
                        }

                        propertyDocument.getProperty("furnishedType").Value = furnishedTypes.Id;

                        PreValue salesStatus = new PreValue();

                        foreach (PreValue preValue in PreValues.GetPreValues(DataTypeDefinition.GetAll().First(x => x.Text == "For Sale Status").DataType.DataTypeDefinitionId).Values)
                        {
                            if (preValue.Value == "For Sale" && web_status == "0")
                            {
                                salesStatus = preValue;
                                break;
                            }
                            else if (preValue.Value == "Under Offer" && web_status == "1")
                            {
                                salesStatus = preValue;
                                break;
                            }
                            else if (preValue.Value == "Sold" && web_status == "2")
                            {
                                salesStatus = preValue;
                                break;
                            }
                            else if (preValue.Value == "SSTC" && web_status == "3")
                            {
                                salesStatus = preValue;
                                break;
                            }
                            else if (preValue.Value == "For Sale By Auction" && web_status == "4")
                            {
                                salesStatus = preValue;
                                break;
                            }
                            else if (preValue.Value == "Reserved" && web_status == "5")
                            {
                                salesStatus = preValue;
                                break;
                            }
                            else if (preValue.Value == "New Instruction" && web_status == "6")
                            {
                                salesStatus = preValue;
                                break;
                            }
                            else if (preValue.Value == "Just on Market" && web_status == "7")
                            {
                                salesStatus = preValue;
                                break;
                            }
                            else if (preValue.Value == "Price Reduction" && web_status == "8")
                            {
                                salesStatus = preValue;
                                break;
                            }
                            else if (preValue.Value == "Keen to Sell" && web_status == "9")
                            {
                                salesStatus = preValue;
                                break;
                            }
                            else if (preValue.Value == "No Chain" && web_status == "10")
                            {
                                salesStatus = preValue;
                                break;
                            }
                            else if (preValue.Value == "Vendor will pay stamp duty" && web_status == "11")
                            {
                                salesStatus = preValue;
                                break;
                            }
                            else if (preValue.Value == "Offers in the region of" && web_status == "12")
                            {
                                salesStatus = preValue;
                                break;
                            }
                            else if (preValue.Value == "Guide Price" && web_status == "13")
                            {
                                salesStatus = preValue;
                                break;
                            }
                        }
                        if (propertyIsForSale)
                        {
                            propertyDocument.getProperty("salesStatus").Value = salesStatus.Id;
                        }
                        propertyDocument.getProperty("branchid").Value = branchid;
                        propertyDocument.getProperty("propertyName").Value = addressName;
                        propertyDocument.getProperty("propertyStreet").Value = addressStreet;
                        propertyDocument.getProperty("propertyLocality").Value = addressLocality;
                        propertyDocument.getProperty("propertyCustomLocation").Value = addressCustomLocation;
                        propertyDocument.getProperty("propertyTown").Value = addressTown;
                        propertyDocument.getProperty("propertyCounty").Value = addressCounty;
                        propertyDocument.getProperty("propertyPostcode").Value = addressPostcode;
                        propertyDocument.getProperty("propertyDisplay").Value = addressDisplay;
                        propertyDocument.getProperty("longitude").Value = longitude;
                        propertyDocument.getProperty("latitude").Value = latitude;
                        propertyDocument.getProperty("easting").Value = easting;
                        propertyDocument.getProperty("northing").Value = northing;
                        propertyDocument.getProperty("price").Value = Convert.ToInt64(price);
                        propertyDocument.getProperty("currency").Value = currency;
                        propertyDocument.getProperty("qualifier").Value = qualifier;
                        propertyDocument.getProperty("service_charge").Value = service_charge;

                        if (instucted != "")
                        {
                            propertyDocument.getProperty("instucted").Value = Convert.ToDateTime(instucted);
                        }

                        propertyDocument.getProperty("groundrent").Value = groundrent;
                        propertyDocument.getProperty("commission").Value = commission;
                        propertyDocument.getProperty("rateable_value").Value = commission;

                        if (solddate != "")
                        {
                            propertyDocument.getProperty("solddate").Value = Convert.ToDateTime(solddate);
                        }

                        propertyDocument.getProperty("soldprice").Value = soldprice;
                        propertyDocument.getProperty("propertyIsForRent").Value = propertyIsForRent;

                        if (available != "" && available != "01/01/1900")
                        {
                            propertyDocument.getProperty("available").Value = Convert.ToDateTime(available);
                        }

                        propertyDocument.getProperty("let_bond").Value = let_bond;
                        propertyDocument.getProperty("rent").Value = rent;

                        if (leaseend != "")
                        {
                            propertyDocument.getProperty("leaseend").Value = Convert.ToDateTime(leaseend);
                        }

                        PreValue rentalStatus = new PreValue();

                        foreach (PreValue preValue in PreValues.GetPreValues(DataTypeDefinition.GetAll().First(x => x.Text == "Let Types").DataType.DataTypeDefinitionId).Values)
                        {
                            if (preValue.Value == "Not Specified" && furnished == "0")
                            {
                                rentalStatus = preValue;
                                break;
                            }
                            else if (preValue.Value == "Long Term" && furnished == "1")
                            {
                                rentalStatus = preValue;
                                break;
                            }
                            else if (preValue.Value == "Short Term" && furnished == "2")
                            {
                                rentalStatus = preValue;
                                break;
                            }
                            else if (preValue.Value == "Student" && furnished == "3")
                            {
                                rentalStatus = preValue;
                                break;
                            }
                            else if (preValue.Value == "Commercial" && furnished == "4")
                            {
                                rentalStatus = preValue;
                                break;
                            }
                        }
                        if (propertyIsForRent)
                        {
                            propertyDocument.getProperty("letTypes").Value = rentalStatus.Id;
                        }
                        propertyDocument.getProperty("propertyIsCommercial").Value = propertyIsCommercial;
                        propertyDocument.getProperty("comm_rent").Value = comm_rent;
                        propertyDocument.getProperty("premium").Value = premium;
                        propertyDocument.getProperty("areaImperial").Value = areaImperial;
                        propertyDocument.getProperty("areaMetric").Value = areaMetric;
                        propertyDocument.getProperty("custom_status").Value = custom_status;
                        propertyDocument.getProperty("energyEfficiencyCurrent").Value = energyEfficiencyCurrent;
                        propertyDocument.getProperty("energyEfficiencyPotential").Value = energyEfficiencyPotential;
                        propertyDocument.getProperty("environmentalImpactCurrent").Value = environmentalImpactCurrent;
                        propertyDocument.getProperty("environmentalImpactPotential").Value = environmentalImpactPotential;

                        propertyDocument.getProperty("id").Value = id;
                        propertyDocument.getProperty("propertyid").Value = propertyid;
                        propertyDocument.getProperty("firmid").Value = firmid;
                        propertyDocument.getProperty("database").Value = database;
                        propertyDocument.getProperty("rm_qualifier").Value = rm_qualifier;

                        try
                        {
                            if (uploaded != "")
                            {
                                propertyDocument.getProperty("uploaded").Value = Convert.ToDateTime(uploaded, ukDtfi);
                            }
                        }
                        catch
                        {
                            propertyDocument.getProperty("uploaded").Value = DateTime.Now;
                        }

                        propertyDocument.getProperty("web_status").Value = web_status;
                        propertyDocument.getProperty("furnished").Value = furnished;
                        propertyDocument.getProperty("rm_type").Value = rm_type;
                        propertyDocument.getProperty("rm_let_type_id").Value = rm_let_type_id;

                        PreValue letTypes = new PreValue();

                        foreach (PreValue preValue in PreValues.GetPreValues(DataTypeDefinition.GetAll().First(x => x.Text == "To Let Status").DataType.DataTypeDefinitionId).Values)
                        {
                            if (preValue.Value == "To Let" && furnished == "0")
                            {
                                letTypes = preValue;
                                break;
                            }
                            else if (preValue.Value == "Let" && furnished == "1")
                            {
                                letTypes = preValue;
                                break;
                            }
                            else if (preValue.Value == "Under Offer" && furnished == "2")
                            {
                                letTypes = preValue;
                                break;
                            }
                            else if (preValue.Value == "Reserved" && furnished == "3")
                            {
                                letTypes = preValue;
                                break;
                            }
                            else if (preValue.Value == "Let Agreed" && furnished == "4")
                            {
                                letTypes = preValue;
                                break;
                            }
                        }
                        if (propertyIsForRent)
                        {
                            propertyDocument.getProperty("rentalStatus").Value = letTypes.Id;
                        }
                        propertyDocument.getProperty("userfield1").Value = userfield1;
                        propertyDocument.getProperty("userfield2").Value = userfield2;

                        propertyDocument.Publish(user);
                        umbraco.library.UpdateDocumentCache(propertyDocument.Id);

                        if (updateProperty)
                        {
                            foreach (Document child in propertyDocument.Children)
                            {
                                Media mediaToDelete = new Media(Convert.ToInt32(child.getProperty("filePicker").Value));
                                mediaToDelete.delete();
                                child.delete();
                                umbraco.library.RefreshContent();
                            }
                        }

                        foreach (XmlNode file in property.SelectNodes("property/files/file"))
                        {
                            XmlElement propertyElement = (XmlElement)file;
                            string name = (propertyElement.GetElementsByTagName("name")[0] != null) ? propertyElement.GetElementsByTagName("name")[0].InnerText : "";
                            string url = (propertyElement.GetElementsByTagName("url")[0] != null) ? propertyElement.GetElementsByTagName("url")[0].InnerText : "";
                            string updated = (propertyElement.GetElementsByTagName("updated")[0] != null) ? propertyElement.GetElementsByTagName("updated")[0].InnerText : "";
                            string fileId = file.Attributes["id"].Value;
                            string fileType = file.Attributes["type"].Value;
                            string[] splitUrl = url.Split('.');
                            string fileExtension = splitUrl.Last();

                            string mediaName = "";

                            if (name != "")
                            {
                                mediaName = name;
                            }
                            else
                            {
                                mediaName = propertyid + "-" + fileId;
                            }

                            string filePath = filesPath + propertyid + "-" + fileId + "." + fileExtension;
                            bool fileDownloaded = false;

                            Directory.CreateDirectory(Path.GetDirectoryName(HttpContext.Current.Server.MapPath(filePath)));
                            WebClient webClient = new WebClient();
                            try
                            {
                                webClient.DownloadFile(url, HttpContext.Current.Server.MapPath(filePath));
                                fileDownloaded = true;
                            }
                            catch
                            {
                                fileDownloaded = false;
                            }
                            finally
                            {
                                webClient.Dispose();
                            }

                            if (fileDownloaded)
                            {
                                string fileAliasType = "image";

                                if (fileType != "0")
                                {
                                    fileAliasType = "file";
                                }

                                Media mediaItem = Media.MakeNew(mediaName, MediaType.GetByAlias(fileAliasType), user, -1);

                                System.IO.FileInfo fileInfo = new FileInfo(HttpContext.Current.Server.MapPath(filePath));

                                string fileNewPath = "/media/" + mediaItem.getProperty("umbracoFile").Id + "/" + fileInfo.Name;
                                Directory.CreateDirectory(Path.GetDirectoryName(HttpContext.Current.Server.MapPath(fileNewPath)));
                                System.IO.File.Copy(HttpContext.Current.Server.MapPath(filePath), HttpContext.Current.Server.MapPath(fileNewPath), true);

                                mediaItem.getProperty("umbracoFile").Value = fileNewPath;
                                mediaItem.getProperty("umbracoExtension").Value = fileInfo.Extension;
                                mediaItem.getProperty("umbracoBytes").Value = fileInfo.Length;
                                mediaItem.XmlGenerate(new XmlDocument());

                                DocumentType mediaDocType = DocumentType.GetByAlias("PropertyFile");
                                Document mediaDocument = Document.MakeNew(mediaName, mediaDocType, user, propertyDocument.Id);
                                mediaDocument.getProperty("filePicker").Value = mediaItem.Id;
                                mediaDocument.getProperty("externalResource").Value = url;

                                PreValue fileTypes = new PreValue();

                                foreach (PreValue preValue in PreValues.GetPreValues(DataTypeDefinition.GetAll().First(x => x.Text == "File Types").DataType.DataTypeDefinitionId).Values)
                                {
                                    if (preValue.Value == "Image" && fileType == "0")
                                    {
                                        fileTypes = preValue;
                                        break;
                                    }
                                    else if (preValue.Value == "Map" && fileType == "1")
                                    {
                                        fileTypes = preValue;
                                        break;
                                    }
                                    else if (preValue.Value == "Floorplan" && fileType == "2")
                                    {
                                        fileTypes = preValue;
                                        break;
                                    }
                                    else if (preValue.Value == "Vebra 360 tour" && fileType == "3")
                                    {
                                        fileTypes = preValue;
                                        break;
                                    }
                                    else if (preValue.Value == "Ehouse" && fileType == "4")
                                    {
                                        fileTypes = preValue;
                                        break;
                                    }
                                    else if (preValue.Value == "Ipix" && fileType == "5")
                                    {
                                        fileTypes = preValue;
                                        break;
                                    }
                                    else if (preValue.Value == "PDF Details" && fileType == "7")
                                    {
                                        fileTypes = preValue;
                                        break;
                                    }
                                    else if (preValue.Value == "Energy Performance Certificate" && fileType == "8")
                                    {
                                        fileTypes = preValue;
                                        break;
                                    }
                                    else if (preValue.Value == "Home information pack" && fileType == "9")
                                    {
                                        fileTypes = preValue;
                                        break;
                                    }
                                }

                                mediaDocument.getProperty("fileType").Value = fileTypes.Id;
                                if (updated != "")
                                {
                                    mediaDocument.getProperty("fileUpdated").Value = Convert.ToDateTime(updated,ukDtfi);
                                }
                                else
                                {
                                    mediaDocument.getProperty("fileUpdated").Value = DateTime.Now;
                                }

                                mediaDocument.getProperty("id").Value = fileId;
                                mediaDocument.getProperty("vebraFileTypeID").Value = fileType;
                                mediaDocument.getProperty("vebraOrginalURL").Value = url;
                                mediaDocument.Publish(user);
                                umbraco.library.UpdateDocumentCache(mediaDocument.Id);
                            }
                        }
                    }
                }
            }
        }

        public void ResetCachedXmlDocuments()
        {
            XmlDocument branchXml = GetBranchesXml();

            XmlNodeList branchNodes = branchXml.GetElementsByTagName("branch");
            foreach (XmlNode node in branchNodes)
            {
                XmlElement branchElement = (XmlElement)node;
                string branchid = branchElement.GetElementsByTagName("branchid")[0].InnerText;
                File.Delete(HttpContext.Current.Server.MapPath(branchesPath + branchid + "-properties.xml"));
                File.Delete(HttpContext.Current.Server.MapPath(branchesPath + branchid + ".xml"));
            }
            File.Delete(HttpContext.Current.Server.MapPath(branchPath));
        }

        private XmlDocument GetXmlFromRequest(HttpWebRequest request, string path)
        {
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            SetToken(response);
            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                XmlTextReader xmlReader = new XmlTextReader(response.GetResponseStream());
                xmlDoc.Load(xmlReader);
                Helpers.WriteXMLToFile(path, xmlDoc);
            }
            catch
            {
                //Problem with stream

                //if this is a property stream, advise the customer
                if (path.Contains("-properties.xml"))
                {
                    string[] splitPath = path.Split('/');
                    string branchId = splitPath.Last().Replace("-properties.xml", "");
                    string branchConfig = homeNode.GetProperty("branches").Value.ToString();

                    Document homeDocument = new Document(homeNode.Id);

                    homeDocument.getProperty("branches").Value = branchConfig.Replace("~" + branchId + "~1~valid", "~" + branchId + "~0~not-valid");
                    homeDocument.Publish(User.GetCurrent());
                    error = "Error with getting properties from one or more branches. Check the branches tab on the home page for more information.";
                    umbraco.library.UpdateDocumentCache(homeDocument.Id);
                }

            }
            response.Close();

            return xmlDoc;
        }

        private XmlDocument GetXmlFromRequest(HttpWebRequest request, string path, bool checkExpiry)
        {
            XmlDocument xmlDoc = new XmlDocument();

            if (DateTime.Compare(request.IfModifiedSince, Helpers.FileLastModified(path)) > 0 || !File.Exists(HttpContext.Current.Server.MapPath(path)))
            {
                xmlDoc = GetXmlFromRequest(request, path);
            }
            else
            {
                xmlDoc.Load(HttpContext.Current.Server.MapPath(path));
            }

            return xmlDoc;
        }

        private void SetToken(HttpWebResponse response)
        {
            if (!tokenValid)
            {
                token = response.GetResponseHeader("Token");
                //write Token to file
                Helpers.WriteStringToFile(token, tokenPath);
                HttpRuntime.Cache.Insert("Token", token, null, DateTime.Now.AddHours(1), System.Web.Caching.Cache.NoSlidingExpiration);
            }
        }

        public HttpWebRequest CreateVebraRequest(string url)
        {
            token = null;
            tokenValid = (DateTime.Compare(Helpers.FileLastModified(tokenPath), DateTime.Now.AddHours(-1)) > 0);

            if (HttpRuntime.Cache["Token"] != null)
            {
                token = (string)HttpRuntime.Cache["Token"];
            }
            else if (tokenValid)
            {
                token = Helpers.ReadStringFromFile(tokenPath);
            }
            else
            {
                token = username + ":" + password;
            }

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            CredentialCache cache = new CredentialCache();
            cache.Add(new Uri(url), "Basic", new NetworkCredential(username,
            password));
            request.Credentials = cache;
            request.Headers.Add("Authorization", "Basic "
            + Convert.ToBase64String(new ASCIIEncoding().GetBytes(token)));

            return request;
        }
    }
}