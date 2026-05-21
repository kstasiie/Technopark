using System.Windows;
using Microsoft.EntityFrameworkCore;
using Technopark.Data;
using Technopark.Models;

namespace Technopark.Dialogs
{
    public partial class MaterialDialog : Window
    {
        private readonly int _projectId;
        private readonly int? _editMaterialId;

        // Конструктор: editMaterialId = null → добавление, иначе редактирование
        public MaterialDialog(int projectId, int? editMaterialId = null)
        {
            InitializeComponent();
            _projectId = projectId;
            _editMaterialId = editMaterialId;
            Loaded += async (s, e) => await InitAsync();
        }

        private async Task InitAsync()
        {
            using var db = new AppDbContext();

            // Справочник типов материалов
            var types = await db.MaterialTypes.OrderBy(t => t.Name).ToListAsync();
            TypeBox.ItemsSource = types;
            TypeBox.DisplayMemberPath = "Name";
            TypeBox.SelectedValuePath = "Id";

            if (_editMaterialId.HasValue)
            {
                // Режим редактирования — подгружаем данные
                var material = await db.ProjectMaterials
                    .FirstOrDefaultAsync(m => m.Id == _editMaterialId.Value);

                if (material == null)
                {
                    MessageBox.Show("Материал не найден. Возможно, он был удалён.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    DialogResult = false;
                    Close();
                    return;
                }

                TitleText.Text = "Редактировать материал";
                TypeBox.SelectedValue = material.TypeId;
                NameBox.Text = material.Name;
                LinkBox.Text = material.Link;
                UploadDatePicker.SelectedDate = material.UploadDate;
            }
            else
            {
                // Режим добавления — значения по умолчанию
                UploadDatePicker.SelectedDate = DateTime.Today;
                if (types.Count > 0)
                    TypeBox.SelectedIndex = 0;
                NameBox.Focus();
            }
        }

        private async void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            if (TypeBox.SelectedValue is not int typeId)
            {
                MessageBox.Show("Выберите тип материала.",
                    "Не заполнено поле", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var name = NameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Введите название материала.",
                    "Не заполнено поле", MessageBoxButton.OK, MessageBoxImage.Warning);
                NameBox.Focus();
                return;
            }

            var link = LinkBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(link))
            {
                MessageBox.Show("Введите ссылку на материал.",
                    "Не заполнено поле", MessageBoxButton.OK, MessageBoxImage.Warning);
                LinkBox.Focus();
                return;
            }

            if (link.Length > 255)
            {
                MessageBox.Show("Ссылка не должна превышать 255 символов.",
                    "Слишком длинная ссылка", MessageBoxButton.OK, MessageBoxImage.Warning);
                LinkBox.Focus();
                return;
            }

            if (UploadDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Укажите дату загрузки материала.",
                    "Не заполнено поле", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var uploadDate = UploadDatePicker.SelectedDate.Value;

            try
            {
                using var db = new AppDbContext();

                if (_editMaterialId.HasValue)
                {
                    var material = await db.ProjectMaterials
                        .FirstOrDefaultAsync(m => m.Id == _editMaterialId.Value);

                    if (material == null)
                    {
                        MessageBox.Show("Материал не найден. Возможно, он был удалён.",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        DialogResult = false;
                        Close();
                        return;
                    }

                    material.TypeId = typeId;
                    material.Name = name;
                    material.Link = link;
                    material.UploadDate = uploadDate;
                    await db.SaveChangesAsync();

                    MessageBox.Show("Материал успешно обновлён.",
                        "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    db.ProjectMaterials.Add(new ProjectMaterial
                    {
                        ProjectId = _projectId,
                        TypeId = typeId,
                        Name = name,
                        Link = link,
                        UploadDate = uploadDate
                    });
                    await db.SaveChangesAsync();

                    MessageBox.Show("Материал успешно добавлен.",
                        "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось сохранить материал:\n\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
