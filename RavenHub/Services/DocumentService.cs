using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RavenHub.Models;

// Алиасы для устранения конфликтов
using WordDocument = DocumentFormat.OpenXml.Wordprocessing.Document;
using PdfDocument = QuestPDF.Fluent.Document;

namespace RavenHub.Services
{
    public class DocumentService : IDocumentService
    {
        public async Task<string> GenerateEmploymentDocumentAsync(Employee employee, DocumentFormat format)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Generating {format} document for {employee.FullName}");

                var result = format == DocumentFormat.Word
                    ? await GenerateEmploymentDocumentWordAsync(employee)
                    : await GenerateEmploymentDocumentPdfAsync(employee);

                System.Diagnostics.Debug.WriteLine($"Document generated: {result}");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GenerateEmploymentDocumentAsync: {ex}");
                throw new Exception($"Ошибка при генерации документа: {ex.Message}", ex);
            }
        }

        public async Task<string> GenerateDismissalDocumentAsync(Employee employee, DocumentFormat format)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Generating dismissal {format} document for {employee.FullName}");

                var result = format == DocumentFormat.Word
                    ? await GenerateDismissalDocumentWordAsync(employee)
                    : await GenerateDismissalDocumentPdfAsync(employee);

                System.Diagnostics.Debug.WriteLine($"Dismissal document generated: {result}");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GenerateDismissalDocumentAsync: {ex}");
                throw new Exception($"Ошибка при генерации документа: {ex.Message}", ex);
            }
        }

        private async Task<string> GenerateEmploymentDocumentWordAsync(Employee employee)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Создаем папку Documents если её нет
                    var documentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RavenHub");
                    if (!Directory.Exists(documentsPath))
                    {
                        Directory.CreateDirectory(documentsPath);
                    }

                    string fileName = Path.Combine(documentsPath, $"Трудоустройство_{employee.FullName.Replace(" ", "_")}_{DateTime.Now:ddMMyyyy_HHmmss}.docx");

                    using (var wordDocument = WordprocessingDocument.Create(fileName, WordprocessingDocumentType.Document))
                    {
                        var mainPart = wordDocument.AddMainDocumentPart();
                        mainPart.Document = new WordDocument();
                        var body = mainPart.Document.AppendChild(new Body());

                        AddParagraph(body, "ТРУДОВОЙ ДОГОВОР", true, "28", JustificationValues.Center);
                        AddParagraph(body, "(Договор о трудоустройстве)", false, "22", JustificationValues.Center);
                        AddParagraph(body, "");
                        AddParagraph(body, $"г. Москва                                                              {DateTime.Now:dd MMMM yyyy} г.");
                        AddParagraph(body, "");
                        AddParagraph(body, $"Настоящий договор заключен между работодателем и {employee.FullName}, " +
                                          "именуемым в дальнейшем \"Работник\", о нижеследующем:");
                        AddParagraph(body, "");
                        AddEmployeeInfo(body, employee);
                        AddParagraph(body, "");
                        AddParagraph(body, $"1. Работник принимается на работу в должности {employee.Position} " +
                                          "с испытательным сроком 3 месяца.");
                        AddParagraph(body, "2. Работник обязуется выполнять свои должностные обязанности в соответствии с должностной инструкцией.");
                        AddParagraph(body, "3. Работодатель обязуется обеспечить Работника необходимыми условиями труда.");
                        AddSignatureSection(body);

                        mainPart.Document.Save();
                    }

                    return fileName;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in GenerateEmploymentDocumentWordAsync: {ex}");
                    throw;
                }
            });
        }

        private async Task<string> GenerateEmploymentDocumentPdfAsync(Employee employee)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Устанавливаем лицензию QuestPDF
                    QuestPDF.Settings.License = LicenseType.Community;

                    var documentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RavenHub");
                    if (!Directory.Exists(documentsPath))
                    {
                        Directory.CreateDirectory(documentsPath);
                    }

                    string fileName = Path.Combine(documentsPath, $"Трудоустройство_{employee.FullName.Replace(" ", "_")}_{DateTime.Now:ddMMyyyy_HHmmss}.pdf");

                    PdfDocument.Create(container =>
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4);
                            page.Margin(2, Unit.Centimetre);
                            page.DefaultTextStyle(x => x.FontSize(12));

                            page.Content()
                                .Column(column =>
                                {
                                    column.Item().Text("ТРУДОВОЙ ДОГОВОР")
                                        .FontSize(20)
                                        .Bold()
                                        .AlignCenter();

                                    column.Item().Text("(Договор о трудоустройстве)")
                                        .FontSize(16)
                                        .AlignCenter();

                                    column.Item().PaddingVertical(10);

                                    column.Item().Row(row =>
                                    {
                                        row.RelativeItem().Text("г. Москва");
                                        row.RelativeItem().Text($"{DateTime.Now:dd MMMM yyyy} г.").AlignRight();
                                    });

                                    column.Item().PaddingVertical(10);

                                    column.Item().Text($"Настоящий договор заключен между работодателем и {employee.FullName}, " +
                                                      "именуемым в дальнейшем \"Работник\", о нижеследующем:");

                                    column.Item().PaddingVertical(10);

                                    column.Item().Text($"ФИО работника: {employee.FullName}");
                                    column.Item().Text($"Должность: {employee.Position}");
                                    column.Item().Text($"Контактный телефон: {FormatPhoneNumber(employee.PhoneNumber)}");
                                    column.Item().Text($"Email: {employee.Email}");
                                    column.Item().Text($"Дата приема на работу: {DateTime.Now:dd.MM.yyyy}");

                                    column.Item().PaddingVertical(10);

                                    column.Item().Text($"1. Работник принимается на работу в должности {employee.Position} " +
                                                      "с испытательным сроком 3 месяца.");
                                    column.Item().Text("2. Работник обязуется выполнять свои должностные обязанности в соответствии с должностной инструкцией.");
                                    column.Item().Text("3. Работодатель обязуется обеспечить Работника необходимыми условиями труда.");

                                    column.Item().PaddingVertical(20);

                                    column.Item().Text($"Дата составления: {DateTime.Now:dd.MM.yyyy}");
                                    column.Item().PaddingVertical(20);

                                    column.Item().Row(row =>
                                    {
                                        row.RelativeItem().Column(col =>
                                        {
                                            col.Item().Text("Работодатель:");
                                            col.Item().PaddingTop(20);
                                            col.Item().Text("_________________");
                                            col.Item().Text("(подпись)").FontSize(10);
                                        });

                                        row.RelativeItem().Column(col =>
                                        {
                                            col.Item().Text("Работник:");
                                            col.Item().PaddingTop(20);
                                            col.Item().Text("_________________");
                                            col.Item().Text("(подпись)").FontSize(10);
                                        });
                                    });
                                });
                        });
                    })
                    .GeneratePdf(fileName);

                    return fileName;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in GenerateEmploymentDocumentPdfAsync: {ex}");
                    throw;
                }
            });
        }

        private async Task<string> GenerateDismissalDocumentWordAsync(Employee employee)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var documentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RavenHub");
                    if (!Directory.Exists(documentsPath))
                    {
                        Directory.CreateDirectory(documentsPath);
                    }

                    string fileName = Path.Combine(documentsPath, $"Увольнение_{employee.FullName.Replace(" ", "_")}_{DateTime.Now:ddMMyyyy_HHmmss}.docx");

                    using (var wordDocument = WordprocessingDocument.Create(fileName, WordprocessingDocumentType.Document))
                    {
                        var mainPart = wordDocument.AddMainDocumentPart();
                        mainPart.Document = new WordDocument();
                        var body = mainPart.Document.AppendChild(new Body());

                        AddParagraph(body, "ПРИКАЗ", true, "28", JustificationValues.Center);
                        AddParagraph(body, $"№ {new Random().Next(100, 999)} от {DateTime.Now:dd.MM.yyyy}", false, "22", JustificationValues.Center);
                        AddParagraph(body, "ОБ УВОЛЬНЕНИИ", true, "24", JustificationValues.Center);
                        AddParagraph(body, "");
                        AddParagraph(body, "На основании трудового законодательства и заявления работника,");
                        AddParagraph(body, "");
                        AddParagraph(body, "ПРИКАЗЫВАЮ:", true);
                        AddParagraph(body, "");
                        AddEmployeeInfo(body, employee);
                        AddParagraph(body, "");
                        AddParagraph(body, $"1. Уволить {employee.FullName} с должности {employee.Position} " +
                                          $"с {DateTime.Now:dd.MM.yyyy} по собственному желанию.");
                        AddParagraph(body, "2. Бухгалтерии произвести полный расчет с работником.");
                        AddParagraph(body, "3. Отделу кадров оформить необходимые документы.");
                        AddSignatureSection(body);

                        mainPart.Document.Save();
                    }

                    return fileName;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in GenerateDismissalDocumentWordAsync: {ex}");
                    throw;
                }
            });
        }

        private async Task<string> GenerateDismissalDocumentPdfAsync(Employee employee)
        {
            return await Task.Run(() =>
            {
            try
            {
                QuestPDF.Settings.License = LicenseType.Community;

                var documentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RavenHub");
                if (!Directory.Exists(documentsPath))
                {
                    Directory.CreateDirectory(documentsPath);
                }

                string fileName = Path.Combine(documentsPath, $"Увольнение_{employee.FullName.Replace(" ", "_")}_{DateTime.Now:ddMMyyyy_HHmmss}.pdf");

                PdfDocument.Create(container =>
                {
                    container.Page(page =>
                    {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Content()
                        .Column(column =>
                        {
                        column.Item().Text("ПРИКАЗ")
                                    .FontSize(20)
                                    .Bold()
                                    .AlignCenter();

                        column.Item().Text($"№ {new Random().Next(100, 999)} от {DateTime.Now:dd.MM.yyyy}")
                                    .FontSize(14)
                                    .AlignCenter();

                        column.Item().Text("ОБ УВОЛЬНЕНИИ")
                                    .FontSize(18)
                                    .Bold()
                                    .AlignCenter();

                        column.Item().PaddingVertical(10);

                        column.Item().Text("На основании трудового законодательства и заявления работника,");

                        column.Item().PaddingVertical(10);

                        column.Item().Text("ПРИКАЗЫВАЮ:").Bold();

                        column.Item().PaddingVertical(10);

                        column.Item().Text($"ФИО работника: {employee.FullName}");
                        column.Item().Text($"Должность: {employee.Position}");
                        column.Item().Text($"Контактный телефон: {FormatPhoneNumber(employee.PhoneNumber)}");
                        column.Item().Text($"Email: {employee.Email}");

                        column.Item().PaddingVertical(10);

                        column.Item().Text($"1. Уволить {employee.FullName} с должности {employee.Position} " +
                                                  $"с {DateTime.Now:dd.MM.yyyy} по собственному желанию.");
                        column.Item().Text("2. Бухгалтерии произвести полный расчет с работником.");
                        column.Item().Text("3. Отделу кадров оформить необходимые документы.");

                        column.Item().PaddingVertical(20);

                        column.Item().Text($"Дата составления: {DateTime.Now:dd.MM.yyyy}");
                        column.Item().PaddingVertical(20);

                        column.Item().Text("Директор: _________________");
                            column.Item().PaddingVertical(10);
                            column.Item().Text("С приказом ознакомлен:");
                            column.Item().Text("Работник: _________________");
                        });
                    });
                })
                    .GeneratePdf(fileName);

                    return fileName;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in GenerateDismissalDocumentPdfAsync: {ex}");
                    throw;
                }
            });
        }

        private void AddParagraph(Body body, string text, bool isBold = false, string fontSize = "22",
            JustificationValues? justification = null)
        {
            var runProperties = new RunProperties();
            if (isBold) runProperties.AppendChild(new Bold());
            if (!string.IsNullOrEmpty(fontSize)) runProperties.AppendChild(new FontSize() { Val = fontSize });

            var paragraphProperties = new ParagraphProperties();
            if (justification.HasValue)
            {
                paragraphProperties.AppendChild(new Justification() { Val = justification.Value });
            }
            paragraphProperties.AppendChild(new SpacingBetweenLines() { After = "200" });

            var paragraph = new Paragraph();
            paragraph.AppendChild(paragraphProperties);

            var run = new Run();
            run.AppendChild(runProperties);
            run.AppendChild(new Text(text));

            paragraph.AppendChild(run);
            body.AppendChild(paragraph);
        }

        private void AddEmployeeInfo(Body body, Employee employee)
        {
            AddParagraph(body, $"ФИО работника: {employee.FullName}");
            AddParagraph(body, $"Должность: {employee.Position}");
            AddParagraph(body, $"Контактный телефон: {FormatPhoneNumber(employee.PhoneNumber)}");
            AddParagraph(body, $"Email: {employee.Email ?? "не указан"}");
        }

        private void AddSignatureSection(Body body)
        {
            AddParagraph(body, "");
            AddParagraph(body, "");
            AddParagraph(body, $"Дата составления: {DateTime.Now:dd.MM.yyyy}");
            AddParagraph(body, "");
            AddParagraph(body, "Подпись работодателя: _________________");
            AddParagraph(body, "");
            AddParagraph(body, "Подпись работника: _________________");
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
    }
}