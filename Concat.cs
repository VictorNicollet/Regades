using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

var chapterRegex = new Regex("c(\\d+)(b?)\\.");
var scRegex = new Regex("\\*\\*([^*]+)\\*\\*");
var iRegex = new Regex("[*_]([^*_]+)[*_]");
var qRegex = new Regex("<p>[*_]([^*_]+)[*_]\\s*</p>");

var output = new StringBuilder();
output.AppendLine(@"<!DOCTYPE html>
<html>
<head><title>La Sirène et le Monolithe</title><style>
h1, h2 { text-align: center }
h2 { page-break-before: always }
.sc { font-variant: small-caps }
.asterism { 
    font-size: 150%;
    text-align: center;
}
.open { 
    background: #EEE; 
    padding: 8px
}
p { 
    max-width: 800px; 
    margin: 1em auto;
    line-height: 1.5em;
    font-family: sans-serif
}
.quote {
    font-style: italic;
    padding: 0 30px;
    box-sizing: border-box;
}
</style></head>
<body>
<h1>La Sirène et le Monolithe</h1>");

foreach (var dir in Directory.GetDirectories(".", "Partie*"))
{
    foreach (var md in Directory.GetFiles(dir))
    {
        var match = chapterRegex.Match(md);
        if (!match.Success) continue;

        var chapter = match.Groups[1].Value.TrimStart('0');
        var special = match.Groups[2].Value.Length != 0;

        if (chapter == "1")
        {
            output.AppendLine("<p class=\"open\">");
        }
        else if (!special)
        {
            output.AppendLine($"<h2>{chapter}</h2>");
            output.AppendLine("<p class=\"open\">");
        }
        else if (chapter == "40")
        {
            output.AppendLine($"<h2>Épilogue</h2>");
            output.AppendLine("<p>");
        }
        else
        {
            output.AppendLine("<h2>❦</h2>");
            output.AppendLine("<p>");
        }

        var contents = File.ReadAllText(md);
        contents = contents.Replace("***", "</p><p class=asterism>⁂</p><p>")
            .Replace("<<", "&laquo;")
            .Replace("&laquo; ", "&laquo;&nbsp;")
            .Replace(">>", "&raquo;")
            .Replace(" &raquo;", "&nbsp;&raquo;")
            .Replace("\n---", "<br>&mdash;")
            .Replace("---", "&mdash;")
            .Replace("&mdash; ", "&mdash;&nbsp;")
            .Replace(" :", "&nbsp;:")
            .Replace(" ;", "&nbsp;;")
            .Replace(" !", "&nbsp;!")
            .Replace(" ?", "&nbsp;?")
            .Replace("...", "…")
            .Replace("\n\n", "</p><p>")
            .Replace("\r\n\r\n", "</p><p>");

        contents = scRegex.Replace(contents, (Match m) => 
            "<span class=\"sc\">" + m.Groups[1] + "</span>");

        contents = qRegex.Replace(contents, (Match m) => 
            "<p class=quote>" + m.Groups[1] + "</p>");

        contents = iRegex.Replace(contents, (Match m) => 
            "<em>" + m.Groups[1] + "</em>");

        output.AppendLine(contents);
        output.AppendLine("</p>");
    }
}   

output.AppendLine("</body></html>");
File.WriteAllText("./sirene-et-monolithe.html", output.ToString());