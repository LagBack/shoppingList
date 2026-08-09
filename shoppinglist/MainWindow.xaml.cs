using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
        private List<ShoppingItem> _items = new List<ShoppingItem>();
        private List<HouseholdItem> _catalog = new List<HouseholdItem>();

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

            private void AddCatalogItem()
            {
                var text = CatalogItemNameTextBox.Text.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    var newItem = new HouseholdItem
                    {
                        Name = text,
                        Category = "Uncategorized" // Default category
                    };
                    _catalog.Add(newItem);
                    CatalogItemNameTextBox.Clear();
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
        }
    }