using System;

namespace Media.Application.Models
{
  public class Language
  {
    /// <summary>
    /// Id of language.
    /// </summary>
    public Guid LanguageId { get; set; }

    /// <summary>
    /// Name of language.
    /// </summary>
    public string Name { get; set; } = String.Empty;
  }
}
