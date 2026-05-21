using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using Technopark.Data;
using Technopark.Dialogs;
using Technopark.Services;

namespace Technopark.Views
{
    public partial class ReferenceDataPage : Page
    {
        public ReferenceDataPage()
        {
            InitializeComponent();
            // Защита маршрута: только админ
            if (!CurrentSession.IsAdmin)
            {
                MessageBox.Show("Управление справочниками доступно только администратору.",
                    "Доступ запрещён", MessageBoxButton.OK, MessageBoxImage.Warning);
                IsEnabled = false;
                return;
            }
            Loaded += async (s, e) => await LoadAllAsync();
        }

        // ───────────────────────── Загрузка всех вкладок ─────────────────────────

        private async Task LoadAllAsync()
        {
            await LoadDirectionsAsync();
            await LoadStatusesAsync();
            await LoadLevelsAsync();
            await LoadResultsAsync();
            await LoadRolesAsync();
            await LoadMaterialTypesAsync();
        }

        private async Task LoadDirectionsAsync()
        {
            using var db = new AppDbContext();
            var items = await db.Directions
                .OrderBy(d => d.Name)
                .Select(d => new
                {
                    d.Id,
                    d.Name,
                    Description = d.Description ?? "",
                    UsageCount = db.Projects.Count(p => p.DirectionId == d.Id)
                               + db.MentorProfiles.Count(m => m.DirectionId == d.Id)
                })
                .ToListAsync();
            DirectionsGrid.ItemsSource = items;
        }

