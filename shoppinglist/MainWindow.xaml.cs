using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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

        public MainWindow()
        {
            InitializeComponent();
            RefreshList();
        }

        private void AddItem()
        {
            var text = ItemNameTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                _items.Add(new ShoppingItem { Name = text, IsPurchased = false });
                ItemNameTextBox.Clear();
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
                _items.Remove(item);
                RefreshList();
            }
        }

        private void ClearPurchasedButton_Click(object sender, RoutedEventArgs e)
        {
            _items.RemoveAll(i => i.IsPurchased);
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