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
        private readonly AppDbContext _db = new();
        private readonly Team? _editTeam;
        private readonly bool _canEdit;
        private ObservableCollection<MemberRow> _members = [];

        public TeamDialog(Team? team = null, bool canEdit = true)
        {
            InitializeComponent();
            _editTeam = team;
            _canEdit = canEdit;
            Loaded += async (s, e) => await InitAsync();
        }

        private async Task InitAsync()
        {
            // Студенты
            var students = await _db.StudentProfiles
                .OrderBy(s => s.LastName)
                .ToListAsync();
            StudentBox.ItemsSource = students;
            StudentBox.DisplayMemberPath = "FullName";
            StudentBox.SelectedValuePath = "Id";

            // Роли
            var roles = await _db.ProjectRoles.ToListAsync();
            RoleBox.ItemsSource = roles;
            RoleBox.DisplayMemberPath = "Name";
            RoleBox.SelectedValuePath = "Id";
            RoleBox.SelectedIndex = 0;

            if (_editTeam != null)
            {
                TitleText.Text = "Редактировать команду";
                NameBox.Text = _editTeam.Name;
                YearBox.Text = _editTeam.FormationYear.ToString();

                _members = new ObservableCollection<MemberRow>(
                    _editTeam.Members.Select(m => new MemberRow
                    {
                        MemberId = m.Id,
                        StudentId = m.StudentId,
                        StudentName = m.Student?.FullName ?? "",
                        RoleId = m.RoleId,
                        RoleName = m.Role?.Name ?? ""
                    }));
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
            var role = (RoleBox.SelectedItem as ProjectRole);

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

            if (_editTeam == null)
            {
                var team = new Team
                {
                    Name = NameBox.Text.Trim(),
                    FormationYear = year
                };
                _db.Teams.Add(team);
                await _db.SaveChangesAsync();

                foreach (var row in _members)
                {
                    _db.TeamMembers.Add(new TeamMember
                    {
                        TeamId = team.Id,
                        StudentId = row.StudentId,
                        RoleId = row.RoleId,
                        InclusionDate = DateTime.Today
                    });
                }
            }
            else
            {
                _editTeam.Name = NameBox.Text.Trim();
                _editTeam.FormationYear = year;
                _db.Entry(_editTeam).State =
                    Microsoft.EntityFrameworkCore.EntityState.Modified;

                // Удаляем старых участников
                var old = await _db.TeamMembers
                    .Where(tm => tm.TeamId == _editTeam.Id)
                    .ToListAsync();
                _db.TeamMembers.RemoveRange(old);

                foreach (var row in _members)
                {
                    _db.TeamMembers.Add(new TeamMember
                    {
                        TeamId = _editTeam.Id,
                        StudentId = row.StudentId,
                        RoleId = row.RoleId,
                        InclusionDate = DateTime.Today
                    });
                }
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

    public class MemberRow
    {
        public int MemberId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = "";
        public int RoleId { get; set; }
        public string RoleName { get; set; } = "";
    }
}