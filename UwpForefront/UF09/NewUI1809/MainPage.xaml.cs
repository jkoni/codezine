﻿using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x411 を参照してください

namespace NewUI1809
{
  /// <summary>
  /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
  /// </summary>
  public sealed partial class MainPage : Page
  {
    private Command _menuCommand;
    private Command _dropDownButtonCommand;

    private readonly List<SolidColorBrush> _colorOptions = new List<SolidColorBrush>
    {
      new SolidColorBrush(Colors.Black),
      new SolidColorBrush(Colors.Red),
      new SolidColorBrush(Colors.Orange),
      new SolidColorBrush(Colors.Yellow),
      new SolidColorBrush(Colors.Green),
      new SolidColorBrush(Colors.Blue),
      new SolidColorBrush(Colors.Indigo),
      new SolidColorBrush(Colors.Violet),
      new SolidColorBrush(Colors.White),
    };



    public MainPage()
    {
      this.InitializeComponent();

      SetupCommand();
      SetupRichEditBox();

      LoadAozoraBunkoDataAsync();
    }

    private void SetupCommand()
    {
      // メニューバーのコマンド
      _menuCommand = new Command(
        async param =>
        {
          if (param is string s)
            await (new MessageDialog($"メニュー「{s}」が選択されました。",
                                     "Menu Sample")).ShowAsync();
        }, true);

      // ドロップダウンボタンのコマンド
      var alignments = new Dictionary<string, Windows.UI.Text.ParagraphAlignment> {
        {"Left", Windows.UI.Text.ParagraphAlignment.Left},
        {"Center", Windows.UI.Text.ParagraphAlignment.Center},
        {"Right", Windows.UI.Text.ParagraphAlignment.Right},
      };
      _dropDownButtonCommand = new Command(
        param =>
        {
          if (param is string s)
          {
            var selectedText = richEdit.Document.Selection;
            if (selectedText != null)
              selectedText.ParagraphFormat.Alignment = alignments[s];
          }
        }, true);
    }

    private void SetupRichEditBox()
    {
      const string SampleText
= @"　吾輩は猫である。名前はまだ無い。
　どこで生れたかとんと見当がつかぬ。何でも薄暗いじめじめした所でニャーニャー泣いていた事だけは記憶している。吾輩はここで始めて人間というものを見た。しかもあとで聞くとそれは書生という人間中で一番獰悪な種族であったそうだ。この書生というのは時々我々を捕えて煮て食うという話である。しかしその当時は何という考もなかったから別段恐しいとも思わなかった。ただ彼の掌に載せられてスーと持ち上げられた時何だかフワフワした感じがあったばかりである。掌の上で少し落ちついて書生の顔を見たのがいわゆる人間というものの見始であろう。この時妙なものだと思った感じが今でも残っている。第一毛をもって装飾されべきはずの顔がつるつるしてまるで薬缶だ。その後猫にもだいぶ逢ったがこんな片輪には一度も出会わした事がない。のみならず顔の真中があまりに突起している。そうしてその穴の中から時々ぷうぷうと煙を吹く。どうも咽せぽくて実に弱った。これが人間の飲む煙草というものである事はようやくこの頃知った。";

      this.richEdit.Document.SetText(Windows.UI.Text.TextSetOptions.None, SampleText);

      this.richEdit.Loaded += (s, e)
        => this.richEdit.ContextFlyout.Opening += ContextFlyout_Opening;
      this.richEdit.Unloaded += (s, e)
        => this.richEdit.ContextFlyout.Opening -= ContextFlyout_Opening;

      void ContextFlyout_Opening(object sender, object e)
      {
        var shareCommand = new StandardUICommand(StandardUICommandKind.Share);
        shareCommand.ExecuteRequested += async (cmd, args) =>
        {
          await (new MessageDialog($"コマンド「{cmd.Label}」が選択されました。",
                                   "StandardUICommand Sample")).ShowAsync();
        };
        var shareButton = new AppBarButton { Command = shareCommand, };
        (sender as CommandBarFlyout).PrimaryCommands.Add(shareButton);
      }
    }



    #region DataGrid
    private ObservableCollection<Data.AozoraBunko> _observableCollection;
    private AdvancedCollectionView _advancedCollectionView;

    private async void LoadAozoraBunkoDataAsync()
    {
      IEnumerable<Data.AozoraBunko> list 
        = (await NewUI1809.Data.AozoraBunko.GetListAsync())
          .Where(d => d.役割フラグ == "著者")
          .OrderBy(d => d.作品ID);
      _observableCollection = new ObservableCollection<Data.AozoraBunko>(list);
      _advancedCollectionView = new AdvancedCollectionView(_observableCollection, true);
      this.dataGrid.ItemsSource = _advancedCollectionView;
    }

    // テーブル：表示している列のTag→ソートに使うプロパティ
    private readonly Dictionary<string, string> _sortTargets
      = new Dictionary<string, string>
      {
        { "作品ID", "作品ID" },
        { "作品名", "作品名読み" },
        { "文字遣い種別", "文字遣い種別" },
        { "姓名", "姓名読み" },
      };

