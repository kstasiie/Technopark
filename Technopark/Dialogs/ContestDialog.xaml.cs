using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using Technopark.Data;
using Technopark.Models;

namespace Technopark.Dialogs
{
    public partial class ContestDialog : Window
    {
        private readonly int? _editContestId;
        private readonly bool _canEdit;
        private ObservableCollection<ParticipationRow> _participations = [];

        public ContestDialog(Contest? contest = null, bool canEdit = true)
        {
            InitializeComponent();
            _editContestId = contest?.Id;
            _canEdit = canEdit;
            Loaded += async (s, e) => await InitAsync();
        }

        private async Task InitAsync()
        {
            using var db = new AppDbContext();

            var levels = await db.ContestLevels.ToListAsync();
            LevelBox.ItemsSource = levels;
            LevelBox.DisplayMemberPath = "Name";
            LevelBox.SelectedValuePath = "Id";

            var projects = await db.Projects
                .Include(p => p.Direction)
                .OrderBy(p => p.Name)
                .ToListAsync();
            ProjectBox.ItemsSource = projects;
            ProjectBox.DisplayMemberPath = "Name";
            ProjectBox.SelectedValuePath = "Id";

            var results = await db.ParticipationResults.ToListAsync();
            var resultCol = (DataGridComboBoxColumn)ParticipationsGrid.Columns[1];
            resultCol.ItemsSource = results;
            resultCol.DisplayMemberPath = "Name";
            resultCol.SelectedValuePath = "Id";

            if (_editContestId != null)
            {
                var contest = await db.Contests
                    .FirstOrDefaultAsync(c => c.Id == _editContestId);

                if (contest != null)
                {
                    TitleText.Text = "Редактировать конкурс";
                    NameBox.Text = contest.Name;
                    OrganizerBox.Text = contest.Organizer;
                    LevelBox.SelectedValue = contest.LevelId;
                    DatePicker.SelectedDate = contest.Date;

                    var participations = await db.ContestParticipations
                        .Include(cp => cp.Project)
                        .Where(cp => cp.ContestId == _editContestId)
                        .ToListAsync();

                    _participations = new ObservableCollection<ParticipationRow>(
                        participations.Select(cp => new ParticipationRow
                        {
                            Id = cp.Id,
                            ProjectId = cp.ProjectId,
                            ProjectName = cp.Project?.Name ?? "",
                            ResultId = cp.ResultId,
                            Place = cp.Place
                        }));
                }
            }
            else
            {
                DatePicker.SelectedDate = DateTime.Today;
                _participations = [];
            }

            ParticipationsGrid.ItemsSource = _participations;

            if (!_canEdit)
            {
                NameBox.IsReadOnly = true;
                OrganizerBox.IsReadOnly = true;
                LevelBox.IsEnabled = false;
                DatePicker.IsEnabled = false;
                ProjectBox.IsEnabled = false;
                SaveBtn.Visibility = Visibility.Collapsed;
                TitleText.Text = "Просмотр конкурса";
            }
        }

        private void AddParticipationBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectBox.SelectedValue is not int projectId) return;

            if (_participations.Any(p => p.ProjectId == projectId))
            {
                MessageBox.Show("Этот проект уже добавлен");
                return;
            }

            var project = (Project)ProjectBox.SelectedItem!;
            _participations.Add(new ParticipationRow
            {
                ProjectId = projectId,
                ProjectName = project.Name
            });
        }

        private async void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text) ||
                string.IsNullOrWhiteSpace(OrganizerBox.Text) ||
                LevelBox.SelectedValue == null ||
                DatePicker.SelectedDate == null)
            {
                MessageBox.Show("Заполните все обязательные поля");
                return;
            }

            using var db = new AppDbContext();

            int contestId;
            if (_editContestId == null)
            {
                var contest = new Contest
                {
                    Name = NameBox.Text.Trim(),
                    Organizer = OrganizerBox.Text.Trim(),
                    LevelId = (int)LevelBox.SelectedValue,
                    Date = DatePicker.SelectedDate.Value
                };
                db.Contests.Add(contest);
                await db.SaveChangesAsync();
                contestId = contest.Id;
            }
            else
            {
                contestId = _editContestId.Value;

                // Создаём "заглушку" с Id и говорим EF обновить только нужные поля
                var contest = new Contest
                {
                    Id = contestId,
                    Name = NameBox.Text.Trim(),
                    Organizer = OrganizerBox.Text.Trim(),
                    LevelId = (int)LevelBox.SelectedValue,
                    Date = DatePicker.SelectedDate.Value
                };
                db.Entry(contest).State = EntityState.Modified;
                await db.SaveChangesAsync();

                // Удаляем старые участия
                var old = await db.ContestParticipations
                    .Where(cp => cp.ContestId == contestId)
                    .ToListAsync();
                db.ContestParticipations.RemoveRange(old);
                await db.SaveChangesAsync();
            }

            // Добавляем актуальный список участий
            foreach (var row in _participations)
            {
                db.ContestParticipations.Add(new ContestParticipation
                {
                    ContestId = contestId,
                    ProjectId = row.ProjectId,
                    ResultId = row.ResultId,
                    Place = row.Place,
                    ApplicationDate = DateTime.Today
                });
            }
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

    public class ParticipationRow
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = "";
        public int? ResultId { get; set; }
        public int? Place { get; set; }
    }
}