using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO.Compression;

var chapterRegex = new Regex("c(\\d+)(b?)\\.");
var scRegex = new Regex("\\*\\*([^*]+)\\*\\*");
var iRegex = new Regex("[*_]([^*_]+)[*_]");
var qRegex = new Regex("<p>[*_]([^*_]+)[*_]\\s*</p>");

IEnumerable<(string part, string chapter, bool isSpecial, string contents)> Enumerate()
{
    foreach (var dir in Directory.GetDirectories(".", "Partie*"))
    {
        var part = Path.GetFileName(dir);

        foreach (var md in Directory.GetFiles(dir))
        {
            var match = chapterRegex.Match(md);
            if (!match.Success) continue;

            var chapter = match.Groups[1].Value.TrimStart('0');
            var special = match.Groups[2].Value.Length != 0;

            var open = "<p>";
            if (!special && chapter != "1" && int.Parse(chapter) < 31)
                open = "<p class=\"open\">";

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

            yield return (part, chapter, special, open + contents + "</p>");
        }
    }
}

string H2(string chapter, bool special) 
{
    if (chapter == "1") 
        return null;

    if (special)
    {
        if (chapter == "40")
            return "Épilogue";
        return "❦";
    }
        
    return "Chapitre " + chapter;
}

// ===========================================================================
// HTML
// ===========================================================================

void MakeHtml()
{
    var htmlOutput = new StringBuilder();
    htmlOutput.AppendLine(@"<!DOCTYPE html>
<html>
<head><title>La Sirène et le Monolithe</title><style>
h1, h2, h3 { 
    text-align: center ;
    font-family: Calibri 
}
h2 { page-break-before: always }
h2.part {
    page-break-after:always;
    font-variant: small-caps
}
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
    font-family: Verdana, sans-serif;
}
.quote {
    font-style: italic;
    padding: 0 30px;
    box-sizing: border-box;
}
</style></head>
<body>
<h1>La Sirène et le Monolithe</h1>");

    var prevPart = "";

    foreach (var (part, chapter, special, contents) in Enumerate())
    {
        if (prevPart != part)
        {
            htmlOutput.AppendLine("<h2 class=part>" + part + "</h2>");
            prevPart = part;
        }

        var h2 = H2(chapter, special);
        if (h2 != null)
            htmlOutput.AppendLine($"<h2>{h2}</h2>");

        htmlOutput.AppendLine(contents);
    }

    htmlOutput.AppendLine("</body></html>");
    File.WriteAllText("./sirene-et-monolithe.html", htmlOutput.ToString());
}

MakeHtml();

// ===========================================================================
// EPUB
// ===========================================================================

