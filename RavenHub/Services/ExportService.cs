using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace RavenHub.Services
{
    public class ExportService : IExportService
    {
        public async Task ExportToCsvAsync(DataView dataView, string filePath)
        {
            await Task.Run(() =>
            {
                using (var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
                {
                    var table = dataView.Table;
                    var headers = table.Columns
                        .Cast<DataColumn>()
                        .Where(c => c.ColumnName != "PositionId" && c.ColumnName != "CreatedAt" && c.ColumnName != "EmployeeId")
                        .Select(c => c.ColumnName);
                    writer.WriteLine(string.Join(";", headers));

                    foreach (DataRowView rowView in dataView)
                    {
                        var row = rowView.Row;
                        var values = new List<string>();
                        foreach (DataColumn col in table.Columns)
                        {
                            if (col.ColumnName != "PositionId" && col.ColumnName != "CreatedAt" && col.ColumnName != "EmployeeId")
                            {
                                var value = row[col].ToString();
                                if (value.Contains(";") || value.Contains("\"") || value.Contains("\n"))
                                {
                                    value = $"\"{value.Replace("\"", "\"\"")}\"";
                                }
                                values.Add(value);
                            }
                        }
                        writer.WriteLine(string.Join(";", values));
                    }
                }
            });
        }

        public async Task ExportToDocxAsync(DataView dataView, string filePath)
        {
            await Task.Run(() =>
            {
                using (var document = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
                {
                    var mainPart = document.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    var body = mainPart.Document.AppendChild(new Body());

                    // Настройки страницы
                    var sectionProps = new SectionProperties(
                        new PageMargin()
                        {
                            Top = 1000,
                            Bottom = 1000,
                            Left = 1000,
                            Right = 1000
                        });
                    body.AppendChild(sectionProps);

                    // Заголовок документа
                    var title = new Paragraph(
                        new ParagraphProperties(
                            new Justification() { Val = JustificationValues.Center },
                            new SpacingBetweenLines() { After = "200" }
                        ),
                        new Run(
                            new RunProperties(
                                new FontSize() { Val = "28" },
                                new Bold()
                            ),
                            new Text("Список сотрудников")
                        )
                    );
                    body.AppendChild(title);

                    // Дата экспорта
                    var date = new Paragraph(
                        new ParagraphProperties(
                            new Justification() { Val = JustificationValues.Right },
                            new SpacingBetweenLines() { After = "200" }
                        ),
                        new Run(
                            new RunProperties(
                                new FontSize() { Val = "14" }
                            ),
                            new Text($"Дата экспорта: {DateTime.Now:dd.MM.yyyy HH:mm}")
                        )
                    );
                    body.AppendChild(date);

                    // Создаем таблицу
                    var table = CreateEmployeesTable(dataView);
                    body.AppendChild(table);
                }
            });
        }

        private Table CreateEmployeesTable(DataView dataView)
        {
            var table = new Table();
            var tableProperties = new TableProperties(
                new TableBorders(
                    new TopBorder() { Val = BorderValues.Single, Size = 4 },
                    new BottomBorder() { Val = BorderValues.Single, Size = 4 },
                    new LeftBorder() { Val = BorderValues.Single, Size = 4 },
                    new RightBorder() { Val = BorderValues.Single, Size = 4 },
                    new InsideHorizontalBorder() { Val = BorderValues.Single, Size = 2 },
                    new InsideVerticalBorder() { Val = BorderValues.Single, Size = 2 }
                ),
                new TableWidth() { Width = "100%", Type = TableWidthUnitValues.Pct },
                new TableLayout() { Type = TableLayoutValues.Autofit }
            );
            table.AppendChild(tableProperties);

            // Заголовки таблицы
            var headerRow = new TableRow();
            string[] headers = { "ФИО", "Должность", "Телефон", "Email", "Соцсети" };
            int[] widths = { 30, 20, 15, 20, 15 };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = new TableCell(
                    new TableCellProperties(
                        new TableCellWidth() { Type = TableWidthUnitValues.Pct, Width = (widths[i] * 50).ToString() }
                    ),
                    new Paragraph(
                        new ParagraphProperties(
                            new Justification() { Val = JustificationValues.Center }
                        ),
                        new Run(
                            new RunProperties(new Bold()),
                            new Text(headers[i])
                        )
                    )
                );
                headerRow.AppendChild(cell);
            }
            table.AppendChild(headerRow);

            // Данные сотрудников
            foreach (DataRowView rowView in dataView)
            {
                var row = rowView.Row;
                var dataRow = new TableRow();

                var cellsData = new[]
                {
                    new { Value = row["FullName"]?.ToString() ?? "", Width = widths[0] },
                    new { Value = row["Position"]?.ToString() ?? "", Width = widths[1] },
                    new { Value = FormatPhoneNumber(row["PhoneNumber"]?.ToString()), Width = widths[2] },
                    new { Value = row["Email"]?.ToString() ?? "", Width = widths[3] },
                    new { Value = FormatSocialLink(row["SocialLink"]?.ToString()), Width = widths[4] }
                };

                foreach (var cellData in cellsData)
                {
                    var cell = new TableCell(
                        new TableCellProperties(
                            new TableCellWidth() { Type = TableWidthUnitValues.Pct, Width = (cellData.Width * 50).ToString() }
                        ),
                        new Paragraph(
                            new Run(
                                new Text(cellData.Value)
                            )
                        )
                    );
                    dataRow.AppendChild(cell);
                }

                table.AppendChild(dataRow);
            }

            return table;
        }

        private string FormatPhoneNumber(string phone)
        {
            if (string.IsNullOrEmpty(phone)) return "";

            var digits = new string(phone.Where(char.IsDigit).ToArray());

            if (digits.Length == 11)
            {
                return $"+7 ({digits.Substring(1, 3)}) {digits.Substring(4, 3)}-{digits.Substring(7, 2)}-{digits.Substring(9)}";
            }

            return phone;
        }

        private string FormatSocialLink(string link)
        {
            if (string.IsNullOrEmpty(link)) return "";
            return link.Replace("https://", "").Replace("http://", "");
        }
    }
}