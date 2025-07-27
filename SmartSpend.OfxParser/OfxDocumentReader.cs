using Sgml;
using SgmlCore;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

namespace SmartSpend.OfxParser
{
    public static class OfxDocumentReader
    {
        public static async Task<XmlDocument> FromSgmlFileAsync(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                var content = await reader.ReadToEndAsync();
                using (var stringReader = new StringReader(content))
                {
                    var sgmlReader = new SgmlReader
                    {
                        DocType = "OFX",
                        InputStream = stringReader
                    };

                    var doc = new XmlDocument();
                    doc.Load(sgmlReader);
                    return doc;
                }
            }
        }
    }
}