        private async Task LoadStatusesAsync()
        {
            using var db = new AppDbContext();
            var items = await db.ProjectStatuses
                .OrderBy(x => x.Name)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    UsageCount = db.Projects.Count(p => p.StatusId == x.Id)
                })
                .ToListAsync();
            StatusesGrid.ItemsSource = items;
        }

        private async Task LoadLevelsAsync()
        {
            using var db = new AppDbContext();
            var items = await db.ContestLevels
                .OrderBy(x => x.Name)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    UsageCount = db.Contests.Count(c => c.LevelId == x.Id)
                })
                .ToListAsync();
            LevelsGrid.ItemsSource = items;
        }

        private async Task LoadResultsAsync()
        {
            using var db = new AppDbContext();
            var items = await db.ParticipationResults
                .OrderBy(x => x.Name)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    UsageCount = db.ContestParticipations.Count(cp => cp.ResultId == x.Id)
                })
                .ToListAsync();
            ResultsGrid.ItemsSource = items;
        }

        private async Task LoadRolesAsync()
        {
            using var db = new AppDbContext();
            var items = await db.ProjectRoles
                .OrderBy(x => x.Name)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    UsageCount = db.TeamMembers.Count(tm => tm.RoleId == x.Id)
                })
                .ToListAsync();
            RolesGrid.ItemsSource = items;
        }

        private async Task LoadMaterialTypesAsync()
        {
            using var db = new AppDbContext();
            var items = await db.MaterialTypes
                .OrderBy(x => x.Name)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    UsageCount = db.ProjectMaterials.Count(pm => pm.TypeId == x.Id)
                })
                .ToListAsync();
            MaterialTypesGrid.ItemsSource = items;
        }

        // ───────────────────────── Универсальная кнопка «Добавить» ─────────────

        private async void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentSession.IsAdmin) return;
            var type = CurrentTabType();
            var dialog = new ReferenceItemDialog(type) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true) await ReloadTabAsync(type);
        }

        private ReferenceType CurrentTabType() => MainTabs.SelectedIndex switch
        {
            0 => ReferenceType.Direction,
            1 => ReferenceType.ProjectStatus,
            2 => ReferenceType.ContestLevel,
            3 => ReferenceType.ParticipationResult,
            4 => ReferenceType.ProjectRole,
            5 => ReferenceType.MaterialType,
            _ => ReferenceType.Direction
        };

        private async Task ReloadTabAsync(ReferenceType type)
        {
            switch (type)
            {
                case ReferenceType.Direction: await LoadDirectionsAsync(); break;
                case ReferenceType.ProjectStatus: await LoadStatusesAsync(); break;
                case ReferenceType.ContestLevel: await LoadLevelsAsync(); break;
                case ReferenceType.ParticipationResult: await LoadResultsAsync(); break;
                case ReferenceType.ProjectRole: await LoadRolesAsync(); break;
                case ReferenceType.MaterialType: await LoadMaterialTypesAsync(); break;
            }
        }

        // ───────────────────────── Редактирование (6 обработчиков) ──────────────

        private async void EditDirection_Click(object sender, RoutedEventArgs e) =>
            await EditAsync(sender, ReferenceType.Direction);
        private async void EditStatus_Click(object sender, RoutedEventArgs e) =>
            await EditAsync(sender, ReferenceType.ProjectStatus);
        private async void EditLevel_Click(object sender, RoutedEventArgs e) =>
            await EditAsync(sender, ReferenceType.ContestLevel);
        private async void EditResult_Click(object sender, RoutedEventArgs e) =>
            await EditAsync(sender, ReferenceType.ParticipationResult);
        private async void EditRole_Click(object sender, RoutedEventArgs e) =>
            await EditAsync(sender, ReferenceType.ProjectRole);
        private async void EditMaterialType_Click(object sender, RoutedEventArgs e) =>
            await EditAsync(sender, ReferenceType.MaterialType);

        private async Task EditAsync(object sender, ReferenceType type)
        {
            if (!CurrentSession.IsAdmin) return;
            if (sender is not Button btn || btn.Tag is not int id) return;

            var dialog = new ReferenceItemDialog(type, id) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true) await ReloadTabAsync(type);
        }

        // ───────────────────────── Удаление (6 обработчиков) ────────────────────

        private async void DeleteDirection_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not int id) return;
            await DeleteAsync(id, ReferenceType.Direction);
        }
        private async void DeleteStatus_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not int id) return;
            await DeleteAsync(id, ReferenceType.ProjectStatus);
        }
        private async void DeleteLevel_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not int id) return;
            await DeleteAsync(id, ReferenceType.ContestLevel);
        }
        private async void DeleteResult_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not int id) return;
            await DeleteAsync(id, ReferenceType.ParticipationResult);
        }
        private async void DeleteRole_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not int id) return;
            await DeleteAsync(id, ReferenceType.ProjectRole);
        }
        private async void DeleteMaterialType_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not int id) return;
            await DeleteAsync(id, ReferenceType.MaterialType);
        }

        private async Task DeleteAsync(int id, ReferenceType type)
        {
            if (!CurrentSession.IsAdmin) return;

            try
            {
                using var db = new AppDbContext();

                // 1) Проверяем, что запись существует и получаем её имя для сообщений
                string? itemName = await GetItemNameAsync(db, id, type);
                if (itemName == null)
                {
                    MessageBox.Show("Запись не найдена. Возможно, она уже была удалена.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    await ReloadTabAsync(type);
                    return;
                }

                // 2) Жёсткая защита: считаем количество ссылок и запрещаем удаление, если есть
                int usage = await CountUsageAsync(db, id, type);
                if (usage > 0)
                {
                    MessageBox.Show(
                        $"Запись «{itemName}» используется в {usage} связанных " +
                        $"{Pluralize(usage, "записи", "записях", "записях")}. " +
                        "Удаление невозможно.\n\n" +
                        "Чтобы удалить эту запись, сначала переназначьте связанные данные " +
                        "на другое значение этого справочника.",
                        "Удаление невозможно", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 3) Подтверждение
                var typeName = ReferenceItemDialog.TypeDisplayName(type).ToLower();
                var result = MessageBox.Show(
                    $"Удалить {typeName} «{itemName}»?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes) return;

                // 4) Удаление
                await DeleteByTypeAsync(db, id, type);
                await db.SaveChangesAsync();

                MessageBox.Show($"Запись «{itemName}» успешно удалена.",
                    "Готово", MessageBoxButton.OK, MessageBoxImage.Information);

                await ReloadTabAsync(type);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось удалить запись:\n\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ───────────────────────── Хелперы ───────────────────────────────────────

        private static async Task<string?> GetItemNameAsync(AppDbContext db, int id, ReferenceType type) =>
            type switch
            {
                ReferenceType.Direction => (await db.Directions.FindAsync(id))?.Name,
                ReferenceType.ProjectStatus => (await db.ProjectStatuses.FindAsync(id))?.Name,
                ReferenceType.ContestLevel => (await db.ContestLevels.FindAsync(id))?.Name,
                ReferenceType.ParticipationResult => (await db.ParticipationResults.FindAsync(id))?.Name,
                ReferenceType.ProjectRole => (await db.ProjectRoles.FindAsync(id))?.Name,
                ReferenceType.MaterialType => (await db.MaterialTypes.FindAsync(id))?.Name,
                _ => null
            };

        private static async Task<int> CountUsageAsync(AppDbContext db, int id, ReferenceType type) =>
            type switch
            {
                ReferenceType.Direction =>
                    await db.Projects.CountAsync(p => p.DirectionId == id)
                  + await db.MentorProfiles.CountAsync(m => m.DirectionId == id),
                ReferenceType.ProjectStatus =>
                    await db.Projects.CountAsync(p => p.StatusId == id),
                ReferenceType.ContestLevel =>
                    await db.Contests.CountAsync(c => c.LevelId == id),
                ReferenceType.ParticipationResult =>
                    await db.ContestParticipations.CountAsync(cp => cp.ResultId == id),
                ReferenceType.ProjectRole =>
                    await db.TeamMembers.CountAsync(tm => tm.RoleId == id),
                ReferenceType.MaterialType =>
                    await db.ProjectMaterials.CountAsync(pm => pm.TypeId == id),
                _ => 0
            };

        private static async Task DeleteByTypeAsync(AppDbContext db, int id, ReferenceType type)
        {
            switch (type)
            {
                case ReferenceType.Direction:
                    var d = await db.Directions.FindAsync(id);
                    if (d != null) db.Directions.Remove(d);
                    break;
                case ReferenceType.ProjectStatus:
                    var ps = await db.ProjectStatuses.FindAsync(id);
                    if (ps != null) db.ProjectStatuses.Remove(ps);
                    break;
                case ReferenceType.ContestLevel:
                    var cl = await db.ContestLevels.FindAsync(id);
                    if (cl != null) db.ContestLevels.Remove(cl);
                    break;
                case ReferenceType.ParticipationResult:
                    var pr = await db.ParticipationResults.FindAsync(id);
                    if (pr != null) db.ParticipationResults.Remove(pr);
                    break;
                case ReferenceType.ProjectRole:
                    var role = await db.ProjectRoles.FindAsync(id);
                    if (role != null) db.ProjectRoles.Remove(role);
                    break;
                case ReferenceType.MaterialType:
                    var mt = await db.MaterialTypes.FindAsync(id);
                    if (mt != null) db.MaterialTypes.Remove(mt);
                    break;
            }
        }

        // 1 запись, 2 записи, 5 записей — простое склонение по русскому
        private static string Pluralize(int n, string one, string few, string many)
        {
            int mod100 = n % 100;
            int mod10 = n % 10;
            if (mod100 >= 11 && mod100 <= 14) return many;
            return mod10 switch
            {
                1 => one,
                2 or 3 or 4 => few,
                _ => many
            };
        }
    }
}
