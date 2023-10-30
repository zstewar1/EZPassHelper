using System.Windows;
using System.IO;
using CsvHelper;
using System.Globalization;
using System;
using System.Text.Json;
using System.Collections.Generic;
using CsvHelper.Configuration;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace ZStewart.EZPass {
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window {
    private static JsonSerializerOptions ConfigSerializationOptions { get; } = new JsonSerializerOptions { WriteIndented = true };
    private string configFile = null;
    private AppConfig config = new AppConfig();

    private string filePath = null;
    private readonly List<TollRecord> rawRecords = new List<TollRecord>();
    private readonly List<ProcessedTollRecord> records = new List<ProcessedTollRecord>();
    public ObservableCollection<TollTotalRecord> TollTotals { get; } = new ObservableCollection<TollTotalRecord>();

    public MainWindow() {
      InitializeComponent();
      DataContext = this;
    }

    /// <summary>
    /// Sets the path to the CSV file being used, loads it and updates the displayed output.
    /// </summary>
    /// <param name="path">Path to the CSV file to use. Must be non-null.</param>
    private void SetFilePath(string path) {
      filePath = path;
      selectedFileNameText.Text = Path.GetFileName(filePath);
    }

    /// <summary>
    /// Process the file selected by filePath.
    /// </summary>
    private void ProcessFile() {
      ClearResults();
      try {
        LoadRawRecords();
      } catch (FileNotFoundException) {
        MessageBox.Show($"File {filePath} not found.", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }
      ProcessRawRecords();
    }


    /// <summary>
    /// Load the raw records from the file specified by filePath.
    /// </summary>
    private void LoadRawRecords() {
      rawRecords.Clear();
      using (var reader = new StreamReader(filePath))
      using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture)) {
        foreach (var record in csv.GetRecords<TollRecord>()) {
          rawRecords.Add(record);
        }
      }
    }

    /// <summary>
    /// Convert the raw records into processed records.
    /// </summary>
    private void ProcessRawRecords() {
      records.Clear();
      for (int i = 0; i < rawRecords.Count; i++) {
        var raw = rawRecords[i];
        int entry = i + 1;

        var record = new ProcessedTollRecord {
          PostingDate = raw.PostingDate,
          TransactionDate = raw.TransactionDate,
          Tag = raw.Tag,
          Owner = LookupTag(raw.Tag),
          Amount = 0.0m,
        };
        try {
          record.Amount = DollarDecimalParser.Parse(raw.Amount);
        } catch (FormatException) {
          MessageBox.Show($"Did not recognize the format of the AMOUNT value for record {entry} which had value {raw.Amount}.", "Unrecognized Format", MessageBoxButton.OK, MessageBoxImage.Warning);
          records.Clear();
          break;
        }
        records.Add(record);
      }

      var grouping = new SortedDictionary<string, TollTotalRecord>();
      foreach (var record in records) {
        TollTotalRecord group;
        if (!grouping.TryGetValue(record.Owner, out group)) {
          group = new TollTotalRecord(record.Owner);
          grouping[record.Owner] = group;
        }
        group.Amount += record.Amount;
        group.Tags.Add(record.Tag);
      }

      TollTotals.Clear();
      foreach (var group in grouping.Values) {
        TollTotals.Add(group);
      }
    }

    /// <summary>
    /// Clear the specified file and the processed results.
    /// </summary>
    private void ClearFile() {
      filePath = null;
      selectedFileNameText.Text = "None Selected";
      ClearResults();
    }

    /// <summary>
    /// Clear the results and results panel.
    /// </summary>
    private void ClearResults() {
      rawRecords.Clear();
      records.Clear();
      TollTotals.Clear();
    }

    /// <summary>
    /// Gets the associated owner for a particular tag.
    /// </summary>
    /// <param name="tag">Tag to look up.</param>
    /// <returns>The associated owner if any, otherwise returns the tag.</returns>
    private string LookupTag(string tag) {
      if (config.TagOwners.TryGetValue(tag, out var owner)) {
        return owner;
      }
      return tag;
    }

    private void ChooseCsvBtn_Click(object sender, RoutedEventArgs e) {
      var fileDialog = new Microsoft.Win32.OpenFileDialog {
        Title = "Choose CSV File",
        DefaultExt = ".csv",
        Filter = "CSV Files (.csv)|*.csv",
        CheckFileExists = true,
        InitialDirectory = Directory.GetCurrentDirectory(),
        Multiselect = false,
      };
      bool? result = fileDialog.ShowDialog(GetWindow(this));
      if (result == true) {
        SetFilePath(fileDialog.FileName);
        ProcessFile();
      }
    }

    private void Window_Initialized(object sender, EventArgs e) {
      string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
      string ezpassData = Path.Combine(appData, "EZPassProcessor");
      Directory.CreateDirectory(ezpassData);
      configFile = Path.Combine(ezpassData, "config.json");
      LoadConfig();
    }

    /// <summary>
    /// Load config data from the config file.
    /// </summary>
    private void LoadConfig() {
      string configText;
      try {
        configText = File.ReadAllText(configFile);
      } catch (FileNotFoundException) {
        SaveConfig();
        return;
      }
      config = JsonSerializer.Deserialize<AppConfig>(configText);
    }

    /// <summary>
    /// Stores the current config file. Assumes that the directory already
    /// exists, and that the configFile variable has already been set.
    /// </summary>
    private void SaveConfig() {
      string content = JsonSerializer.Serialize(config, ConfigSerializationOptions);
      File.WriteAllText(configFile, content);
    }

    private void ExportProcessedCsvBtn_Click(object sender, RoutedEventArgs e) {
      if (filePath == null) {
        MessageBox.Show("No source CSV file was selected.", "No File Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }
      if (records.Count != rawRecords.Count) {
        MessageBox.Show("Converting raw records failed.", "Processing Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }
      var filename = Path.GetFileNameWithoutExtension(filePath);
      var dir = Path.GetDirectoryName(filePath);
      var fileDialog = new Microsoft.Win32.SaveFileDialog {
        Title = "Choose Destination File",
        DefaultExt = ".csv",
        Filter = "CSV Files (.csv)|*.csv",
        FileName = Path.Combine(dir, $"{filename}-processed.csv"),
      };
      bool? result = fileDialog.ShowDialog(GetWindow(this));
      if (result == true) {
        using (var writer = new StreamWriter(fileDialog.FileName))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture)) {
          csv.WriteRecords(records);
        }
      }
    }

    private void EditTagsBtn_Click(object sender, RoutedEventArgs e) {
      Process.Start(configFile);
    }

    private void ReloadTagsBtn_Click(object sender, RoutedEventArgs e) {
      LoadConfig();
      ProcessRawRecords();
    }
  }
}
