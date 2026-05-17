using System.Windows;
using Microsoft.EntityFrameworkCore;
using Technopark.Data;
using Technopark.Models;

namespace Technopark.Dialogs
{
    public partial class AddToContestDialog : Window
    {
        private readonly int _projectId;

        public AddToContestDialog(int projectId)
        {
            InitializeComponent();
            _projectId = projectId;
            Loaded += async (s, e) => await InitAsync();
        }

        private async Task InitAsync()
        {
            using var db = new AppDbContext();

            // Конкурсы, в которых проект ещё не участвует
            var existingIds = await db.ContestParticipations
                .Where(cp => cp.ProjectId == _projectId)
                .Select(cp => cp.ContestId)
                .ToListAsync();

            var availableContests = await db.Contests
                .Where(c => !existingIds.Contains(c.Id))
                .OrderByDescending(c => c.Date)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            ContestBox.ItemsSource = availableContests;
            ContestBox.DisplayMemberPath = "Name";
            ContestBox.SelectedValuePath = "Id";

            var results = await db.ParticipationResults
                .Select(r => new { Id = (int?)r.Id, r.Name })
                .ToListAsync();
            var withEmpty = new List<object> {
                new { Id = (int?)null, Name = "Не указан" }
            };
            withEmpty.AddRange(results.Cast<object>());
            ResultBox.ItemsSource = withEmpty;
            ResultBox.DisplayMemberPath = "Name";
            ResultBox.SelectedValuePath = "Id";
            ResultBox.SelectedIndex = 0;

            if (availableContests.Count == 0)
            {
                NoContestsText.Visibility = Visibility.Visible;
                SaveBtn.IsEnabled = false;
            }
        }

        private async void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ContestBox.SelectedValue is not int contestId)
            {
                MessageBox.Show("Выберите конкурс");
                return;
            }

            int? resultId = ResultBox.SelectedValue as int?;

            int? place = null;
            if (!string.IsNullOrWhiteSpace(PlaceBox.Text))
            {
                if (!int.TryParse(PlaceBox.Text.Trim(), out int p) || p < 1)
                {
                    MessageBox.Show("Место должно быть числом больше 0");
                    return;
                }
                place = p;
            }

            using var db = new AppDbContext();
            db.ContestParticipations.Add(new ContestParticipation
            {
                ProjectId = _projectId,
                ContestId = contestId,
                ResultId = resultId,
                Place = place,
                ApplicationDate = DateTime.Today
            });
            await db.SaveChangesAsync();

            DialogResult = true;
            Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}