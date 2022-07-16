using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Application.Models;

public class Artist
{
  /// <summary>
  /// Id of artist.
  /// </summary>
  public Guid ArtistId { get; set; }

  /// <summary>
  /// Name of artist.
  /// </summary>
  public string Name { get; set; } = String.Empty;
}
