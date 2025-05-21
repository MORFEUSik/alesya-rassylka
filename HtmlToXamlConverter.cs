using System;
using System.IO;
using System.Text;
using System.Windows.Documents;
using System.Xml;

namespace alesya_rassylka
{
    public static class HtmlToXamlConverter
    {
        public static string ConvertHtmlToXaml(string htmlString, bool asFlowDocument)
        {
            if (string.IsNullOrWhiteSpace(htmlString))
            {
                System.Diagnostics.Debug.WriteLine("ConvertHtmlToXaml: Input HTML is empty.");
                return asFlowDocument
                    ? "<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" />"
                    : "<Section xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" FontFamily=\"Arial\" FontSize=\"12\" />";
            }

            string xamlString = string.Empty;

            try
            {
                // Оборачиваем HTML в XHTML
                string xhtml = "<?xml version=\"1.0\"?><html xmlns=\"http://www.w3.org/1999/xhtml\"><body>" + htmlString + "</body></html>";
                System.Diagnostics.Debug.WriteLine($"XHTML input: {xhtml}");

                // Загружаем в XmlDocument
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xhtml);

                // Настраиваем пространство имён
                XmlNamespaceManager nsMgr = new XmlNamespaceManager(xmlDoc.NameTable);
                nsMgr.AddNamespace("xhtml", "http://www.w3.org/1999/xhtml");

                var bodyNode = xmlDoc.SelectSingleNode("//xhtml:body", nsMgr);
                if (bodyNode == null)
                    throw new Exception("HTML does not contain a <body> tag.");

                // Создаём XAML
                StringBuilder xamlBuilder = new StringBuilder();
                XmlWriterSettings settings = new XmlWriterSettings
                {
                    Indent = true,
                    OmitXmlDeclaration = true
                };

                using (XmlWriter writer = XmlWriter.Create(xamlBuilder, settings))
                {
                    string wpfNamespace = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
                    writer.WriteStartElement(asFlowDocument ? "FlowDocument" : "Section", wpfNamespace);

                    foreach (XmlNode node in bodyNode.ChildNodes)
                    {
                        WriteNodeRecursive(writer, node);
                    }

                    writer.WriteEndElement();
                }

                xamlString = xamlBuilder.ToString();
                System.Diagnostics.Debug.WriteLine($"Generated XAML: {xamlString}");

                if (string.IsNullOrWhiteSpace(xamlString))
                {
                    throw new InvalidOperationException("Generated XAML is empty.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ConvertHtmlToXaml error: {ex.Message}");
                xamlString = $"<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph>Ошибка конвертации: {System.Security.SecurityElement.Escape(ex.Message)}</Paragraph></FlowDocument>";
            }

            return xamlString;
        }

        private static void WriteNodeRecursive(XmlWriter writer, XmlNode node)
        {
            if (node is XmlText textNode)
            {
                string text = textNode.Value?.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    writer.WriteStartElement("Run");
                    writer.WriteString(text);
                    writer.WriteEndElement();
                }
                return;
            }

            if (node is XmlElement element)
            {
                switch (element.Name.ToLower())
                {
                    case "p":
                        writer.WriteStartElement("Paragraph");
                        // Извлекаем стили из атрибутов HTML
                        string fontFamily = element.GetAttribute("data-font-family") ?? "Arial";
                        string fontSize = element.GetAttribute("data-font-size") ?? "12";
                        string textAlignment = element.GetAttribute("data-text-align") ?? "Left";
                        string fontWeight = element.GetAttribute("data-font-weight") ?? "Normal";
                        string fontStyle = element.GetAttribute("data-font-style") ?? "Normal";
                        string textDecorations = element.GetAttribute("data-text-decorations") ?? "";

                        writer.WriteAttributeString("FontFamily", fontFamily);
                        writer.WriteAttributeString("FontSize", fontSize);
                        if (textAlignment != "Left")
                            writer.WriteAttributeString("TextAlignment", textAlignment);
                        if (fontWeight != "Normal")
                            writer.WriteAttributeString("FontWeight", fontWeight);
                        if (fontStyle != "Normal")
                            writer.WriteAttributeString("FontStyle", fontStyle);
                        if (!string.IsNullOrEmpty(textDecorations))
                            writer.WriteAttributeString("TextDecorations", textDecorations);

                        foreach (XmlNode child in element.ChildNodes)
                        {
                            WriteNodeRecursive(writer, child);
                        }
                        writer.WriteEndElement();
                        break;
                    case "b":
                    case "strong":
                        writer.WriteStartElement("Bold");
                        foreach (XmlNode child in element.ChildNodes)
                        {
                            WriteNodeRecursive(writer, child);
                        }
                        writer.WriteEndElement();
                        break;
                    case "i":
                    case "em":
                        writer.WriteStartElement("Italic");
                        foreach (XmlNode child in element.ChildNodes)
                        {
                            WriteNodeRecursive(writer, child);
                        }
                        writer.WriteEndElement();
                        break;
                    case "u":
                        writer.WriteStartElement("Underline");
                        foreach (XmlNode child in element.ChildNodes)
                        {
                            WriteNodeRecursive(writer, child);
                        }
                        writer.WriteEndElement();
                        break;
                    case "br":
                        writer.WriteStartElement("LineBreak");
                        writer.WriteEndElement();
                        break;
                    case "ul":
                    case "ol":
                        writer.WriteStartElement("List");
                        string markerStyle = element.Name.ToLower() == "ol" ? "Decimal" : "Disc";
                        writer.WriteAttributeString("MarkerStyle", markerStyle);
                        foreach (XmlNode child in element.ChildNodes)
                        {
                            if (child.Name.ToLower() == "li")
                            {
                                writer.WriteStartElement("ListItem");
                                writer.WriteStartElement("Paragraph");
                                foreach (XmlNode liChild in child.ChildNodes)
                                {
                                    WriteNodeRecursive(writer, liChild);
                                }
                                writer.WriteEndElement(); // Paragraph
                                writer.WriteEndElement(); // ListItem
                            }
                        }
                        writer.WriteEndElement(); // List
                        break;
                    default:
                        foreach (XmlNode child in element.ChildNodes)
                        {
                            WriteNodeRecursive(writer, child);
                        }
                        break;
                }
            }
        }
    }
}