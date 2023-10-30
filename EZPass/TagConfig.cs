using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ZStewart.EZPass {
  /// <summary>
  /// Contains a list of tag/plate to owner mappings.
  /// </summary>
  public class AppConfig {
    /// <summary>
    /// Mapping from tags to their owners.
    /// </summary>
    [JsonRequired]
    public Dictionary<string, string> TagOwners { get; set; } = new Dictionary<string, string>();
  }
}
