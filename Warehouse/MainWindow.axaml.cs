using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System.Diagnostics;
using System.IO;


namespace Warehouse
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _client = new HttpClient { BaseAddress = new Uri("http://localhost:5003/") };
        private ObservableCollection<Product> _products = new ObservableCollection<Product>();
        private string _currentTemplate = "GridTemplate";
        private ListBox? _productsList;

        public MainWindow()
        {
            InitializeComponent();

            _productsList = this.FindControl<ListBox>("ProductsList");
            if (_productsList != null)
            {
                _productsList.ItemsSource = _products;
                _productsList.ItemTemplate = this.FindResource(_currentTemplate) as IDataTemplate;
            }

            _ = LoadProductsFromApi(); // загрузка при старте
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        // ЗАГРУЗКА ТОВАРА
      
        private async Task LoadProductsFromApi()
        {
            try
            {
                var productsFromApi = await _client.GetFromJsonAsync<List<ProductApi>>("products");
                if (productsFromApi != null)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _products.Clear();
                        foreach (var p in productsFromApi)
                        {
                            _products.Add(new Product
                            {
                                Id = p.WarehouseId,
                                Name = p.Name,
                                Quantity = p.Quantity
                            });
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                await ShowMessage("Ошибка", "Не удалось загрузить товары: " + ex.Message);
            }
        }

      
        // ДОБАВЛЕНИЕ ТОВАРА

        private async void Add_Click(object? sender, RoutedEventArgs e)
        {
            var nameInput = this.FindControl<TextBox>("NameInput");
            var qtyInput = this.FindControl<TextBox>("QtyInput");

            if (nameInput == null || qtyInput == null) return;

            var name = nameInput.Text?.Trim();
            if (!int.TryParse(qtyInput.Text, out int qty) || qty < 0)
            {
                await ShowMessage("Ошибка", "Введите корректное количество.");
                return;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                await ShowMessage("Ошибка", "Название товара не может быть пустым.");
                return;
            }

            try
            {
                var newProduct = new ProductApi { Name = name, Quantity = qty };
                var response = await _client.PostAsJsonAsync("products", newProduct);
                if (response.IsSuccessStatusCode)
                {
                    nameInput.Text = "";
                    qtyInput.Text = "";
                    await LoadProductsFromApi();
                    await ShowMessage("Успех", "Товар добавлен!");
                }
                else
                {
                    var errorText = await response.Content.ReadAsStringAsync();
                    await ShowMessage("Ошибка добавления", errorText);
                }
            }
            catch (Exception ex)
            {
                await ShowMessage("Ошибка", ex.Message);
            }
        }

       
        // УДАЛИТЬ ТОВАРЫ
        
        private async Task DeleteProductAsync(Product selected)
        {
            try
            {
               
                var confirm = await ShowConfirmDialog($"Удалить товар '{selected.Name}'?");
                if (!confirm) return;

                var response = await _client.DeleteAsync($"products/{selected.Id}");
                
                if (response.IsSuccessStatusCode)
                {
                  
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _products.Remove(selected);
                    });
                    
                    await ShowMessage("Успех", $"Товар '{selected.Name}' удален.");
                }
                else
                {
                    var errorText = await response.Content.ReadAsStringAsync();
                    await ShowMessage("Ошибка удаления", $"Статус: {response.StatusCode}\n{errorText}");
                }
            }
            catch (Exception ex)
            {
                await ShowMessage("Ошибка", ex.Message);
            }
        }

        private async void Delete_Click(object? sender, RoutedEventArgs e)
        {
            if (_productsList?.SelectedItem is Product selected)
                await DeleteProductAsync(selected);
            else
                await ShowMessage("Внимание", "Выберите товар для удаления.");
        }
        
        private async void Issue_Click(object? sender, RoutedEventArgs e)
        {
            if (_productsList?.SelectedItem is not Product selectedItem)
            {
                await ShowMessage("Внимание", "Выберите товар для выдачи.");
                return;
            }
        }
      
        private async Task<bool> ShowConfirmDialog(string message)
        {
            var tcs = new TaskCompletionSource<bool>();

            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var dialog = new Window
                {
                    Title = "Подтверждение",
                    Width = 300,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Content = new StackPanel
                    {
                        Margin = new Avalonia.Thickness(20),
                        Children =
                        {
                            new TextBlock 
                            { 
                                Text = message,
                                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                                Margin = new Avalonia.Thickness(0, 0, 0, 20)
                            },
                            new StackPanel
                            {
                                Orientation = Avalonia.Layout.Orientation.Horizontal,
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                Spacing = 10,
                                Children =
                                {
                                    new Button { Content = "Да", Width = 70 },
                                    new Button { Content = "Нет", Width = 70 }
                                }
                            }
                        }
                    }
                };

                var buttons = ((StackPanel)dialog.Content).Children[1] as StackPanel;
                var yesButton = buttons?.Children[0] as Button;
                var noButton = buttons?.Children[1] as Button;

                if (yesButton != null)
                    yesButton.Click += (s, args) => { tcs.SetResult(true); dialog.Close(); };
                
                if (noButton != null)
                    noButton.Click += (s, args) => { tcs.SetResult(false); dialog.Close(); };

                dialog.Closed += (s, args) =>
                {
                    if (!tcs.Task.IsCompleted)
                        tcs.SetResult(false);
                };

                await dialog.ShowDialog(this);
            });

            return await tcs.Task;
        }

     
        // ВИДЫ
   
        private void SwitchView_Click(object? sender, RoutedEventArgs e)
        {
            if (_productsList == null) return;
            _currentTemplate = _currentTemplate == "GridTemplate" ? "ListTemplate" : "GridTemplate";
            _productsList.ItemTemplate = this.FindResource(_currentTemplate) as IDataTemplate;
        }

       
        // ОТЧЕТЫ
        
        private async void Report_Click(object? sender, RoutedEventArgs e)
        {
            var dialog = new Window
            {
                Title = "Выберите формат отчета",
                Width = 300,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(20),
                    Spacing = 10,
                    Children =
                    {
                        new Button { Content = "PDF" },
                        new Button { Content = "Excel" },
                        new Button { Content = "Word" }
                    }
                }
            };

            var panel = dialog.Content as StackPanel;

            var pdfBtn = panel?.Children[0] as Button;
            var excelBtn = panel?.Children[1] as Button;
            var wordBtn = panel?.Children[2] as Button;

            if (pdfBtn != null)
                pdfBtn.Click += async (s, args) =>
                {
                    dialog.Close();
                    await DownloadAndOpenReport("Products/report/pdf", "Report.pdf", "pdf");
                };

            if (excelBtn != null)
                excelBtn.Click += async (s, args) =>
                {
                    dialog.Close();
                    await DownloadAndOpenReport("Products/report/excel", "Report.xlsx", "xlsx");
                };

            if (wordBtn != null)
                wordBtn.Click += async (s, args) =>
                {
                    dialog.Close();
                    await DownloadAndOpenReport("Products/report/word", "Report.docx", "docx");
                };

            await dialog.ShowDialog(this);
        }
        

        private async Task ShowMessage(string title, string message)
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var messageBox = new Window
                {
                    Title = title,
                    Width = 400,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Content = new StackPanel
                    {
                        Margin = new Avalonia.Thickness(20),
                        Children =
                        {
                            new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap, Margin = new Avalonia.Thickness(0,0,0,20) },
                            new Button { Content = "OK", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, Width = 100 }
                        }
                    }
                };
                var button = ((StackPanel)messageBox.Content).Children[1] as Button;
                if (button != null) button.Click += (s, args) => messageBox.Close();
                await messageBox.ShowDialog(this);
            });
        }
        
        private async Task DownloadAndOpenReport(string url, string fileName, string extension)
        {
            using var client = new HttpClient();
            var response = await client.GetAsync("http://localhost:5003/" + url);

            response.EnsureSuccessStatusCode();

            var bytes = await response.Content.ReadAsByteArrayAsync();
            var filePath = Path.Combine(Path.GetTempPath(), fileName);

            await File.WriteAllBytesAsync(filePath, bytes);
            
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            
            
        }

    }
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Quantity { get; set; }
        public string NameAndQty => $"{Name} ({Quantity})";
    }

    public class ProductApi
    {
        public int WarehouseId { get; set; }
        public string Name { get; set; } = "";
        public int Quantity { get; set; }
    }
}