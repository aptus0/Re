using System.IO;
using System.Windows;
using Microsoft.Win32;
using Re.Desktop.ViewModels.Products;

namespace Re.Desktop.Views.Products;

public partial class ProductEditorWindow : Window
{
    public ProductEditorWindow() => InitializeComponent();

    private void SelectImage_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ProductListViewModel vm) return;
        var dialog = new OpenFileDialog { Filter = "Görseller|*.png;*.jpg;*.jpeg", Title = "Ürün görseli seç" };
        if (dialog.ShowDialog(this) != true) return;
        if (new FileInfo(dialog.FileName).Length > 5 * 1024 * 1024)
        {
            MessageBox.Show("Görsel 5 MB sınırını aşamaz.", "Ürün Görseli", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ReERP", "ProductImages");
        Directory.CreateDirectory(folder);
        var target = Path.Combine(folder, $"{Guid.NewGuid():N}{Path.GetExtension(dialog.FileName).ToLowerInvariant()}");
        File.Copy(dialog.FileName, target, false);
        vm.FormModel.ImagePath = target;
    }
    private void RemoveImage_Click(object sender, RoutedEventArgs e) { if (DataContext is ProductListViewModel vm) vm.FormModel.ImagePath = string.Empty; }
    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
