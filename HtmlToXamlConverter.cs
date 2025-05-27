// Простая реализация HtmlToXamlConverter для преобразования базового HTML в FlowDocument-compatible XAML
// Поддерживаются: <p>, <b>, <i>, <u>, <br>, <span style="color:"> и <body>

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

            // Декодируем HTML
            htmlString = HttpUtility.HtmlDecode(htmlString);

            // Обрезаем до <body>
            Match bodyMatch = Regex.Match(htmlString, "<body[^>]*>(.*?)</body>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            string bodyContent = bodyMatch.Success ? bodyMatch.Groups[1].Value : htmlString;

            // Преобразуем <p>...</p> в XAML-параграфы
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

                // Удаляем <p> открывающий тег
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
            // Жирный
            html = Regex.Replace(html, "<b[^>]*>", "<Bold>", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, "</b>", "</Bold>", RegexOptions.IgnoreCase);

            // Курсив
            html = Regex.Replace(html, "<i[^>]*>", "<Italic>", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, "</i>", "</Italic>", RegexOptions.IgnoreCase);

            // Подчёркивание
            html = Regex.Replace(html, "<u[^>]*>", "<Underline>", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, "</u>", "</Underline>", RegexOptions.IgnoreCase);

            // Цвет
            html = Regex.Replace(html, "<span style=\"color:([#a-zA-Z0-9]+)\">", "<Span Foreground=\"$1\">", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, "</span>", "</Span>", RegexOptions.IgnoreCase);

            // <br> как LineBreak
            html = Regex.Replace(html, "<br ?/?>", "<LineBreak />", RegexOptions.IgnoreCase);

            // Удаляем неизвестные HTML-теги
            html = Regex.Replace(html, "<[^>]+>", "", RegexOptions.IgnoreCase);

            // Экранируем оставшийся текст
            return SecurityElement.Escape(html);
        }
    }

}