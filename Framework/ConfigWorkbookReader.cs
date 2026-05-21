using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace RoboticEnterpriseFrameworkcoded
{
    internal static class ConfigWorkbookReader
    {
        private static readonly XNamespace SpreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static readonly XNamespace RelationshipNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private static readonly XNamespace PackageRelationshipNs = "http://schemas.openxmlformats.org/package/2006/relationships";

        public static DataTable ReadSheet(string workbookPath, string sheetName)
        {
            if (string.IsNullOrWhiteSpace(workbookPath))
            {
                throw new ArgumentException("Workbook path is required.", nameof(workbookPath));
            }

            if (!File.Exists(workbookPath))
            {
                throw new FileNotFoundException("Configuration workbook was not found.", workbookPath);
            }

            using (var archive = ZipFile.OpenRead(workbookPath))
            {
                var sharedStrings = ReadSharedStrings(archive);
                var worksheetPath = ResolveWorksheetPath(archive, sheetName);
                var worksheetEntry = archive.GetEntry(worksheetPath);
                if (worksheetEntry == null)
                {
                    throw new InvalidOperationException("Worksheet file was not found for sheet '" + sheetName + "'.");
                }

                using (var stream = worksheetEntry.Open())
                {
                    var sheet = XDocument.Load(stream);
                    var rows = sheet.Descendants(SpreadsheetNs + "row")
                        .Select(row => ReadRow(row, sharedStrings))
                        .Where(row => row.Count > 0)
                        .ToList();

                    return ToDataTable(rows, sheetName);
                }
            }
        }

        private static List<string> ReadSharedStrings(ZipArchive archive)
        {
            var entry = archive.GetEntry("xl/sharedStrings.xml");
            if (entry == null)
            {
                return new List<string>();
            }

            using (var stream = entry.Open())
            {
                var document = XDocument.Load(stream);
                return document.Descendants(SpreadsheetNs + "si")
                    .Select(item => string.Concat(item.Descendants(SpreadsheetNs + "t").Select(text => text.Value)))
                    .ToList();
            }
        }

        private static string ResolveWorksheetPath(ZipArchive archive, string sheetName)
        {
            var workbookEntry = archive.GetEntry("xl/workbook.xml");
            var relationshipsEntry = archive.GetEntry("xl/_rels/workbook.xml.rels");
            if (workbookEntry == null || relationshipsEntry == null)
            {
                throw new InvalidOperationException("The workbook metadata is incomplete.");
            }

            XDocument workbook;
            XDocument relationships;
            using (var stream = workbookEntry.Open())
            {
                workbook = XDocument.Load(stream);
            }

            using (var stream = relationshipsEntry.Open())
            {
                relationships = XDocument.Load(stream);
            }

            var sheet = workbook.Descendants(SpreadsheetNs + "sheet")
                .FirstOrDefault(candidate => string.Equals((string)candidate.Attribute("name"), sheetName, StringComparison.OrdinalIgnoreCase));
            if (sheet == null)
            {
                throw new InvalidOperationException("Sheet '" + sheetName + "' was not found in the configuration workbook.");
            }

            var relationshipId = (string)sheet.Attribute(RelationshipNs + "id");
            var target = relationships.Descendants(PackageRelationshipNs + "Relationship")
                .Where(relationship => string.Equals((string)relationship.Attribute("Id"), relationshipId, StringComparison.Ordinal))
                .Select(relationship => (string)relationship.Attribute("Target"))
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(target))
            {
                throw new InvalidOperationException("Sheet relationship was not found for '" + sheetName + "'.");
            }

            return "xl/" + target.TrimStart('/').Replace("\\", "/");
        }

        private static SortedDictionary<int, string> ReadRow(XElement row, IList<string> sharedStrings)
        {
            var values = new SortedDictionary<int, string>();
            foreach (var cell in row.Elements(SpreadsheetNs + "c"))
            {
                var reference = (string)cell.Attribute("r");
                var columnIndex = GetColumnIndex(reference);
                values[columnIndex] = ReadCellValue(cell, sharedStrings);
            }

            return values;
        }

        private static string ReadCellValue(XElement cell, IList<string> sharedStrings)
        {
            var type = (string)cell.Attribute("t");
            if (type == "inlineStr")
            {
                return string.Concat(cell.Descendants(SpreadsheetNs + "t").Select(text => text.Value));
            }

            var rawValue = cell.Element(SpreadsheetNs + "v")?.Value ?? string.Empty;
            if (type == "s" && int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sharedIndex))
            {
                return sharedIndex >= 0 && sharedIndex < sharedStrings.Count ? sharedStrings[sharedIndex] : string.Empty;
            }

            if (type == "b")
            {
                return rawValue == "1" ? "True" : "False";
            }

            return rawValue;
        }

        private static DataTable ToDataTable(IList<SortedDictionary<int, string>> rows, string tableName)
        {
            var table = new DataTable(tableName);
            if (rows.Count == 0)
            {
                return table;
            }

            var headers = rows[0];
            var maxColumn = rows.Max(row => row.Keys.DefaultIfEmpty(0).Max());
            for (var column = 1; column <= maxColumn; column++)
            {
                var header = headers.ContainsKey(column) ? headers[column].Trim() : string.Empty;
                if (string.IsNullOrWhiteSpace(header))
                {
                    header = "Column" + column.ToString(CultureInfo.InvariantCulture);
                }

                var uniqueHeader = header;
                var suffix = 1;
                while (table.Columns.Contains(uniqueHeader))
                {
                    uniqueHeader = header + "_" + suffix.ToString(CultureInfo.InvariantCulture);
                    suffix++;
                }

                table.Columns.Add(uniqueHeader, typeof(string));
            }

            foreach (var rowValues in rows.Skip(1))
            {
                var row = table.NewRow();
                for (var column = 1; column <= table.Columns.Count; column++)
                {
                    row[column - 1] = rowValues.ContainsKey(column) ? rowValues[column] : string.Empty;
                }

                table.Rows.Add(row);
            }

            return table;
        }

        private static int GetColumnIndex(string cellReference)
        {
            if (string.IsNullOrWhiteSpace(cellReference))
            {
                return 1;
            }

            var result = 0;
            foreach (var character in cellReference.TakeWhile(char.IsLetter))
            {
                result *= 26;
                result += char.ToUpperInvariant(character) - 'A' + 1;
            }

            return result == 0 ? 1 : result;
        }
    }
}
