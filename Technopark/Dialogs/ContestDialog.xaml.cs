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
        private readonly AppDbContext _db = new();
        private readonly Contest? _editContest;
        private readonly bool _canEdit;
        private ObservableCollection<ParticipationRow> _participations = [];

        public ContestDialog(Contest? contest = null, bool canEdit = true)
        {
            InitializeComponent();
            _editContest = contest;
            _canEdit = canEdit;
            Loaded += async (s, e) => await InitAsync();
        }

        private async Task InitAsync()
        {
            var levels = await _db.ContestLevels.ToListAsync();
            LevelBox.ItemsSource = levels;
            LevelBox.DisplayMemberPath = "Name";
            LevelBox.SelectedValuePath = "Id";

            var projects = await _db.Projects
                .Include(p => p.Direction)
                .OrderBy(p => p.Name)
                .ToListAsync();
            ProjectBox.ItemsSource = projects;
            ProjectBox.DisplayMemberPath = "Name";
            ProjectBox.SelectedValuePath = "Id";

            var results = await _db.ParticipationResults.ToListAsync();

            // Настраиваем колонку результатов
            var resultCol = (DataGridComboBoxColumn)ParticipationsGrid.Columns[1];
            resultCol.ItemsSource = results;
            resultCol.DisplayMemberPath = "Name";
            resultCol.SelectedValuePath = "Id";

            if (_editContest != null)
            {
                TitleText.Text = "Редактировать конкурс";
                NameBox.Text = _editContest.Name;
                OrganizerBox.Text = _editContest.Organizer;
                LevelBox.SelectedValue = _editContest.LevelId;
                DatePicker.SelectedDate = _editContest.Date;

                var participations = await _db.ContestParticipations
                    .Include(cp => cp.Project)
                    .Include(cp => cp.Result)
                    .Where(cp => cp.ContestId == _editContest.Id)
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

            Contest contest;
            if (_editContest == null)
            {
                contest = new Contest
                {
                    Name = NameBox.Text.Trim(),
                    Organizer = OrganizerBox.Text.Trim(),
                    LevelId = (int)LevelBox.SelectedValue,
                    Date = DatePicker.SelectedDate.Value
                };
                _db.Contests.Add(contest);
                await _db.SaveChangesAsync();
            }
            else
            {
                contest = _editContest;
                contest.Name = NameBox.Text.Trim();
                contest.Organizer = OrganizerBox.Text.Trim();
                contest.LevelId = (int)LevelBox.SelectedValue;
                contest.Date = DatePicker.SelectedDate.Value;
                _db.Contests.Update(contest);
                await _db.SaveChangesAsync();

                // Удаляем старые участия
                var old = await _db.ContestParticipations
                    .Where(cp => cp.ContestId == contest.Id)
                    .ToListAsync();
                _db.ContestParticipations.RemoveRange(old);
                await _db.SaveChangesAsync();
            }

            // Сохраняем участия
            foreach (var row in _participations)
            {
                _db.ContestParticipations.Add(new ContestParticipation
                {
                    ContestId = contest.Id,
                    ProjectId = row.ProjectId,
                    ResultId = row.ResultId,
                    Place = row.Place,
                    ApplicationDate = DateTime.Today
                });
            }

            await _db.SaveChangesAsync();
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