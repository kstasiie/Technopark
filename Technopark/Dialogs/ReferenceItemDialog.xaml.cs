using System.Windows;
using Microsoft.EntityFrameworkCore;
using Technopark.Data;
using Technopark.Models;

namespace Technopark.Dialogs
{
    /// <summary>
    /// Тип справочника, с которым работает универсальный диалог.
    /// </summary>
    public enum ReferenceType
    {
        Direction,
        ProjectStatus,
        ContestLevel,
        ParticipationResult,
        ProjectRole,
        MaterialType
    }

    public partial class ReferenceItemDialog : Window
    {
        private readonly ReferenceType _type;
        private readonly int? _editId;

        public ReferenceItemDialog(ReferenceType type, int? itemId = null)
        {
            InitializeComponent();
            _type = type;
            _editId = itemId;
            Loaded += async (s, e) => await InitAsync();
        }

        private async Task InitAsync()
        {
            // Поле «Описание» есть только у Направлений
            DescriptionPanel.Visibility = _type == ReferenceType.Direction
                ? Visibility.Visible : Visibility.Collapsed;

            string typeName = TypeDisplayName(_type);
            Title = typeName;

            if (_editId == null)
            {
                TitleText.Text = $"Добавить запись: {typeName}";
                NameBox.Focus();
                return;
            }

            TitleText.Text = $"Редактировать запись: {typeName}";

            using var db = new AppDbContext();
            switch (_type)
            {
                case ReferenceType.Direction:
                    var d = await db.Directions.FindAsync(_editId);
                    if (d != null)
                    {
                        NameBox.Text = d.Name;
                        DescriptionBox.Text = d.Description ?? "";
                    }
                    break;
                case ReferenceType.ProjectStatus:
                    var ps = await db.ProjectStatuses.FindAsync(_editId);
                    if (ps != null) NameBox.Text = ps.Name;
                    break;
                case ReferenceType.ContestLevel:
                    var cl = await db.ContestLevels.FindAsync(_editId);
                    if (cl != null) NameBox.Text = cl.Name;
                    break;
                case ReferenceType.ParticipationResult:
                    var pr = await db.ParticipationResults.FindAsync(_editId);
                    if (pr != null) NameBox.Text = pr.Name;
                    break;
                case ReferenceType.ProjectRole:
                    var role = await db.ProjectRoles.FindAsync(_editId);
                    if (role != null) NameBox.Text = role.Name;
                    break;
                case ReferenceType.MaterialType:
                    var mt = await db.MaterialTypes.FindAsync(_editId);
                    if (mt != null) NameBox.Text = mt.Name;
                    break;
            }

            NameBox.Focus();
            NameBox.SelectAll();
        }

        private async void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            var name = NameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Введите название.",
                    "Не заполнено поле", MessageBoxButton.OK, MessageBoxImage.Warning);
                NameBox.Focus();
                return;
            }

            string? description = null;
            if (_type == ReferenceType.Direction)
            {
                var desc = DescriptionBox.Text.Trim();
                description = string.IsNullOrWhiteSpace(desc) ? null : desc;
            }

            try
            {
                using var db = new AppDbContext();

                // Проверка дубликата по названию
                if (await IsDuplicateNameAsync(db, name))
                {
                    MessageBox.Show("Запись с таким названием уже существует. " +
                        "Выберите другое название.",
                        "Дубликат", MessageBoxButton.OK, MessageBoxImage.Warning);
                    NameBox.Focus();
                    return;
                }

                bool isNew = _editId == null;

                switch (_type)
                {
                    case ReferenceType.Direction:
                        if (isNew)
                        {
                            db.Directions.Add(new Direction
                            {
                                Name = name,
                                Description = description
                            });
                        }
                        else
                        {
                            var d = await db.Directions.FindAsync(_editId);
                            if (d == null) { ShowNotFound(); return; }
                            d.Name = name;
                            d.Description = description;
                        }
                        break;
                    case ReferenceType.ProjectStatus:
                        if (isNew) db.ProjectStatuses.Add(new ProjectStatus { Name = name });
                        else
                        {
                            var ps = await db.ProjectStatuses.FindAsync(_editId);
                            if (ps == null) { ShowNotFound(); return; }
                            ps.Name = name;
                        }
                        break;
                    case ReferenceType.ContestLevel:
                        if (isNew) db.ContestLevels.Add(new ContestLevel { Name = name });
                        else
                        {
                            var cl = await db.ContestLevels.FindAsync(_editId);
                            if (cl == null) { ShowNotFound(); return; }
                            cl.Name = name;
                        }
                        break;
                    case ReferenceType.ParticipationResult:
                        if (isNew) db.ParticipationResults.Add(new ParticipationResult { Name = name });
                        else
                        {
                            var pr = await db.ParticipationResults.FindAsync(_editId);
                            if (pr == null) { ShowNotFound(); return; }
                            pr.Name = name;
                        }
                        break;
                    case ReferenceType.ProjectRole:
                        if (isNew) db.ProjectRoles.Add(new ProjectRole { Name = name });
                        else
                        {
                            var role = await db.ProjectRoles.FindAsync(_editId);
                            if (role == null) { ShowNotFound(); return; }
                            role.Name = name;
                        }
                        break;
                    case ReferenceType.MaterialType:
                        if (isNew) db.MaterialTypes.Add(new MaterialType { Name = name });
                        else
                        {
                            var mt = await db.MaterialTypes.FindAsync(_editId);
                            if (mt == null) { ShowNotFound(); return; }
                            mt.Name = name;
                        }
                        break;
                }

                await db.SaveChangesAsync();

                MessageBox.Show(
                    isNew ? "Запись успешно создана." : "Запись успешно обновлена.",
                    "Готово", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось сохранить запись:\n\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<bool> IsDuplicateNameAsync(AppDbContext db, string name)
        {
            int excludeId = _editId ?? -1;
            return _type switch
            {
                ReferenceType.Direction => await db.Directions
                    .AnyAsync(x => x.Id != excludeId && x.Name == name),
                ReferenceType.ProjectStatus => await db.ProjectStatuses
                    .AnyAsync(x => x.Id != excludeId && x.Name == name),
                ReferenceType.ContestLevel => await db.ContestLevels
                    .AnyAsync(x => x.Id != excludeId && x.Name == name),
                ReferenceType.ParticipationResult => await db.ParticipationResults
                    .AnyAsync(x => x.Id != excludeId && x.Name == name),
                ReferenceType.ProjectRole => await db.ProjectRoles
                    .AnyAsync(x => x.Id != excludeId && x.Name == name),
                ReferenceType.MaterialType => await db.MaterialTypes
                    .AnyAsync(x => x.Id != excludeId && x.Name == name),
                _ => false
            };
        }

        private void ShowNotFound()
        {
            MessageBox.Show("Запись не найдена. Возможно, она была удалена.",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            DialogResult = false;
            Close();
        }

        public static string TypeDisplayName(ReferenceType type) => type switch
        {
            ReferenceType.Direction => "Направление",
            ReferenceType.ProjectStatus => "Статус проекта",
            ReferenceType.ContestLevel => "Уровень конкурса",
            ReferenceType.ParticipationResult => "Результат участия",
            ReferenceType.ProjectRole => "Проектная роль",
            ReferenceType.MaterialType => "Тип материала",
            _ => "Запись"
        };

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
