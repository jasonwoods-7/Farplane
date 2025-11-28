using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Farplane.Common;
using Farplane.Common.Controls;
using Farplane.FFX.Data;
using Farplane.FFX.Values;
using Farplane.Memory;
using MahApps.Metro;

namespace Farplane.FFX.EditorPanels.ItemsPanel;

/// <summary>
/// Interaction logic for ItemsPanel.xaml
/// </summary>
public partial class ItemsPanel : UserControl
{
    readonly int _offsetKeyItem = OffsetScanner.GetOffset(GameOffset.FFX_KeyItems);
    readonly int _offsetAlBhed = OffsetScanner.GetOffset(GameOffset.FFX_AlBhed);

    readonly ButtonGrid _itemButtons = new(2, 112);
    readonly ButtonGrid _keyItemButtons = new(2, KeyItem.KeyItems.Length - 1);

    static readonly ComboBox ComboItemList = new()
    {
        ItemsSource = Item.Items.Select(item => item.Name),
    };
    static readonly TextBox TextItemCount = new();
    static readonly StackPanel PanelEditItem = new()
    {
        Orientation = Orientation.Horizontal,
        Children = { ComboItemList, TextItemCount },
    };

    bool _refreshing = false;

    Item[] _currentItems;
    int _editingItem = -1;

    bool[] _keyItemState;
    bool[] _alBhedState;

    static readonly Tuple<AppTheme, Accent> currentStyle = ThemeManager.DetectAppStyle(
        Application.Current
    );
    readonly Brush _trueKeyItemBrush = new SolidColorBrush(
        (Color)currentStyle.Item1.Resources["BlackColor"]
    );
    readonly Brush _falseKeyItemBrush = new SolidColorBrush(
        (Color)currentStyle.Item1.Resources["Gray2"]
    );

    public ItemsPanel()
    {
        this.InitializeComponent();
        this.TabItems.Content = this._itemButtons;
        this.ContentKeyItems.Content = this._keyItemButtons;

        this._itemButtons.ButtonClicked += this.ItemButtonsOnButtonClicked;

        ComboItemList.KeyDown += this.ItemEditor_KeyDown;
        TextItemCount.KeyDown += this.ItemEditor_KeyDown;

        this._keyItemButtons.ButtonClicked += this.KeyItemButtonsOnButtonClicked;
    }

    void KeyItemButtonsOnButtonClicked(int buttonIndex)
    {
        var keyItemData = GameMemory.Read<byte>(this._offsetKeyItem, 8, false);
        var bitIndex = KeyItem.KeyItems[buttonIndex].BitIndex;
        var keyByteIndex = bitIndex / 8;
        var keyBitIndex = bitIndex % 8;

        keyItemData[keyByteIndex] = BitHelper.ToggleBit(keyItemData[keyByteIndex], keyBitIndex);
        GameMemory.Write(this._offsetKeyItem, keyItemData, false);
        this.Refresh();
    }

    void ItemButtonsOnButtonClicked(int buttonIndex)
    {
        if (this._editingItem == buttonIndex)
        {
            return;
        }

        this.Refresh();

        var clickedItem = this._currentItems[buttonIndex];
        var baseItem = Item.Items.First(item => item.ID == clickedItem.ID);

        var itemIndex = Item.Items.ToList().IndexOf(baseItem);

        ComboItemList.SelectedIndex = itemIndex;
        ComboItemList.KeyDown += this.ItemEditor_KeyDown;

        TextItemCount.Text = clickedItem.Count.ToString();

        this._itemButtons.SetContent(buttonIndex, PanelEditItem);
        this._editingItem = buttonIndex;

        TextItemCount.SelectionStart = 0;
        TextItemCount.SelectionLength = TextItemCount.Text.Length;

        TextItemCount.Focus();
    }

    void ItemEditor_KeyDown(object sender, KeyEventArgs keyEventArgs)
    {
        if (keyEventArgs.Key is not Key.Enter and not Key.Escape)
        {
            return;
        }

        switch (keyEventArgs.Key)
        {
            case Key.Enter:
                var itemIndex = ComboItemList.SelectedIndex;
                var itemCount = byte.Parse(TextItemCount.Text);
                if (itemCount == 0)
                {
                    itemIndex = 0;
                }

                if (itemIndex == 0)
                {
                    itemCount = 0;
                }

                Item.WriteItem(this._editingItem, Item.Items[itemIndex].ID, itemCount);
                this.Refresh();
                break;
            case Key.Escape:
                this.Refresh();
                break;
        }
    }

    public void Refresh()
    {
        this._refreshing = true;
        this._editingItem = -1;
        this._currentItems = Item.ReadItems();

        // Refresh inventory items
        for (var i = 0; i < this._currentItems.Length; i++)
        {
            if (this._currentItems[i].ID == 0xFF)
            {
                // Empty slot
                this._itemButtons.SetContent(i, "< EMPTY >");
            }
            else
            {
                // Show item name and count
                this._itemButtons.SetContent(
                    i,
                    this._currentItems[i].Name + " x" + this._currentItems[i].Count
                );
            }
        }

        // Refresh key items and al bhed dictionaries
        var keyItemData = GameMemory.Read<byte>(this._offsetKeyItem, 8, false);
        var alBhedData = GameMemory.Read<byte>(this._offsetAlBhed, 4, false);
        this._keyItemState = BitHelper.GetBitArray(keyItemData, 58);
        this._alBhedState = BitHelper.GetBitArray(alBhedData, 26);

        // Key Items
        for (var i = 0; i < KeyItem.KeyItems.Length - 1; i++)
        {
            if (this._keyItemState[KeyItem.KeyItems[i].BitIndex])
            {
                // Key item owned
                this._keyItemButtons.Buttons[i].Foreground = this._trueKeyItemBrush;
                this._keyItemButtons.SetContent(i, $"{KeyItem.KeyItems[i].Name}");
            }
            else
            {
                // Key item not owned
                this._keyItemButtons.Buttons[i].Foreground = this._falseKeyItemBrush;
                this._keyItemButtons.SetContent(i, $"{KeyItem.KeyItems[i].Name}");
            }
        }

        // Al Bhed Dictionaries
        for (var i = 0; i < 26; i++)
        {
            (this.PanelAlBhed.Children[i] as CheckBox).IsChecked = this._alBhedState[i];
        }
        this._refreshing = false;
    }

    void AlBhedDictionary_CheckedChanged(object sender, RoutedEventArgs e)
    {
        if (this._refreshing)
        {
            return;
        }

        var checkBox = sender as CheckBox;
        var alBhedData = GameMemory.Read<byte>(this._offsetAlBhed, 4, false);

        var boxIndex = this.PanelAlBhed.Children.IndexOf(checkBox);

        var byteIndex = boxIndex / 8;
        var bitIndex = boxIndex % 8;

        alBhedData[byteIndex] = BitHelper.ToggleBit(alBhedData[byteIndex], bitIndex);
        GameMemory.Write(this._offsetAlBhed, alBhedData, false);
        this.Refresh();
    }
}
