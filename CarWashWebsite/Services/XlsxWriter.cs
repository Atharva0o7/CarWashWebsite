using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;

namespace CarWashWebsite.Services;

/// <summary>
/// Minimal SpreadsheetML (.xlsx) writer — an xlsx is a ZIP of XML parts, and
/// <see cref="ZipArchive"/> is in the BCL, so this needs no third-party package.
///
/// Supports what an export actually needs: multiple sheets, a bold frozen header row,
/// text / number / currency / date cells, and column widths. Values are written as
/// inline strings, so there is no shared-string table to keep in sync.
/// </summary>
public static class XlsxWriter
{
    public enum CellKind { Text, Number, Currency, Date }

    public readonly record struct Cell(string? Value, CellKind Kind = CellKind.Text)
    {
        public static Cell Text(string? v) => new(v);
        public static Cell Number(long v) => new(v.ToString(CultureInfo.InvariantCulture), CellKind.Number);
        public static Cell Money(long v) => new(v.ToString(CultureInfo.InvariantCulture), CellKind.Currency);
        public static Cell Date(DateOnly v) => new(
            // Excel serial date: days since 1899-12-30.
            (v.ToDateTime(TimeOnly.MinValue) - new DateTime(1899, 12, 30)).Days
                .ToString(CultureInfo.InvariantCulture),
            CellKind.Date);
    }

    public sealed class Sheet(string name)
    {
        /// <summary>Excel forbids : \ / ? * [ ] in sheet names and caps them at 31 chars.</summary>
        public string Name { get; } = Sanitise(name);
        public List<string> Headers { get; } = [];
        public List<Cell[]> Rows { get; } = [];
        public List<double> ColumnWidths { get; } = [];

        public Sheet WithHeaders(params string[] headers)
        {
            Headers.Clear();
            Headers.AddRange(headers);
            return this;
        }

        public Sheet WithWidths(params double[] widths)
        {
            ColumnWidths.Clear();
            ColumnWidths.AddRange(widths);
            return this;
        }

        public Sheet AddRow(params Cell[] cells)
        {
            Rows.Add(cells);
            return this;
        }

        private static string Sanitise(string name)
        {
            var cleaned = new string(name.Where(c => !":\\/?*[]".Contains(c)).ToArray());
            return cleaned.Length <= 31 ? cleaned : cleaned[..31];
        }
    }

    /// <summary>Style indices matching the &lt;cellXfs&gt; order written in styles.xml.</summary>
    private const int StyleDefault = 0;
    private const int StyleHeader = 1;
    private const int StyleCurrency = 2;
    private const int StyleDate = 3;

    public static byte[] Build(params Sheet[] sheets)
    {
        if (sheets.Length == 0)
        {
            throw new ArgumentException("At least one sheet is required.", nameof(sheets));
        }

        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            Write(zip, "[Content_Types].xml", ContentTypes(sheets.Length));
            Write(zip, "_rels/.rels", RootRels());
            Write(zip, "xl/workbook.xml", Workbook(sheets));
            Write(zip, "xl/_rels/workbook.xml.rels", WorkbookRels(sheets.Length));
            Write(zip, "xl/styles.xml", Styles());

            for (var i = 0; i < sheets.Length; i++)
            {
                Write(zip, $"xl/worksheets/sheet{i + 1}.xml", WorksheetXml(sheets[i]));
            }
        }

