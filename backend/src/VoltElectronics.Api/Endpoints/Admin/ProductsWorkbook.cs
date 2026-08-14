using System.Text.RegularExpressions;
using ClosedXML.Excel;
using VoltElectronics.Application.Admin;
using VoltElectronics.Application.Catalog;

namespace VoltElectronics.Api.Endpoints.Admin;

/// <summary>
/// The Excel side of product import/export. The export sheet doubles as the import template:
/// columns are matched by header name (case-insensitive), so order doesn't matter, and the Id
/// column is informational only — imports match rows to products by SKU.
/// </summary>
internal static class ProductsWorkbook
{
    private const string SheetName = "Products";

    /// <summary>
    /// Translated-text columns emitted by export and the template; Parse accepts any
    /// "Name (xx)" / "Description (xx)" pair of headers.
    /// </summary>
    private static readonly string[] TranslationLangs = ["hy", "ru"];

    private static readonly Regex NameHeader =
        new(@"^Name \((?<lang>[a-z]{2}(-[a-z]{2,4})?)\)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DescriptionHeader =
        new(@"^Description \((?<lang>[a-z]{2}(-[a-z]{2,4})?)\)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static string[] AllHeaders() =>
        ["Id", "Name", .. TranslationLangs.Select(l => $"Name ({l})"), "SKU", "Category", "Description",
         .. TranslationLangs.Select(l => $"Description ({l})"),
         "Price", "Compare-at price", "Stock", "Status", "Badge", "Rating", "Reviews", "Specs"];

    public static byte[] Build(IReadOnlyList<ProductExportRowDto> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(SheetName);

        var headers = AllHeaders();
        for (var c = 0; c < headers.Length; c++)
            sheet.Cell(1, c + 1).Value = headers[c];
        sheet.Row(1).Style.Font.SetBold();
        sheet.SheetView.FreezeRows(1);

        int Col(string header) => Array.IndexOf(headers, header) + 1;

        for (var r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            var cells = sheet.Row(r + 2);
            cells.Cell(Col("Id")).Value = row.Id;
            cells.Cell(Col("Name")).Value = row.Name;
            foreach (var lang in TranslationLangs)
                cells.Cell(Col($"Name ({lang})")).Value =
                    row.Translations.FirstOrDefault(t => t.Lang == lang)?.Name ?? "";
            cells.Cell(Col("SKU")).Value = row.Sku;
            cells.Cell(Col("Category")).Value = row.Category;
            cells.Cell(Col("Description")).Value = row.Description;
            foreach (var lang in TranslationLangs)
                cells.Cell(Col($"Description ({lang})")).Value =
                    row.Translations.FirstOrDefault(t => t.Lang == lang)?.Description ?? "";
            cells.Cell(Col("Price")).Value = row.Price;
            cells.Cell(Col("Compare-at price")).Value = row.CompareAtPrice is { } cap ? cap : Blank.Value;
            cells.Cell(Col("Stock")).Value = row.Stock;
            cells.Cell(Col("Status")).Value = row.Status;
            cells.Cell(Col("Badge")).Value = row.Badge ?? "";
            cells.Cell(Col("Rating")).Value = row.Rating;
            cells.Cell(Col("Reviews")).Value = row.ReviewCount;
            cells.Cell(Col("Specs")).Value = row.Specs;
            cells.Cell(Col("Specs")).Style.Alignment.WrapText = true;
        }

        sheet.Columns().AdjustToContents(minWidth: 8.0, maxWidth: 60.0);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// An empty import sheet for admins to prefill: the same headers Parse matches on (minus the
    /// informational Id column), a Status dropdown, per-column notes, and an Instructions sheet
    /// listing the rules and the store's existing categories.
    /// </summary>
    public static byte[] BuildTemplate(IReadOnlyList<CategoryDto> categories)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(SheetName);

        var templateHeaders = AllHeaders().Where(h => h != "Id").ToArray();
        var notes = new Dictionary<string, string>
        {
            ["Name"] = "Required. The canonical name, used when a translation is missing.",
            ["SKU"] = "Required. Rows are matched to existing products by SKU: a known SKU updates that product, a new SKU creates one.",
            ["Category"] = "Required. Matched by name; an unknown name creates a new category. Existing categories are listed on the Instructions sheet.",
            ["Price"] = "Required. A positive number in the store's base currency.",
            ["Compare-at price"] = "Optional \"was\" price, shown struck through in the store.",
            ["Stock"] = "Whole number; empty counts as 0.",
            ["Status"] = "Active, Draft or Archived. Anything else falls back to Active.",
            ["Rating"] = "Optional, 0 to 5.",
            ["Reviews"] = "Optional review count.",
            ["Specs"] = "One \"Name: Value\" spec per line within this cell (Alt+Enter for a new line).",
        };
        foreach (var lang in TranslationLangs)
        {
            notes[$"Name ({lang})"] = $"Optional display name in \"{lang}\"; shoppers browsing in that language see it instead of Name.";
            notes[$"Description ({lang})"] = $"Optional description in \"{lang}\"; falls back to Description when blank.";
        }

        for (var c = 0; c < templateHeaders.Length; c++)
        {
            var cell = sheet.Cell(1, c + 1);
            cell.Value = templateHeaders[c];
            if (notes.TryGetValue(templateHeaders[c], out var note))
                cell.CreateComment().AddText(note);
        }
        sheet.Row(1).Style.Font.SetBold();
        sheet.SheetView.FreezeRows(1);

        var statusColumn = Array.IndexOf(templateHeaders, "Status") + 1;
        var statusRange = sheet.Range(2, statusColumn, 500, statusColumn);
        statusRange.CreateDataValidation().List("\"Active,Draft,Archived\"", inCellDropdown: true);

        sheet.Column(Array.IndexOf(templateHeaders, "Specs") + 1).Style.Alignment.WrapText = true;
        foreach (var (header, width) in new[] { ("Name", 30), ("Description", 45), ("Specs", 40) })
            sheet.Column(Array.IndexOf(templateHeaders, header) + 1).Width = width;

        var help = workbook.Worksheets.Add("Instructions");
        var lines = new List<string>
        {
            "How the import works",
            "• Fill the Products sheet, one product per row, then upload the file on the admin Products page.",
            "• Rows are matched to existing products by SKU — a known SKU updates that product, a new SKU creates one.",
            "• Name, SKU, Category and Price are required; the other columns are optional.",
            "• \"Name (hy)\" / \"Name (ru)\" and \"Description (hy)\" / \"Description (ru)\" hold translations — leave blank to fall back to the main Name/Description.",
            "• An unknown category name creates that category automatically.",
            "• Specs go in one cell, one \"Name: Value\" pair per line (Alt+Enter inside a cell adds a line).",
            "• Rows with problems are skipped and reported after the import — the rest of the file still goes through.",
            "",
            "Example row",
            "Name: Studio Mic Pro   SKU: VLT-MIC-100   Category: Audio   Price: 199.00   Status: Active   Specs: \"Pattern: Cardioid\" + new line + \"Weight: 550 g\"",
            "",
            "Existing categories",
        };
        lines.AddRange(categories.Select(c => $"• {c.Name}"));

        string[] headings = ["How the import works", "Example row", "Existing categories"];
        for (var r = 0; r < lines.Count; r++)
        {
            var cell = help.Cell(r + 1, 1);
            cell.Value = lines[r];
            if (headings.Contains(lines[r])) cell.Style.Font.SetBold();
        }
        help.Column(1).Width = 110;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Reads the first worksheet into import rows. Cell-level parse problems (a price that isn't
    /// a number) surface as row errors alongside the ones the import command reports.
    /// </summary>
    public static (List<ImportProductRow> Rows, List<ImportRowError> Errors) Parse(Stream stream)
    {
        var rows = new List<ImportProductRow>();
        var errors = new List<ImportRowError>();

        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.First();

        var headerRow = sheet.FirstRowUsed();
        if (headerRow is null) return (rows, errors);

        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerRow.CellsUsed())
            columns.TryAdd(cell.GetString().Trim(), cell.Address.ColumnNumber);

        foreach (var row in sheet.RowsUsed().Where(r => r.RowNumber() > headerRow.RowNumber()))
        {
            var rowNumber = row.RowNumber();
            var rowErrors = new List<string>();

            string? Text(string header) =>
                columns.TryGetValue(header, out var col) && row.Cell(col).GetString().Trim() is { Length: > 0 } s
                    ? s : null;

            // TryGetValue handles both real numeric cells and text cells holding a number.
            decimal? Decimal(string header) => Number<decimal>(header);
            double? Double(string header) => Number<double>(header);
            int? Int(string header) => Number<int>(header);

            T? Number<T>(string header) where T : struct
            {
                if (!columns.TryGetValue(header, out var col)) return null;
                var cell = row.Cell(col);
                if (cell.IsEmpty() || cell.GetString().Trim().Length == 0) return null;
                if (cell.TryGetValue<T>(out var value)) return value;
                rowErrors.Add($"{header} \"{cell.GetString().Trim()}\" isn't a valid number.");
                return null;
            }

            // Null when the file has no translation columns at all ("leave unchanged"); an empty
            // list when the columns exist but the cells are blank ("clear translations").
            List<ProductTranslationDto>? translations = null;
            var perLang = new Dictionary<string, (string? Name, string? Description)>();
            foreach (var (header, col) in columns)
            {
                var nameMatch = NameHeader.Match(header);
                var descMatch = DescriptionHeader.Match(header);
                if (!nameMatch.Success && !descMatch.Success) continue;

                translations ??= [];
                var lang = (nameMatch.Success ? nameMatch : descMatch).Groups["lang"].Value.ToLowerInvariant();
                var value = row.Cell(col).GetString().Trim();
                var entry = perLang.GetValueOrDefault(lang);
                perLang[lang] = nameMatch.Success
                    ? (value.Length > 0 ? value : entry.Name, entry.Description)
                    : (entry.Name, value.Length > 0 ? value : entry.Description);
            }
            if (translations is not null)
                translations.AddRange(perLang
                    .Where(kv => kv.Value.Name is not null || kv.Value.Description is not null)
                    .Select(kv => new ProductTranslationDto(kv.Key, kv.Value.Name, kv.Value.Description)));

            var parsed = new ImportProductRow(
                rowNumber,
                Name: Text("Name"),
                Sku: Text("SKU"),
                Category: Text("Category"),
                Description: Text("Description"),
                Price: Decimal("Price"),
                CompareAtPrice: Decimal("Compare-at price"),
                Stock: Int("Stock"),
                Status: Text("Status"),
                Badge: Text("Badge"),
                Rating: Double("Rating"),
                ReviewCount: Int("Reviews"),
                Specs: Text("Specs"),
                Translations: translations);

            if (rowErrors.Count > 0)
                errors.Add(new ImportRowError(rowNumber, string.Join(" ", rowErrors)));
            else
                rows.Add(parsed);
        }

        return (rows, errors);
    }
}
