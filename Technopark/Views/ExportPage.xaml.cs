using System.IO;
using System.Windows;
using System.Windows.Controls;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using Technopark.Data;
using Technopark.Services;

namespace Technopark.Views
{
    public partial class ExportPage : Page
    {

        public ExportPage()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                if (!CurrentSession.IsAdmin)
                    MentorsCard.Visibility = Visibility.Collapsed;
            };
        }

        private async void ExportBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            var tag = btn.Tag?.ToString() ?? "";

            var parts = tag.Split('_');
            var report = parts[0];
            var format = parts[1];

            try
            {
                if (format == "Excel")
                    await ExportExcelAsync(report);
                else
                    await ExportWordAsync(report);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте:\n{ex.Message}");
            }
        }

        private async Task ExportExcelAsync(string report)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Excel файл (*.xlsx)|*.xlsx",
                FileName = $"{GetReportName(report)}_{DateTime.Today:dd-MM-yyyy}"
            };
            if (dialog.ShowDialog() != true) return;

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add(GetReportName(report));

            if (report == "Projects")
                await FillProjectsExcel(ws);
            else if (report == "Contests")
                await FillContestsExcel(ws);
            else if (report == "Mentors")
                await FillMentorsExcel(ws);

            // Стиль заголовков
            var header = ws.Row(1);
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.FromHtml("#534AB7");
            header.Style.Font.FontColor = XLColor.White;
            ws.Columns().AdjustToContents();

            wb.SaveAs(dialog.FileName);
            MessageBox.Show("Файл успешно сохранён!", "Экспорт",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task FillProjectsExcel(IXLWorksheet ws)
        {
            ws.Cell(1, 1).Value = "Название";
            ws.Cell(1, 2).Value = "Направление";
            ws.Cell(1, 3).Value = "Наставник";
            ws.Cell(1, 4).Value = "Команда";
            ws.Cell(1, 5).Value = "Дата начала";
            ws.Cell(1, 6).Value = "Статус";

            var projects = await GetProjectsAsync();
            int row = 2;
            foreach (var p in projects)
            {
                ws.Cell(row, 1).Value = p.Name;
                ws.Cell(row, 2).Value = p.Direction?.Name ?? "";
                ws.Cell(row, 3).Value = p.Mentor?.FullName ?? "";
                ws.Cell(row, 4).Value = p.Team?.Name ?? "";
                ws.Cell(row, 5).Value = p.StartDate.ToString("dd.MM.yyyy");
                ws.Cell(row, 6).Value = p.Status?.Name ?? "";
                row++;
            }
        }

        private async Task FillContestsExcel(IXLWorksheet ws)
        {
            ws.Cell(1, 1).Value = "Конкурс";
            ws.Cell(1, 2).Value = "Организатор";
            ws.Cell(1, 3).Value = "Уровень";
            ws.Cell(1, 4).Value = "Дата";
            ws.Cell(1, 5).Value = "Проект";
            ws.Cell(1, 6).Value = "Результат";
            ws.Cell(1, 7).Value = "Место";

            var participations = await GetParticipationsAsync();
            int row = 2;
            foreach (var cp in participations)
            {
                ws.Cell(row, 1).Value = cp.Contest?.Name ?? "";
                ws.Cell(row, 2).Value = cp.Contest?.Organizer ?? "";
                ws.Cell(row, 3).Value = cp.Contest?.Level?.Name ?? "";
                ws.Cell(row, 4).Value = cp.Contest?.Date.ToString("dd.MM.yyyy") ?? "";
                ws.Cell(row, 5).Value = cp.Project?.Name ?? "";
                ws.Cell(row, 6).Value = cp.Result?.Name ?? "—";
                ws.Cell(row, 7).Value = cp.Place?.ToString() ?? "—";
                row++;
            }
        }

        private async Task FillMentorsExcel(IXLWorksheet ws)
        {
            using var db = new AppDbContext();

            ws.Cell(1, 1).Value = "ФИО наставника";
            ws.Cell(1, 2).Value = "Должность";
            ws.Cell(1, 3).Value = "Направление";
            ws.Cell(1, 4).Value = "Кол-во проектов";

            var mentors = await db.MentorProfiles
                .Include(m => m.Direction)
                .Include(m => m.Projects)
                .OrderBy(m => m.LastName)
                .ToListAsync();

            int row = 2;
            foreach (var m in mentors)
            {
                ws.Cell(row, 1).Value = m.FullName;
                ws.Cell(row, 2).Value = m.Position;
                ws.Cell(row, 3).Value = m.Direction?.Name ?? "";
                ws.Cell(row, 4).Value = m.Projects.Count;
                row++;
            }
        }

        private async Task ExportWordAsync(string report)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Word файл (*.docx)|*.docx",
                FileName = $"{GetReportName(report)}_{DateTime.Today:dd-MM-yyyy}"
            };
            if (dialog.ShowDialog() != true) return;

            using var doc = WordprocessingDocument.Create(
                dialog.FileName, WordprocessingDocumentType.Document);

            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            // Заголовок
            AddWordTitle(body, GetReportName(report));
            AddWordParagraph(body,
                $"Дата формирования: {DateTime.Today:dd.MM.yyyy}");
            AddWordParagraph(body, "");

            if (report == "Projects")
                await FillProjectsWord(body);
            else if (report == "Contests")
                await FillContestsWord(body);
            else if (report == "Mentors")
                await FillMentorsWord(body);

            mainPart.Document.Save();
            MessageBox.Show("Файл успешно сохранён!", "Экспорт",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task FillProjectsWord(Body body)
        {
            var projects = await GetProjectsAsync();
            var headers = new[] { "Название", "Направление", "Наставник",
                                   "Команда", "Дата начала", "Статус" };
            var rows = projects.Select(p => new[]
            {
                p.Name, p.Direction?.Name ?? "", p.Mentor?.FullName ?? "",
                p.Team?.Name ?? "", p.StartDate.ToString("dd.MM.yyyy"),
                p.Status?.Name ?? ""
            }).ToList();

            AddWordTable(body, headers, rows);
        }

        private async Task FillContestsWord(Body body)
        {
            var participations = await GetParticipationsAsync();
            var headers = new[] { "Конкурс", "Уровень", "Проект",
                                   "Результат", "Место" };
            var rows = participations.Select(cp => new[]
            {
                cp.Contest?.Name ?? "", cp.Contest?.Level?.Name ?? "",
                cp.Project?.Name ?? "", cp.Result?.Name ?? "—",
                cp.Place?.ToString() ?? "—"
            }).ToList();

            AddWordTable(body, headers, rows);
        }

        private async Task FillMentorsWord(Body body)
        {
            using var db = new AppDbContext();
            var mentors = await db.MentorProfiles
                .Include(m => m.Direction)
                .Include(m => m.Projects)
                .OrderBy(m => m.LastName)
                .ToListAsync();

            var headers = new[] { "ФИО", "Должность", "Направление",
                                   "Кол-во проектов" };
            var rows = mentors.Select(m => new[]
            {
                m.FullName, m.Position,
                m.Direction?.Name ?? "",
                m.Projects.Count.ToString()
            }).ToList();

            AddWordTable(body, headers, rows);
        }

        // Вспомогательные методы Word
        private static void AddWordTitle(Body body, string text)
        {
            var para = new Paragraph();
            var run = new Run();
            run.AppendChild(new RunProperties(
                new Bold(), new FontSize { Val = "32" }));
            run.AppendChild(new Text(text));
            para.AppendChild(new ParagraphProperties(
                new Justification { Val = JustificationValues.Center }));
            para.AppendChild(run);
            body.AppendChild(para);
        }

        private static void AddWordParagraph(Body body, string text)
        {
            var para = new Paragraph();
            var run = new Run(new Text(text));
            para.AppendChild(run);
            body.AppendChild(para);
        }

        private static void AddWordTable(Body body, string[] headers,
            List<string[]> rows)
        {
            var table = new Table();
            table.AppendChild(new TableProperties(
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 4 },
                    new BottomBorder { Val = BorderValues.Single, Size = 4 },
                    new LeftBorder { Val = BorderValues.Single, Size = 4 },
                    new RightBorder { Val = BorderValues.Single, Size = 4 },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
                )
            ));

            // Заголовок
            var headerRow = new TableRow();
            foreach (var h in headers)
            {
                var cell = new TableCell(new Paragraph(
                    new Run(
                        new RunProperties(new Bold()),
                        new Text(h))));
                cell.AppendChild(new TableCellProperties(
                    new Shading
                    {
                        Fill = "534AB7",
                        Color = "FFFFFF",
                        Val = ShadingPatternValues.Clear
                    }));
                headerRow.AppendChild(cell);
            }
            table.AppendChild(headerRow);

            // Данные
            foreach (var row in rows)
            {
                var tableRow = new TableRow();
                foreach (var cell in row)
                    tableRow.AppendChild(new TableCell(
                        new Paragraph(new Run(new Text(cell ?? "")))));
                table.AppendChild(tableRow);
            }

            body.AppendChild(table);
        }

        // Получение данных с учётом роли
        private async Task<List<Models.Project>> GetProjectsAsync()
        {
            using var db = new AppDbContext();
            var query = db.Projects
                .Include(p => p.Direction)
                .Include(p => p.Mentor)
                .Include(p => p.Team)
                .Include(p => p.Status)
                .AsQueryable();

            if (CurrentSession.IsMentor)
            {
                var mentor = await db.MentorProfiles
                    .FirstOrDefaultAsync(m => m.UserId == CurrentSession.UserId);
                if (mentor != null)
                    query = query.Where(p => p.MentorId == mentor.Id);
            }

            return await query.OrderBy(p => p.Name).ToListAsync();
        }

        private async Task<List<Models.ContestParticipation>> GetParticipationsAsync()
        {
            using var db = new AppDbContext();

            var query = db.ContestParticipations
                .Include(cp => cp.Contest).ThenInclude(c => c!.Level)
                .Include(cp => cp.Project)
                .Include(cp => cp.Result)
                .AsQueryable();

            if (CurrentSession.IsMentor)
            {
                var mentor = await db.MentorProfiles
                    .FirstOrDefaultAsync(m => m.UserId == CurrentSession.UserId);
                if (mentor != null)
                    query = query.Where(cp => cp.Project!.MentorId == mentor.Id);
            }

            return await query.ToListAsync();
        }

        private static string GetReportName(string report) => report switch
        {
            "Projects" => "Список проектов",
            "Contests" => "Результаты конкурсов",
            "Mentors" => "Наставники",
            _ => report
        };
    }
}