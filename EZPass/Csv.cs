using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace ZStewart.EZPass {
  /// <summary>
  ///  Contains the relevant fields of the EZ-Pass transaction record.
  /// </summary>
  public class TollRecord {
    /// <summary>
    /// Text of the Posting Date.
    /// </summary>
    [Name("POSTING DATE")]
    public string PostingDate { get; set; }

    /// <summary>
    /// Text of the Transaction Date.
    /// </summary>
    [Name("TRANSACTION DATE")]
    public string TransactionDate { get; set; }

    /// <summary>
    /// Tag ID of the EZPass tag that was used for this toll, or - for account
    /// actions like payments.
    /// </summary>
    [Name("TAG/PLATE NUMBER")]
    public string Tag { get; set; }

    /// <summary>
    /// Text of the amount column.
    /// </summary>
    [Name("AMOUNT")]
    public string Amount { get; set; }
  }

  /// <summary>
  /// Processed version of a TollRecord with values converted to application-usable data.
  /// </summary>
  public class ProcessedTollRecord {
    /// <summary>
    /// Text of the Posting Date.
    /// </summary>
    [Name("Posting Date")]
    public string PostingDate { get; set; }

    /// <summary>
    /// Text of the Transaction Date.
    /// </summary>
    [Name("Transaction Date")]
    public string TransactionDate { get; set; }

    /// <summary>
    /// Tag ID of the EZPass tag that was used for this toll, or - for account
    /// actions like payments.
    /// </summary>
    [Name("Tag or Plate Number")]
    public string Tag { get; set; }

    /// <summary>
    /// Owner of the vehicle that was used for this toll.
    /// </summary>
    [Name("Owner")]
    public string Owner { get; set; }

    /// <summary>
    /// Numeric amount.
    /// </summary>
    [Name("Amount")]
    public decimal Amount { get; set; }
  }

  /// <summary>
  /// Processed version of a TollRecord with values converted to application-usable data.
  /// </summary>
  public class TollTotalRecord {
    public TollTotalRecord(string owner) {
      Owner = owner;
    }

    /// <summary>
    /// Tag ID of the EZPass tag that was used for this toll, or - for account
    /// actions like payments.
    /// </summary>
    public SortedSet<string> Tags { get; } = new SortedSet<string>();

    /// <summary>
    /// Owner of the vehicle that was used for this toll.
    /// </summary>
    public string Owner { get; }

    /// <summary>
    /// Total amount.
    /// </summary>
    public decimal Amount { get; set; }
  }

  [ValueConversion(typeof(SortedSet<string>), typeof(string))]
  public class SetConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      var set = (SortedSet<string>)value;
      return string.Join(", ", set);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
      return new SortedSet<string>();
      throw new InvalidOperationException("Does not support converting back");
    }
  }

  /// <summary>
  /// Utility for parsing the annoying dolar decimal format.
  /// </summary>
  public static class DollarDecimalParser {
    /// <summary>
    /// Matches $1.0 format.
    /// </summary>
    private static Regex PositiveDolarDecimal { get; } = new Regex(@"^\s*\$\s*(?<num>\d+(?:\.\d+)?)\s*$", RegexOptions.Compiled);
    /// <summary>
    /// Matches ($1.0) format.
    /// </summary>
    private static Regex NegativeDolarDecimal { get; } = new Regex(@"^\s*\(\s*\$\s*(?<num>\d+(?:\.\d+)?)\s*\)\s*$", RegexOptions.Compiled);

    /// <summary>
    /// Parse a decimal preceeded by a dollar sign and optionally surrouned by parenthesis.
    /// </summary>
    /// <param name="val">A string in the form $1.0 or ($1.0)</param>
    /// <returns>A decimal, negative if the value was parenthesized.</returns>
    public static decimal Parse(string val) {
      var positive = PositiveDolarDecimal.Match(val);
      if (positive.Success) {
        var numText = positive.Groups["num"].Value;
        return decimal.Parse(numText);
      }
      var negative = NegativeDolarDecimal.Match(val);
      if (negative.Success) {
        var numText = negative.Groups["num"].Value;
        return -decimal.Parse(numText);
      }
      throw new FormatException($"Expected either ($1.00) or $1.00 format got {val}.");
    }
  }
}
