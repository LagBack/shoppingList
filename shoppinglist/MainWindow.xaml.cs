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
    public class ShoppingItem : INotifyPropertyChanged
    {
        private bool _isPurchased;
        private string _name = string.Empty;

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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    public partial class MainWindow : Window
    {
        private List<ShoppingItem> _items = new List<ShoppingItem>();
        private const string SaveFilePath = "shoppinglist.json";

        public MainWindow()
        {
            InitializeComponent();
            LoadList();
            RefreshList();
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
    }
}