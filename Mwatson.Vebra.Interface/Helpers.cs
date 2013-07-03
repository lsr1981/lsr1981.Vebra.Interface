using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml;

namespace MWatson.Vebra.Interface
{
    public class Helpers
    {
        public static void WriteStringToFile(string content, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(HttpContext.Current.Server.MapPath(path)));
            using (FileStream stream = new FileStream(HttpContext.Current.Server.MapPath(path), FileMode.Create))
            {
                using (TextWriter writer = new StreamWriter(stream))
                {
                    writer.WriteLine(content);
                }
            }
        }

        public static string ReadStringFromFile(string path)
        {
            string content = null;

            System.IO.StreamReader readToken =
            new System.IO.StreamReader(HttpContext.Current.Server.MapPath(path));

            content = readToken.ReadToEnd();

            readToken.Close();
            readToken.Dispose();

            return content.Replace("\r\n", "");
        }

        public static DateTime FileLastModified(string path)
        {
            DateTime modified = new DateTime();

            modified = File.GetLastWriteTime(HttpContext.Current.Server.MapPath(path));

            return modified;
        }

        public static void WriteXMLToFile(string path, XmlDocument xmlDoc)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(HttpContext.Current.Server.MapPath(path)));
            XmlTextWriter writer = new XmlTextWriter(HttpContext.Current.Server.MapPath(path), null);
            writer.Formatting = Formatting.Indented;
            xmlDoc.Save(writer);
            writer.Flush();
            writer.Close();
        }
    }
}