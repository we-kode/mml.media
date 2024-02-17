using System.Collections.Generic;

namespace Media.API.Contracts
{
  public class FolderItemsRequest
  {
    /// <summary>
    /// Items to be requested.
    /// </summary>
    public List<RecordFolder> Items { get; set; } = new List<RecordFolder>();
  }
}
