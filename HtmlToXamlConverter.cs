// ������� ���������� HtmlToXamlConverter ��� �������������� �������� HTML � FlowDocument-compatible XAML
// ��������������: <p>, <b>, <i>, <u>, <br>, <span style="color:"> � <body>

using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace HTMLConverter
{
    public static class HtmlToXamlConverter
    {
        public static string ConvertHtmlToXaml(string htmlString, bool asFlowDocument)
        {
            if (string.IsNullOrWhiteSpace(htmlString))
                return "<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph /></FlowDocument>";

            // ���������� HTML
            htmlString = HttpUtility.HtmlDecode(htmlString);

            // �������� �� <body>
            Match bodyMatch = Regex.Match(htmlString, "<body[^>]*>(.*?)</body>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            string bodyContent = bodyMatch.Success ? bodyMatch.Groups[1].Value : htmlString;

            // ����������� <p>...</p> � XAML-���������
            var paragraphs = Regex.Split(bodyContent, "</p>", RegexOptions.IgnoreCase);
            var xamlBuilder = new StringBuilder();

            string containerOpen = asFlowDocument
                ? "<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">"
                : "<Section xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">";
            xamlBuilder.Append(containerOpen);

            foreach (var para in paragraphs)
            {
                string trimmed = para.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                // ������� <p> ����������� ���
                trimmed = Regex.Replace(trimmed, "<p[^>]*>", "", RegexOptions.IgnoreCase);

                string xamlParagraph = ConvertInlineHtmlToXaml(trimmed);
                xamlBuilder.Append($"<Paragraph>{xamlParagraph}</Paragraph>");
            }

            string containerClose = asFlowDocument ? "</FlowDocument>" : "</Section>";
            xamlBuilder.Append(containerClose);

            return xamlBuilder.ToString();
        }

        private static string ConvertInlineHtmlToXaml(string html)
        {
            // ������
            html = Regex.Replace(html, "<b[^>]*>", "<Bold>", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, "</b>", "</Bold>", RegexOptions.IgnoreCase);

            // ������
            html = Regex.Replace(html, "<i[^>]*>", "<Italic>", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, "</i>", "</Italic>", RegexOptions.IgnoreCase);

            // �������������
            html = Regex.Replace(html, "<u[^>]*>", "<Underline>", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, "</u>", "</Underline>", RegexOptions.IgnoreCase);

            // ����
            html = Regex.Replace(html, "<span style=\"color:([#a-zA-Z0-9]+)\">", "<Span Foreground=\"$1\">", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, "</span>", "</Span>", RegexOptions.IgnoreCase);

            // <br> ��� LineBreak
            html = Regex.Replace(html, "<br ?/?>", "<LineBreak />", RegexOptions.IgnoreCase);

            // ������� ����������� HTML-����
            html = Regex.Replace(html, "<[^>]+>", "", RegexOptions.IgnoreCase);

            // ���������� ���������� �����
            return SecurityElement.Escape(html);
        }
    }

}