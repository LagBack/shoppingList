using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace shoppinglist
{
    public class HouseholdItem : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = string.Empty;
        private string _category = string.Empty;
        private string? _imagePath;
        private int _timesPurchased;
        private DateTime? _lastPurchased;

        public string Id 
        { 
            get => _id; 
            set { _id = value; OnPropertyChanged(nameof(Id)); } 
        }

        public string Name 
        { 
            get => _name; 
            set { _name = value; OnPropertyChanged(nameof(Name)); } 
        }

        public string Category 
        { 
            get => _category; 
            set { _category = value; OnPropertyChanged(nameof(Category)); } 
        }

        public string? ImagePath 
        { 
            get => _imagePath; 
            set { _imagePath = value; OnPropertyChanged(nameof(ImagePath)); } 
        }

        public int TimesPurchased 
        { 
            get => _timesPurchased; 
            set { _timesPurchased = value; OnPropertyChanged(nameof(TimesPurchased)); } 
        }

        public DateTime? LastPurchased 
        { 
            get => _lastPurchased; 
            set { _lastPurchased = value; OnPropertyChanged(nameof(LastPurchased)); } 
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class ShoppingItem : INotifyPropertyChanged
    {
        private bool _isPurchased;
        private string _name = string.Empty;
        private string? _catalogItemId;

        public string Name 
        { 
            get => _name; 
            set 
            { 
                _name = value; 
                OnPropertyChanged(nameof(Name)); 
            } 
        }

        public bool IsPurchased 
        { 
            get => _isPurchased; 
            set 
            { 
                _isPurchased = value; 
                OnPropertyChanged(nameof(IsPurchased)); 
            } 
        }

        public string? CatalogItemId 
        { 
            get => _catalogItemId; 
            set 
            { 
                _catalogItemId = value; 
                OnPropertyChanged(nameof(CatalogItemId)); 
            } 
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    public partial class MainWindow : Window
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        private List<ShoppingItem> _items = new List<ShoppingItem>();
        private List<HouseholdItem> _catalog = new List<HouseholdItem>();

        private string? _pendingImagePath = null;

        private const string SaveFilePath = "shoppinglist.json";
        private const string CatalogFilePath = "catalog.json";

        public MainWindow()
        {
            InitializeComponent();
            LoadList();
            LoadCatalog();
            RefreshList();
        }

        private void LoadCatalog()
        {
            try
            {
                if (File.Exists(CatalogFilePath))
                {
                    string json = File.ReadAllText(CatalogFilePath);
                    _catalog = JsonSerializer.Deserialize<List<HouseholdItem>>(json) ?? new List<HouseholdItem>();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading catalog: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _catalog = new List<HouseholdItem>();
            }
            RefreshCatalog();
        }

        private void SaveCatalog()
        {
            try
            {
                string json = JsonSerializer.Serialize(_catalog, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(CatalogFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving catalog: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshCatalog()
        {
            CatalogListBox.ItemsSource = null;
            CatalogListBox.ItemsSource = _catalog.OrderByDescending(i => i.TimesPurchased).ToList();
        }

        private void LoadList()
        {
            try
            {
                if (File.Exists(SaveFilePath))
                {
                    string json = File.ReadAllText(SaveFilePath);
                    _items = JsonSerializer.Deserialize<List<ShoppingItem>>(json) ?? new List<ShoppingItem>();

                    foreach (var item in _items)
                    {
                        item.PropertyChanged += Item_PropertyChanged;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading shopping list: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _items = new List<ShoppingItem>();
            }
        }

        private void SaveList()
        {
            try
            {
                string json = JsonSerializer.Serialize(_items, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SaveFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving shopping list: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ShoppingItem.IsPurchased) || e.PropertyName == nameof(ShoppingItem.Name))
            {
                if (e.PropertyName == nameof(ShoppingItem.IsPurchased) && sender is ShoppingItem item && item.IsPurchased && !string.IsNullOrEmpty(item.CatalogItemId))
                {
                    var catItem = _catalog.FirstOrDefault(c => c.Id == item.CatalogItemId);
                    if (catItem != null)
                    {
                        catItem.TimesPurchased++;
                        catItem.LastPurchased = DateTime.Now;
                        SaveCatalog();
                        RefreshCatalog(); // Optional: updates the UI to show the new count
                    }
                }
                SaveList();
            }
        }

        private void AddItem()
        {
            var text = ItemNameTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                var newItem = new ShoppingItem { Name = text, IsPurchased = false };
                newItem.PropertyChanged += Item_PropertyChanged;
                _items.Add(newItem);
                ItemNameTextBox.Clear();
                SaveList();
                RefreshList();
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddItem();
        }

        private void ItemNameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddItem();
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ShoppingItem item)
            {
                item.PropertyChanged -= Item_PropertyChanged;
                _items.Remove(item);
                SaveList();
                RefreshList();
            }
        }

        private void ClearPurchasedButton_Click(object sender, RoutedEventArgs e)
        {
            var purchasedItems = _items.Where(i => i.IsPurchased).ToList();
            foreach (var item in purchasedItems)
            {
                item.PropertyChanged -= Item_PropertyChanged;
            }
            _items.RemoveAll(i => i.IsPurchased);
            SaveList();
            RefreshList();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            UpdateStatus();
        }

        private void RefreshList()
        {
            ShoppingListBox.ItemsSource = null;
            ShoppingListBox.ItemsSource = _items;
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            int total = _items.Count;
            int purchased = _items.Count(i => i.IsPurchased);
            StatusTextBlock.Text = $"{total} items ({purchased} purchased)";
        }
            private void CatalogItemNameTextBox_KeyDown(object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Enter)
                {
                    AddCatalogItem();
                }
            }

            private void AddCatalogItemButton_Click(object sender, RoutedEventArgs e)
            {
                AddCatalogItem();
            }

            private void UploadImageButton_Click(object sender, RoutedEventArgs e)
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "Image files (*.png;*.jpeg;*.jpg;*.bmp)|*.png;*.jpeg;*.jpg;*.bmp|All files (*.*)|*.*",
                    Title = "Select Item Image"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    _pendingImagePath = openFileDialog.FileName;
                    SelectedImagePathTextBlock.Text = Path.GetFileName(_pendingImagePath);
                }
            }

            private async void SearchImageButton_Click(object sender, RoutedEventArgs e)
            {
                var keyword = CatalogItemNameTextBox.Text.Trim();
                if (string.IsNullOrEmpty(keyword))
                {
                    MessageBox.Show("Please enter an item name first to search for an image.", "Search", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                SearchImageButton.IsEnabled = false;
                SelectedImagePathTextBlock.Text = "Searching internet...";

                // Setup header required by Wikipedia API
                if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
                {
                    _httpClient.DefaultRequestHeaders.Add("User-Agent", "ShoppingListApp/1.0");
                }

                try
                {
                    const int maxRetries = 3;
                    HttpResponseMessage response = null!;
                    for (int attempt = 0; attempt < maxRetries; attempt++)
                    {
                        string url = $"https://en.wikipedia.org/w/api.php?action=query&prop=pageimages&format=json&pithumbsize=200&titles={Uri.EscapeDataString(keyword)}";
                        response = await _httpClient.GetAsync(url);

                        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                        {
                            int delayMs = (int)(Math.Pow(2, attempt) * 1000); // 1s, 2s, 4s
                            SelectedImagePathTextBlock.Text = $"Rate limited — retrying in {delayMs / 1000}s ({attempt + 1}/{maxRetries})...";
                            await Task.Delay(delayMs);
                            continue;
                        }

                        response.EnsureSuccessStatusCode();
                        break; // Success or non-retryable error
                    }

                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                    var pages = doc.RootElement.GetProperty("query").GetProperty("pages");

                    string? imageUrl = null;
                    foreach (var page in pages.EnumerateObject())
                    {
                        if (page.Value.TryGetProperty("thumbnail", out JsonElement thumbnail))
                        {
                            imageUrl = thumbnail.GetProperty("source").GetString();
                            break; // Use the first found thumbnail
                        }
                    }

                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        byte[] imageBytes = await _httpClient.GetByteArrayAsync(imageUrl);

                        string ext = Path.GetExtension(imageUrl) ?? ".jpg";
                        if (string.IsNullOrEmpty(ext) || ext.Length > 5) ext = ".jpg";

                        string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ext);
                        await File.WriteAllBytesAsync(tempPath, imageBytes);

                        _pendingImagePath = tempPath;
                        SelectedImagePathTextBlock.Text = $"Downloaded: {Path.GetFileName(tempPath)}";
                    }
                    else
                    {
                        SelectedImagePathTextBlock.Text = "No image found.";
                        _pendingImagePath = null;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error searching image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    SelectedImagePathTextBlock.Text = "Search failed.";
                    _pendingImagePath = null;
                }
                finally
                {
                    SearchImageButton.IsEnabled = true;
                }
            }

            private void AddCatalogItem()
            {
                var text = CatalogItemNameTextBox.Text.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    string? finalImagePath = null;
                    if (!string.IsNullOrEmpty(_pendingImagePath) && File.Exists(_pendingImagePath))
                    {
                        try
                        {
                            string imagesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                            Directory.CreateDirectory(imagesDir);

                            string ext = Path.GetExtension(_pendingImagePath);
                            string newFileName = Guid.NewGuid().ToString() + ext;
                            finalImagePath = Path.Combine(imagesDir, newFileName);

                            File.Copy(_pendingImagePath, finalImagePath, true);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to save image: {ex.Message}", "Image Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }

                    var newItem = new HouseholdItem
                    {
                        Name = text,
                        Category = "Uncategorized", // Default category
                        ImagePath = finalImagePath
                    };
                    _catalog.Add(newItem);

                    // Reset UI
                    CatalogItemNameTextBox.Clear();
                    _pendingImagePath = null;
                    SelectedImagePathTextBlock.Text = "No image selected";

                    SaveCatalog();
                    RefreshCatalog();
                }
            }

            private void AddFromCatalogButton_Click(object sender, RoutedEventArgs e)
            {
                if (sender is Button button && button.Tag is HouseholdItem catItem)
                {
                    var newItem = new ShoppingItem
                    {
                        Name = catItem.Name,
                        IsPurchased = false,
                        CatalogItemId = catItem.Id
                    };
                    newItem.PropertyChanged += Item_PropertyChanged;
                    _items.Add(newItem);
                    SaveList();
                    RefreshList();

                    // Optionally switch to the shopping list tab to show it was added
                    // e.g. MyTabControl.SelectedIndex = 0; if we had x:Name="MyTabControl"
                }
            }

            private void RemoveCatalogItem_Click(object sender, RoutedEventArgs e)
            {
                if (sender is Button button && button.Tag is HouseholdItem catItem)
                {
                    // Delete associated image file if it exists
                    if (!string.IsNullOrEmpty(catItem.ImagePath) && File.Exists(catItem.ImagePath))
                    {
                        try
                        {
                            File.Delete(catItem.ImagePath);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to delete image: {ex.Message}", "Image Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }

                    _catalog.Remove(catItem);
                    SaveCatalog();
                    RefreshCatalog();
                }
            }
        }
    }