using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using Technopark.Data;
using Technopark.Models;

namespace Technopark.Dialogs
{
    public partial class TeamDialog : Window
    {
        private readonly int? _editTeamId;
        private readonly bool _canEdit;
        private ObservableCollection<MemberRow> _members = [];

        public TeamDialog(Team? team = null, bool canEdit = true)
        {
            InitializeComponent();
            _editTeamId = team?.Id;
            _canEdit = canEdit;
            Loaded += async (s, e) => await InitAsync();
        }

        private async Task InitAsync()
        {
            using var db = new AppDbContext();

            var students = await db.StudentProfiles
                .OrderBy(s => s.LastName).ToListAsync();
            StudentBox.ItemsSource = students;
            StudentBox.DisplayMemberPath = "FullName";
            StudentBox.SelectedValuePath = "Id";

            var roles = await db.ProjectRoles.ToListAsync();
            RoleBox.ItemsSource = roles;
            RoleBox.DisplayMemberPath = "Name";
            RoleBox.SelectedValuePath = "Id";
            RoleBox.SelectedIndex = 0;

            if (_editTeamId != null)
            {
                var team = await db.Teams
                    .Include(t => t.Members).ThenInclude(m => m.Student)
                    .Include(t => t.Members).ThenInclude(m => m.Role)
                    .FirstOrDefaultAsync(t => t.Id == _editTeamId);

                if (team != null)
                {
                    TitleText.Text = "Редактировать команду";
                    NameBox.Text = team.Name;
                    YearBox.Text = team.FormationYear.ToString();

                    _members = new ObservableCollection<MemberRow>(
                        team.Members.Select(m => new MemberRow
                        {
                            StudentId = m.StudentId,
                            StudentName = m.Student?.FullName ?? "",
                            RoleId = m.RoleId,
                            RoleName = m.Role?.Name ?? ""
                        }));
                }
            }
            else
            {
                YearBox.Text = DateTime.Now.Year.ToString();
                _members = [];
            }

            MembersGrid.ItemsSource = _members;

            if (!_canEdit)
            {
                NameBox.IsReadOnly = true;
                YearBox.IsReadOnly = true;
                StudentBox.IsEnabled = false;
                RoleBox.IsEnabled = false;
                SaveBtn.Visibility = Visibility.Collapsed;
                TitleText.Text = "Просмотр команды";
            }
        }

        private void AddMemberBtn_Click(object sender, RoutedEventArgs e)
        {
            if (StudentBox.SelectedValue is not int studentId) return;
            if (RoleBox.SelectedValue is not int roleId) return;

            if (_members.Any(m => m.StudentId == studentId))
            {
                MessageBox.Show("Этот участник уже добавлен в команду");
                return;
            }

            var student = StudentBox.SelectedItem as StudentProfile;
            var role = RoleBox.SelectedItem as ProjectRole;

            _members.Add(new MemberRow
            {
                StudentId = studentId,
                StudentName = student?.FullName ?? "",
                RoleId = roleId,
                RoleName = role?.Name ?? ""
            });
        }

        private void RemoveMember_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int studentId)
            {
                var member = _members.FirstOrDefault(m => m.StudentId == studentId);
                if (member != null) _members.Remove(member);
            }
        }

        private async void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("Введите название команды");
                return;
            }

            if (!int.TryParse(YearBox.Text, out int year) || year < 2000 || year > 2100)
            {
                MessageBox.Show("Введите корректный год (например, 2025)");
                return;
            }

            using var db = new AppDbContext();

            if (_editTeamId == null)
            {
                var team = new Team
                {
                    Name = NameBox.Text.Trim(),
                    FormationYear = year
                };
                db.Teams.Add(team);
                await db.SaveChangesAsync();

                foreach (var row in _members)
                {
                    db.TeamMembers.Add(new TeamMember
                    {
                        TeamId = team.Id,
                        StudentId = row.StudentId,
                        RoleId = row.RoleId,
                        InclusionDate = DateTime.Today
                    });
                }
                await db.SaveChangesAsync();
            }
            else
            {
                // Загружаем команду свежим запросом в наш контекст
                var team = await db.Teams
                    .Include(t => t.Members)
                    .FirstOrDefaultAsync(t => t.Id == _editTeamId);

                if (team == null)
                {
                    MessageBox.Show("Команда не найдена");
                    return;
                }

                team.Name = NameBox.Text.Trim();
                team.FormationYear = year;

                // Удаляем всех старых участников
                db.TeamMembers.RemoveRange(team.Members);
                await db.SaveChangesAsync();

                // Добавляем новых
                foreach (var row in _members)
                {
                    db.TeamMembers.Add(new TeamMember
                    {
                        TeamId = team.Id,
                        StudentId = row.StudentId,
                        RoleId = row.RoleId,
                        InclusionDate = DateTime.Today
                    });
                }
                await db.SaveChangesAsync();
            }

            DialogResult = true;
            Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class MemberRow
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = "";
        public int RoleId { get; set; }
        public string RoleName { get; set; } = "";
    }
}