using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using Technopark.Data;
using Technopark.Models;
using Technopark.Services;

namespace Technopark.Dialogs
{
    public partial class ProjectDialog : Window
    {
        private readonly AppDbContext _db = new();
        private readonly Project? _editProject;
        private readonly bool _canEdit;

        public ProjectDialog(Project? project = null, bool canEdit = true)
        {
            InitializeComponent();
            _editProject = project;
            _canEdit = canEdit;
            Loaded += async (s, e) => await InitAsync();
        }

        private async Task InitAsync()
        {
            // Загружаем справочники
            var directions = await _db.Directions.OrderBy(d => d.Name).ToListAsync();
            DirectionBox.ItemsSource = directions;
            DirectionBox.DisplayMemberPath = "Name";
            DirectionBox.SelectedValuePath = "Id";

            var statuses = await _db.ProjectStatuses.ToListAsync();
            StatusBox.ItemsSource = statuses;
            StatusBox.DisplayMemberPath = "Name";
            StatusBox.SelectedValuePath = "Id";

            var mentors = await _db.MentorProfiles
                .OrderBy(m => m.LastName).ToListAsync();
            MentorBox.ItemsSource = mentors;
            MentorBox.DisplayMemberPath = "FullName";
            MentorBox.SelectedValuePath = "Id";

            var teams = await _db.Teams.OrderBy(t => t.Name).ToListAsync();
            TeamBox.ItemsSource = teams;
            TeamBox.DisplayMemberPath = "Name";
            TeamBox.SelectedValuePath = "Id";

            if (_editProject != null)
            {
                TitleText.Text = "Редактировать проект";
                NameBox.Text = _editProject.Name;
                DescriptionBox.Text = _editProject.Description ?? "";
                DirectionBox.SelectedValue = _editProject.DirectionId;
                StatusBox.SelectedValue = _editProject.StatusId;
                MentorBox.SelectedValue = _editProject.MentorId;
                TeamBox.SelectedValue = _editProject.TeamId;
                StartDatePicker.SelectedDate = _editProject.StartDate;
                EndDatePicker.SelectedDate = _editProject.PlannedEndDate;
            }
            else
            {
                StartDatePicker.SelectedDate = DateTime.Today;
                StatusBox.SelectedIndex = 0;

                // Если наставник — сразу выбираем его
                if (CurrentSession.IsMentor)
                {
                    var mentor = await _db.MentorProfiles
                        .FirstOrDefaultAsync(m => m.UserId == CurrentSession.UserId);
                    if (mentor != null)
                    {
                        MentorBox.SelectedValue = mentor.Id;
                        MentorBox.IsEnabled = false;
                    }
                }
            }

            // Блокируем поля если нет прав
            if (!_canEdit)
            {
                NameBox.IsReadOnly = true;
                DescriptionBox.IsReadOnly = true;
                DirectionBox.IsEnabled = false;
                StatusBox.IsEnabled = false;
                MentorBox.IsEnabled = false;
                TeamBox.IsEnabled = false;
                StartDatePicker.IsEnabled = false;
                EndDatePicker.IsEnabled = false;
                SaveBtn.Visibility = Visibility.Collapsed;
                TitleText.Text = "Просмотр проекта";
            }
        }

        private void NewTeamBtn_Click(object sender, RoutedEventArgs e)
        {
            NewTeamPanel.Visibility = Visibility.Visible;
            NewTeamNameBox.Focus();
        }

        private async void CreateTeamBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewTeamNameBox.Text))
            {
                MessageBox.Show("Введите название команды");
                return;
            }

            var team = new Team
            {
                Name = NewTeamNameBox.Text.Trim(),
                FormationYear = DateTime.Now.Year
            };
            _db.Teams.Add(team);
            await _db.SaveChangesAsync();

            var teams = await _db.Teams.OrderBy(t => t.Name).ToListAsync();
            TeamBox.ItemsSource = teams;
            TeamBox.SelectedValue = team.Id;

            NewTeamPanel.Visibility = Visibility.Collapsed;
            NewTeamNameBox.Text = "";
        }

        private async void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text) ||
                DirectionBox.SelectedValue == null ||
                StatusBox.SelectedValue == null ||
                MentorBox.SelectedValue == null ||
                TeamBox.SelectedValue == null ||
                StartDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Заполните все обязательные поля");
                return;
            }

            if (_editProject == null)
            {
                var project = new Project
                {
                    Name = NameBox.Text.Trim(),
                    Description = DescriptionBox.Text.Trim(),
                    DirectionId = (int)DirectionBox.SelectedValue,
                    StatusId = (int)StatusBox.SelectedValue,
                    MentorId = (int)MentorBox.SelectedValue,
                    TeamId = (int)TeamBox.SelectedValue,
                    StartDate = StartDatePicker.SelectedDate.Value,
                    PlannedEndDate = EndDatePicker.SelectedDate
                };
                _db.Projects.Add(project);
            }
            else
            {
                _editProject.Name = NameBox.Text.Trim();
                _editProject.Description = DescriptionBox.Text.Trim();
                _editProject.DirectionId = (int)DirectionBox.SelectedValue;
                _editProject.StatusId = (int)StatusBox.SelectedValue;
                _editProject.MentorId = (int)MentorBox.SelectedValue;
                _editProject.TeamId = (int)TeamBox.SelectedValue;
                _editProject.StartDate = StartDatePicker.SelectedDate.Value;
                _editProject.PlannedEndDate = EndDatePicker.SelectedDate;
                _db.Entry(_editProject).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
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
}