    private async void DataGrid_Sorting(object sender, DataGridColumnEventArgs e)
    {
      try
      {
        // プログレスリングを出す
        this.dataGrid.IsEnabled = false;
        this.dataGridProgress.IsActive = true;
        await Task.Delay(1);

        // ソートに使うプロパティ名（例：［作品名］をクリックされたら［作品名読み］でソート）
        string sortTarget = _sortTargets[e.Column.Tag as string];

        // クリックされた列の、現在のソート方向を取得する
        var currentSortDirection = e.Column.SortDirection;

        // 全ての列ヘッダーのソートマーク(↓↑)を消す
        foreach (var c in this.dataGrid.Columns)
          c.SortDirection = null;

        // 以前のSortDescriptionオブジェクトを削除する（もしあれば）
        var oldSort = _advancedCollectionView.SortDescriptions
                        .FirstOrDefault(s => s.PropertyName == sortTarget);
        if (oldSort != null)
          _advancedCollectionView.SortDescriptions.Remove(oldSort);

        if (currentSortDirection == null || currentSortDirection == DataGridSortDirection.Descending)
        {
          // 昇順にソート（新しいSortDescriptionオブジェクトを先頭に挿入する）
          _advancedCollectionView.SortDescriptions
            .Insert(0, new SortDescription(sortTarget, SortDirection.Ascending));

          // 列ヘッダーにソートマーク「↑」を付ける
          e.Column.SortDirection = DataGridSortDirection.Ascending;
        }
        else
        {
          // 降順にソート（新しいSortDescriptionオブジェクトを先頭に挿入する）
          _advancedCollectionView.SortDescriptions
            .Insert(0, new SortDescription(sortTarget, SortDirection.Descending));

          // 列ヘッダーにソートマーク「↓」を付ける
          e.Column.SortDirection = DataGridSortDirection.Descending;
        }
      }
      finally
      {
        // プログレスリングを消す
        this.dataGrid.IsEnabled = true;
        this.dataGridProgress.IsActive = false;
      }
    }
    #endregion



    #region スプリットボタン

    private SolidColorBrush _currentColorBrush;

    private void SplitButtonClick(SplitButton sender, SplitButtonClickEventArgs args)
      => ChangeColor();

    private void SplitButtonSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      _currentColorBrush = (SolidColorBrush)e.AddedItems[0];
      SelectedColorBorder.Background = _currentColorBrush;
      ChangeColor();
      BrushFlyout.Hide();
    }

    private void ChangeColor()
    {
      // Apply the color to the selected text in a RichEditBox.
      Windows.UI.Text.ITextSelection selectedText = richEdit.Document.Selection;
      if (selectedText != null)
      {
        Windows.UI.Text.ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
        charFormatting.ForegroundColor = _currentColorBrush.Color;
        selectedText.CharacterFormat = charFormatting;
      }
    }
    #endregion



    #region トグルスプリットボタン

    private void ToggleSplitButtonListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (ListButton.IsChecked)
      {
        var listStyle = ((FrameworkElement)(e.AddedItems[0])).Tag.ToString();
        // Toggle button is on. Turn it off...
        if (listStyle == "none")
          ListButton.IsChecked = false;
        else
          // or apply the new selection.
          ApplyListStyle(listStyle);
      }
      else
        // Toggle button is off. Turn it on, which will apply the selection
        // in the IsCheckedChanged event handler.
        ListButton.IsChecked = true;
    }

    private void ToggleSplitButton_IsCheckedChanged(ToggleSplitButton sender, ToggleSplitButtonIsCheckedChangedEventArgs args)
    {
      // Use the toggle button to turn the selected list style on or off.
      if (((ToggleSplitButton)sender).IsChecked)
      {
        // On. Apply the list style selected in the drop down to the selected text.
        var listStyle = ((FrameworkElement)(ListStylesListView.SelectedItem))?.Tag.ToString();
        ApplyListStyle(listStyle);
      }
      else
      {
        // Off. Make the selected text not a list,
        // but don't change the list style selected in the drop down.
        ApplyListStyle("none");
      }
    }

    private readonly Dictionary<string, Windows.UI.Text.MarkerType> _listStyles
      = new Dictionary<string, Windows.UI.Text.MarkerType>
          {
            {"none", Windows.UI.Text.MarkerType.None},
            {"bullet", Windows.UI.Text.MarkerType.Bullet},
            {"numeric", Windows.UI.Text.MarkerType.Arabic},
            {"alpha", Windows.UI.Text.MarkerType.UppercaseEnglishLetter},
          };
    private void ApplyListStyle(string listStyle)
    {
      Windows.UI.Text.ITextSelection selectedText = richEdit.Document.Selection;
      if (selectedText != null)
        selectedText.ParagraphFormat.ListType = _listStyles[listStyle??"none"];
    }
    #endregion

  }
}
