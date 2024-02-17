using System;
using System.Collections.Generic;

namespace Media.API.Contracts
{
  public class ItemsRequest
  {
    /// <summary>
    /// Items to be requested.
    /// </summary>
    public List<Guid> Items { get; set; } = new List<Guid>();
  }
}
