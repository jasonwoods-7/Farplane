using System;
using System.Threading;
using System.Windows;
using Farplane.FFX.Values;
using Farplane.Memory;
using CheckBox = System.Windows.Controls.CheckBox;

namespace Farplane.FFX.EditorPanels.Boosters;

/// <summary>
/// Interaction logic for BoostersPanel.xaml
/// </summary>
public partial class BoostersPanel
{
    static readonly int _offsetInBattle = Offsets.GetOffset(OffsetType.PartyInBattleFlags);
    static readonly int _offsetGainedAp = Offsets.GetOffset(OffsetType.PartyGainedApFlags);
    readonly Thread _apThread;
    bool _sharedApEnabled;
    readonly byte[] _sharedApState = new byte[8];

    public BoostersPanel()
    {
        this.InitializeComponent();

        this._apThread = new Thread(this.SharedAPThread) { IsBackground = true };
        this._apThread.Start();

        for (var i = 0; i < 8; i++)
        {
            this.ShareBoxes.Children.Add(
                new CheckBox()
                {
                    Name = "CheckBoxAPShare" + i,
                    Content = (Character)i,
                    Margin = new Thickness(5),
                    IsChecked = i != 7,
                }
            );
        }
    }

    public void Refresh()
    {
        // No refresh logic for this panel
    }

    void GiveAllItems_Click(object sender, RoutedEventArgs e) => Cheats.GiveAllItems();

    void MaxAllStats_Click(object sender, RoutedEventArgs e) => Cheats.MaxAllStats();

    void MaxSphereLevels_Click(object sender, RoutedEventArgs e) => Cheats.MaxSphereLevels();

    void LearnAllAbilities_Click(object sender, RoutedEventArgs e) => Cheats.LearnAllAbilities();

    void SharedAPToggle_Click(object sender, RoutedEventArgs e)
    {
        this._sharedApEnabled = !this._sharedApEnabled;

        this.ButtonSharedAP.Content = this._sharedApEnabled ? "ENABLED" : "DISABLED";
    }

    void UpdateSharedAPState()
    {
        var gainedAp = GameMemory.Read<byte>(_offsetGainedAp, 8);

        for (var i = 0; i < 8; i++)
        {
            var box = (CheckBox)this.ShareBoxes.Children[i];
            this._sharedApState[i] = box.IsChecked.Value ? (byte)1 : gainedAp[i];
        }
    }

    void SharedAPThread()
    {
        while (true)
        {
            // Shared AP mod
            if (this._sharedApEnabled)
            {
                if (!GameMemory.IsAttached)
                {
                    break;
                }

                try
                {
                    this.Dispatcher.Invoke(this.UpdateSharedAPState);
                }
                catch (Exception)
                {
                    // App probably exited, silent exception
                }

                var writeBuffer = new byte[8];
                for (var i = 0; i < 8; i++)
                {
                    writeBuffer[i] = this._sharedApState[i];
                }

                GameMemory.Write(_offsetInBattle, writeBuffer);
                GameMemory.Write(_offsetGainedAp, writeBuffer);
            }

            Thread.Sleep(10);
        }
    }
}