void MakeEpub()
{
    using var fs = new FileStream("./sirene-et-monolithe.epub", FileMode.Create); 
    using var archive = new ZipArchive(fs, ZipArchiveMode.Create);

    // Manifest (lists all files)
    void WriteOpfToZip()
    {
        var opf = archive.CreateEntry("OEBPS/content.opf");
        using var stream = opf.Open();
        using var w = new StreamWriter(stream);

        w.WriteLine(@"<?xml version='1.0' encoding='utf-8'?>
<package xmlns=""http://www.idpf.org/2007/opf""
            xmlns:dc=""http://purl.org/dc/elements/1.1/""
            unique-identifier=""bookid"" version=""2.0"">
  <metadata>
    <dc:title>La Sirène et le Monolithe</dc:title>
    <dc:creator>Victor Nicollet</dc:creator>
    <dc:identifier id=""bookid"">urn:https://nicollet.net/books/sirene-et-monolithe</dc:identifier>
    <dc:language>fr-FR</dc:language>
  </metadata>
  <manifest>
    <item id=""ncx"" href=""toc.ncx"" media-type=""application/x-dtbncx+xml""/>
    <item id=""cover"" href=""title.htm"" media-type=""application/xhtml+xml""/>");

        foreach (var (_, name, special, _) in Enumerate())
        {
            var id = name + (special ? "b" : "");
            w.WriteLine($"<item id=\"{id}\" href=\"{id}.htm\" media-type=\"application/xhtml+xml\"/>");
        }

        w.WriteLine(@"    <item id=""css"" href=""main.css"" media-type=""text/css""/>
  </manifest>
  <spine toc=""ncx"">
    <itemref idref=""cover"" linear=""no""/>");

        foreach (var (_, name, special, _) in Enumerate())
        {
            var id = name + (special ? "b" : "");
            w.WriteLine($"<itemref idref=\"{id}\"/>");
        }

        w.WriteLine(@"  </spine>
  <guide>
    <reference href=""title.htm"" type=""cover"" title=""La Sirène et Le Monolithe""/>
  </guide>
</package>");
    }

    // Table of contents
    void WriteNcxToZip() 
    {
        var opf = archive.CreateEntry("OEBPS/toc.ncx");
        using var stream = opf.Open();
        using var w = new StreamWriter(stream);

        w.WriteLine(@"<?xml version='1.0' encoding='utf-8'?>
<!DOCTYPE ncx PUBLIC ""-//NISO//DTD ncx 2005-1//EN""
                 ""http://www.daisy.org/z3986/2005/ncx-2005-1.dtd"">
<ncx xmlns=""http://www.daisy.org/z3986/2005/ncx/"" version=""2005-1"">
  <head>
    <meta name=""dtb:uid""
content=""urn:https://nicollet.net/books/sirene-et-monolithe""/>
    <meta name=""dtb:depth"" content=""1""/>
    <meta name=""dtb:totalPageCount"" content=""0""/>
    <meta name=""dtb:maxPageNumber"" content=""0""/>
  </head>
  <docTitle>
    <text>La Sirène et le Monolithe</text>
  </docTitle>
  <navMap>
    <navPoint id=""title"" playOrder=""0"">
      <navLabel>
        <text>Couverture</text>
      </navLabel>
      <content src=""title.htm""/>
    </navPoint>");

        var order = 1;
        
        foreach (var (_, name, special, _) in Enumerate())
        {
            var playOrder = order++;
            var id = name + (special ? "b" : "");
            var title = special ? (name == "40" ? "Épilogue" : "Interlude") : "Chapitre " + name;
            w.WriteLine($@"    <navPoint id=""chapter-{id}"" playOrder=""{playOrder}"">
      <navLabel>
        <text>{title}</text>
      </navLabel>
      <content src=""{id}.htm""/>
    </navPoint>");
        }

        w.WriteLine(@"  </navMap>
</ncx>");
    }

    void WriteChapterToZip(string name, bool special, string content)
    {
        var id = name + (special ? "b" : "");

        var opf = archive.CreateEntry($"OEBPS/{id}.htm");
        using var stream = opf.Open();
        using var w = new StreamWriter(stream);

        var h2 = H2(name, special);

        w.WriteLine($@"<?xml version=""1.0"" encoding=""UTF-8"" ?>
<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.1//EN"" ""http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd"">
<html xmlns=""http://www.w3.org/1999/xhtml"" xml:lang=""en"">
  <head>
    <meta http-equiv=""Content-Type"" content=""application/xhtml+xml; charset=utf-8"" />
    <title>{h2 ?? ""}</title>
    <link rel=""stylesheet"" href=""main.css"" type=""text/css"" />
  </head>
  <body>");

        if (h2 != null) w.WriteLine($"<h2>{h2}</h2>");

        w.WriteLine(content);
        w.WriteLine("</body></html>");
    }

    void WriteToZip(string path, string body) 
    {
        var opf = archive.CreateEntry(path);
        using var stream = opf.Open();
        using var w = new StreamWriter(stream);
        w.Write(body);
    }

    WriteOpfToZip();
    WriteNcxToZip();
    
    foreach (var (_, name, special, content) in Enumerate())
        WriteChapterToZip(name, special, content);

    WriteToZip("META-INF/container.xml", @"<?xml version=""1.0""?>
<container version=""1.0"" xmlns=""urn:oasis:names:tc:opendocument:xmlns:container"">
  <rootfiles>
    <rootfile full-path=""OEBPS/content.opf""
     media-type=""application/oebps-package+xml"" />
  </rootfiles>
</container>");

    WriteToZip("mimetype", "application/epub+zip");

    WriteToZip("OEBPS/main.css", @"
* { font-family: sans-serif }
.sc { font-variant: small-caps }
h2, h3 { text-align: center }
.asterism { 
    font-size: 150%;
    text-align: center;
}
.open { 
    background: rgba(128,128,128,0.2); 
    padding: 8px;
}
.quote {
    font-style: italic;
}");

    WriteToZip("OEBPS/title.htm", $@"<?xml version=""1.0"" encoding=""UTF-8"" ?>
<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.1//EN"" ""http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd"">
<html xmlns=""http://www.w3.org/1999/xhtml"" xml:lang=""en"">
  <head>
    <meta http-equiv=""Content-Type"" content=""application/xhtml+xml; charset=utf-8"" />
    <title>La Sirène et le Monolithe</title>
    <link rel=""stylesheet"" href=""main.css"" type=""text/css"" />
  </head>
  <body style=""text-align:center"">
    <h1>La Sirène et le Monolithe</h1>
    <h2>Victor Nicollet</h2>
    <p><small>Version {DateTime.UtcNow:yyyy-MM-dd}</small></p>
  </body>
</html>");

}

MakeEpub();