        return buffer.ToArray();
    }

    private static void Write(ZipArchive zip, string path, string xml)
    {
        var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        // No BOM — Excel rejects a BOM inside the package parts.
        var bytes = new UTF8Encoding(false).GetBytes(xml);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static string ContentTypes(int sheetCount)
    {
        var sb = new StringBuilder();
        sb.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        sb.Append("""<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">""");
        sb.Append("""<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>""");
        sb.Append("""<Default Extension="xml" ContentType="application/xml"/>""");
        sb.Append("""<Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>""");
        sb.Append("""<Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>""");

        for (var i = 1; i <= sheetCount; i++)
        {
            sb.Append($"""<Override PartName="/xl/worksheets/sheet{i}.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>""");
        }

        sb.Append("</Types>");
        return sb.ToString();
    }

    private static string RootRels() =>
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>"""
        + """<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">"""
        + """<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>"""
        + "</Relationships>";

    private static string Workbook(Sheet[] sheets)
    {
        var sb = new StringBuilder();
        sb.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        sb.Append("""<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">""");
        sb.Append("<sheets>");

        for (var i = 0; i < sheets.Length; i++)
        {
            sb.Append($"""<sheet name="{Escape(sheets[i].Name)}" sheetId="{i + 1}" r:id="rId{i + 1}"/>""");
        }

        sb.Append("</sheets></workbook>");
        return sb.ToString();
    }

    private static string WorkbookRels(int sheetCount)
    {
        var sb = new StringBuilder();
        sb.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        sb.Append("""<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">""");

        for (var i = 1; i <= sheetCount; i++)
        {
            sb.Append($"""<Relationship Id="rId{i}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet{i}.xml"/>""");
        }

        // Styles must come after the sheet relationships so the ids do not collide.
        sb.Append($"""<Relationship Id="rId{sheetCount + 1}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>""");
        sb.Append("</Relationships>");
        return sb.ToString();
    }

    private static string Styles() =>
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>"""
        + """<styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">"""
        // 164 = rupee currency, 165 = day-month-year.
        + """<numFmts count="2">"""
        + """<numFmt numFmtId="164" formatCode="&quot;₹&quot;#,##0"/>"""
        + """<numFmt numFmtId="165" formatCode="dd\-mmm\-yyyy"/>"""
        + "</numFmts>"
        + """<fonts count="2">"""
        + """<font><sz val="11"/><name val="Calibri"/></font>"""
        + """<font><b/><sz val="11"/><color rgb="FFFFFFFF"/><name val="Calibri"/></font>"""
        + "</fonts>"
        + """<fills count="3">"""
        + """<fill><patternFill patternType="none"/></fill>"""
        + """<fill><patternFill patternType="gray125"/></fill>"""
        + """<fill><patternFill patternType="solid"><fgColor rgb="FF810034"/><bgColor indexed="64"/></patternFill></fill>"""
        + "</fills>"
        + """<borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders>"""
        + """<cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>"""
        + """<cellXfs count="4">"""
        + """<xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>"""
        + """<xf numFmtId="0" fontId="1" fillId="2" borderId="0" xfId="0" applyFont="1" applyFill="1"/>"""
        + """<xf numFmtId="164" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/>"""
        + """<xf numFmtId="165" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/>"""
        + "</cellXfs>"
        + "</styleSheet>";

    private static string WorksheetXml(Sheet sheet)
    {
        var sb = new StringBuilder();
        sb.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        sb.Append("""<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">""");

        if (sheet.Headers.Count > 0)
        {
            // Freeze the header so it stays put while scrolling.
            sb.Append("""<sheetViews><sheetView workbookViewId="0">""");
            sb.Append("""<pane ySplit="1" topLeftCell="A2" activePane="bottomLeft" state="frozen"/>""");
            sb.Append("</sheetView></sheetViews>");
        }

        if (sheet.ColumnWidths.Count > 0)
        {
            sb.Append("<cols>");
            for (var i = 0; i < sheet.ColumnWidths.Count; i++)
            {
                var w = sheet.ColumnWidths[i].ToString(CultureInfo.InvariantCulture);
                sb.Append($"""<col min="{i + 1}" max="{i + 1}" width="{w}" customWidth="1"/>""");
            }
            sb.Append("</cols>");
        }

        sb.Append("<sheetData>");

        var rowIndex = 1;
        if (sheet.Headers.Count > 0)
        {
            sb.Append($"""<row r="{rowIndex}">""");
            for (var c = 0; c < sheet.Headers.Count; c++)
            {
                AppendCell(sb, Column(c), rowIndex, Cell.Text(sheet.Headers[c]), StyleHeader);
            }
            sb.Append("</row>");
            rowIndex++;
        }

        foreach (var row in sheet.Rows)
        {
            sb.Append($"""<row r="{rowIndex}">""");
            for (var c = 0; c < row.Length; c++)
            {
                var style = row[c].Kind switch
                {
                    CellKind.Currency => StyleCurrency,
                    CellKind.Date => StyleDate,
                    _ => StyleDefault,
                };
                AppendCell(sb, Column(c), rowIndex, row[c], style);
            }
            sb.Append("</row>");
            rowIndex++;
        }

        sb.Append("</sheetData>");

        if (sheet.Headers.Count > 0 && sheet.Rows.Count > 0)
        {
            var last = $"{Column(sheet.Headers.Count - 1)}{sheet.Rows.Count + 1}";
            sb.Append($"""<autoFilter ref="A1:{last}"/>""");
        }

        sb.Append("</worksheet>");
        return sb.ToString();
    }

    private static void AppendCell(StringBuilder sb, string col, int row, Cell cell, int style)
    {
        var reference = $"{col}{row}";
        var styleAttr = style == StyleDefault ? "" : $" s=\"{style}\"";

        if (string.IsNullOrEmpty(cell.Value))
        {
            sb.Append($"""<c r="{reference}"{styleAttr}/>""");
            return;
        }

        if (cell.Kind is CellKind.Text)
        {
            sb.Append($"""<c r="{reference}"{styleAttr} t="inlineStr"><is><t xml:space="preserve">{Escape(cell.Value)}</t></is></c>""");
        }
        else
        {
            sb.Append($"""<c r="{reference}"{styleAttr}><v>{cell.Value}</v></c>""");
        }
    }

    /// <summary>0 → A, 25 → Z, 26 → AA.</summary>
    private static string Column(int index)
    {
        var name = "";
        for (var i = index; i >= 0; i = i / 26 - 1)
        {
            name = (char)('A' + i % 26) + name;
        }
        return name;
    }

    private static string Escape(string value)
    {
        // Strip control characters Excel refuses to load, then XML-escape.
        var cleaned = new string(value.Where(c => c >= 0x20 || c is '\t' or '\n' or '\r').ToArray());
        return SecurityElement.Escape(cleaned) ?? "";
    }
}
