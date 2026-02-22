using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;


namespace Warehouse;

public partial class GiveWindow : Window
{
    private readonly int _productId;
    private readonly string _productName;
    private readonly int _currentQuantity;
    private readonly HttpClient _http = new HttpClient();

    public GiveWindow(int productId, string productName, int currentQuantity)
    {
        InitializeComponent();
        _productId = productId;
        _productName = productName;
        _currentQuantity = currentQuantity;
        
        ItemNameTextBox.Text = productName;
        InfoTextBlock.Text = $"В наличии: {currentQuantity}";
    }

    private async void GiveButton_Click(object sender, RoutedEventArgs e)
    {
        var receiver = ReceiverTextBox.Text;
        var quantityText = QuantityTextBox.Text;

        if (string.IsNullOrWhiteSpace(receiver))
        {
            await ShowMessage("Ошибка", "Введите получателя");
            return;
        }

        if (!int.TryParse(quantityText, out int quantity) || quantity < 1)
        {
            await ShowMessage("Ошибка", "Введите корректное количество");
            return;
        }

        if (quantity > _currentQuantity)
        {
            await ShowMessage("Ошибка", $"Недостаточно предметов! Доступно: {_currentQuantity}");
            return;
        }

        try
        {
            GiveButton.IsEnabled = false;
            
            var json = JsonSerializer.Serialize(quantity);

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            var response = await _http.PostAsync(
                $"http://localhost:5003/Products/{_productId}/give",
                content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                await ShowMessage("Ошибка", error);
                GiveButton.IsEnabled = true;
                return;
            }
            
            var responseJson = await response.Content.ReadAsStringAsync();

            var updatedProduct = JsonSerializer.Deserialize<Warehouse>(
                responseJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            await ShowMessage("Успех",
                $"Предмет '{_productName}'\n" +
                $"Передано: {quantity} шт. {receiver}\n" +
                $"Осталось: {updatedProduct?.Quantity}");

            Close();
        }
        catch (Exception ex)
        {
            await ShowMessage("Ошибка", $"Не удалось соединиться: {ex.Message}");
            GiveButton.IsEnabled = true;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async Task ShowMessage(string title, string text)
    {
        var msg = new Window
        {
            Title = title,
            Content = text,
            Width = 300,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        await msg.ShowDialog(this);
        
    }
    public class Warehouse
    {
        public int WarehouseId { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
    }
    
}