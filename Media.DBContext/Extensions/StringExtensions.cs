using System.Text;

namespace Media.DBContext.Extensions;

public static class StringExtensions
{
  /// <summary>
  /// Converts the class name to snake case, so postgres criteria ios fullfilled
  /// </summary>
  /// <param name="str">string to be parsed</param>
  /// <returns>string in snake case</returns>
  public static string ToUnderscoreCase(this string str)
  {
    var sb = new StringBuilder();
    for (int i = 0; i < str.Length; i++)
    {
      var actChar = str[i];
      var nChar = i + 1 < str.Length ? str[i + 1] : ' ';
      if (char.IsUpper(nChar) && char.IsUpper(actChar))
      {
        sb.Append(actChar);
        continue;
      }

      if (char.IsLower(nChar) && char.IsUpper(actChar) && i > 0)
      {
        sb.Append('_');
        sb.Append(actChar);
        continue;
      }

      sb.Append(actChar);
    }

    return sb.ToString().ToLower();
  }
}
