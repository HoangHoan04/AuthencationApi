using ClosedXML.Excel;
using System.Globalization;

namespace AuthApi.Application.Common.Helpers;

public static class ExcelHelper
{
    public static readonly XLColor HeaderBackgroundColor = XLColor.FromHtml("#3b82f6");

    public static void WriteStyledHeaderCell(IXLWorksheet worksheet, int col, string title, bool required)
    {
        IXLCell cell = worksheet.Cell(1, col);
        cell.Style.Fill.BackgroundColor = HeaderBackgroundColor;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        cell.Style.Alignment.WrapText = false;
        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        cell.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        if (required)
        {
            IXLRichText richText = cell.GetRichText();
            _ = richText.ClearText();
            IXLRichString textPart = richText.AddText(title);
            textPart.FontColor = XLColor.White;
            textPart.Bold = true;

            IXLRichString starPart = richText.AddText(" *");
            starPart.FontColor = XLColor.FromHtml("#fef08a");
            starPart.Bold = true;
        }
        else
        {
            cell.Value = title;
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
        }
    }

    public static void WriteReferenceSheet(
        IXLWorkbook workbook,
        string sheetName,
        string codeHeader,
        string nameHeader,
        IEnumerable<(string Code, string Name)> items)
    {
        IXLWorksheet ws = workbook.Worksheets.Add(sheetName);
        WriteStyledHeaderCell(ws, 1, codeHeader, false);
        WriteStyledHeaderCell(ws, 2, nameHeader, false);
        ws.Row(1).Height = 28;

        int row = 2;
        foreach (var item in items)
        {
            IXLCell c1 = ws.Cell(row, 1);
            c1.Value = item.Code ?? string.Empty;
            c1.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            c1.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            IXLCell c2 = ws.Cell(row, 2);
            c2.Value = item.Name ?? string.Empty;
            c2.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            c2.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            row++;
        }

        ApplyColumnWidths(ws);
        FreezeHeaderRow(ws);
    }

    public static void ApplyColumnWidths(IXLWorksheet worksheet)
    {
        IXLColumns usedColumns = worksheet.ColumnsUsed();
        foreach (IXLColumn? column in usedColumns)
        {
            int maxTextLength = 0;
            foreach (IXLCell? cell in column.CellsUsed())
            {
                string text = string.Empty;
                if (cell.HasRichText)
                {
                    text = cell.GetRichText().Text;
                }
                else
                {
                    try
                    {
                        text = cell.GetFormattedString();
                    }
                    catch
                    {
                    }

                    if (string.IsNullOrEmpty(text))
                    {
                        text = cell.Value.ToString() ?? string.Empty;
                    }
                }

                if (!string.IsNullOrEmpty(text))
                {
                    string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                    foreach (string line in lines)
                    {
                        if (line.Length > maxTextLength)
                        {
                            maxTextLength = line.Length;
                        }
                    }
                }
            }

            int finalWidth = Math.Clamp(maxTextLength + 4, 12, 50);
            column.Width = finalWidth;
        }
    }

    public static void FreezeHeaderRow(IXLWorksheet worksheet, int rowsToFreeze = 1)
    {
        worksheet.SheetView.FreezeRows(rowsToFreeze);
    }

    public static string? GetString(IXLRow row, int col)
    {
        var cell = row.Cell(col);
        if (cell.IsEmpty()) return null;
        var text = cell.GetString()?.Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    public static int? GetInt(IXLRow row, int col)
    {
        var cell = row.Cell(col);
        if (cell.IsEmpty()) return null;
        if (cell.TryGetValue(out int intVal)) return intVal;
        var str = cell.GetString()?.Trim();
        if (int.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out int parsed)) return parsed;
        return null;
    }

    public static bool GetBool(IXLRow row, int col, bool defaultValue = true)
    {
        var cell = row.Cell(col);
        if (cell.IsEmpty()) return defaultValue;
        if (cell.TryGetValue(out bool bVal)) return bVal;
        var str = cell.GetString()?.Trim().ToLower();
        if (string.IsNullOrEmpty(str)) return defaultValue;
        if (str == "1" || str == "true" || str == "hoạt động" || str == "active" || str == "có" || str == "yes") return true;
        if (str == "0" || str == "false" || str == "tạm dừng" || str == "inactive" || str == "không" || str == "no") return false;
        return defaultValue;
    }
}
