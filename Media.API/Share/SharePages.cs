using Media.Application.Models.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.Json;

namespace Media.API.Share;

/// <summary>
/// Creates the public pages of share links. The pages are the same for all links and contain no titles of records. Title and colors
/// are those of the app, see <see cref="ShareStyling"/>.
/// </summary>
/// <param name="styling">Title and colors of the pages.</param>
public class SharePages(ShareStyling styling)
{
  private static readonly Dictionary<string, string[]> Texts = new()
  {
    ["downloadTitle"] = ["Your download is ready", "Ihr Download ist bereit", "Ваша загрузка готова"],
    ["downloadCount"] = ["Recordings: {n}", "Aufnahmen: {n}", "Записей: {n}"],
    ["downloadText"] = ["A single recording is downloaded as audio file, several recordings as ZIP archive.", "Eine einzelne Aufnahme wird als Audiodatei geladen, mehrere Aufnahmen als ZIP-Archiv.", "Одна запись скачивается как аудиофайл, несколько записей как ZIP-архив."],
    ["downloadSingleUse"] = ["This is a single-use link. It becomes invalid after the download.", "Das ist ein Einmal-Link. Er wird nach dem Download ungültig.", "Это одноразовая ссылка. После загрузки она станет недействительной."],
    ["downloadMultiUse"] = ["The link can be used several times until it expires.", "Der Link kann bis zum Ablauf mehrfach verwendet werden.", "Ссылку можно использовать несколько раз до истечения срока."],
    ["downloadValidUntil"] = ["Valid until {date} ({left})", "Gültig bis {date} ({left})", "Действительна до {date} ({left})"],
    ["download"] = ["Download", "Herunterladen", "Скачать"],
    ["invalidTitle"] = ["This link is no longer valid", "Dieser Link ist nicht mehr gültig", "Эта ссылка больше недействительна"],
    ["invalidText"] = ["The link has expired or was already used. Please ask the sender for a new link.", "Der Link ist abgelaufen oder wurde bereits verwendet. Bitten Sie den Absender um einen neuen Link.", "Срок действия ссылки истёк или она уже использована. Попросите отправителя прислать новую ссылку."],
    ["openTitle"] = ["Opening {app}", "{app} wird geöffnet", "Открывается {app}"],
    ["openText"] = ["If the app does not open, press the button. The app must be installed and set up for this server.", "Falls sich die App nicht öffnet, tippen Sie auf die Schaltfläche. Die App muss installiert und für diesen Server eingerichtet sein.", "Если приложение не открылось, нажмите кнопку. Приложение должно быть установлено и настроено для этого сервера."],
    ["open"] = ["Open app", "App öffnen", "Открыть приложение"],
  };

  private const string Page = """
    <!doctype html>
    <html lang="en">
    <head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width,initial-scale=1">
    <meta name="referrer" content="no-referrer">
    <meta name="robots" content="noindex,nofollow">
    <meta name="color-scheme" content="light dark">
    <title>@@APP@@</title>
    <style>
    @@COLORS@@
    *{box-sizing:border-box}
    body{margin:0;min-height:100vh;display:flex;align-items:center;justify-content:center;padding:24px;font-family:Roboto,system-ui,-apple-system,"Segoe UI",sans-serif;color:var(--on-surface);background:var(--surface)}
    main{width:100%;max-width:440px;padding:40px 32px 28px;text-align:center;background:var(--surface-high);border-radius:28px}
    .eq{display:flex;align-items:flex-end;justify-content:center;gap:6px;height:56px;margin:0 auto 24px}
    .eq i{width:10px;border-radius:5px;background:var(--primary);animation:eq 1.1s ease-in-out infinite}
    .eq i:nth-child(1){height:28px;animation-delay:-.9s}.eq i:nth-child(2){height:52px;animation-delay:-.6s}.eq i:nth-child(3){height:38px;animation-delay:-.3s}.eq i:nth-child(4){height:48px;animation-delay:-.75s}.eq i:nth-child(5){height:24px;animation-delay:-.15s}
    .eq.still i{animation:none}
    @keyframes eq{0%,100%{transform:scaleY(.35)}50%{transform:scaleY(1)}}
    @media (prefers-reduced-motion:reduce){.eq i{animation:none}}
    h1{margin:0 0 12px;font-size:1.6rem;font-weight:500;line-height:1.25}
    p{margin:0 0 14px;color:var(--on-surface-variant);line-height:1.5}
    .count{display:inline-block;margin:4px 0 16px;padding:6px 16px;border-radius:999px;font-weight:500;color:var(--on-primary-container);background:var(--primary-container)}
    a.button,button{display:inline-block;margin:10px 0 18px;padding:14px 36px;border:0;border-radius:999px;font:inherit;font-weight:500;color:var(--on-primary);text-decoration:none;cursor:pointer;background:var(--primary);transition:opacity .15s}
    a.button:hover,button:hover{opacity:.9}
    a.button:focus-visible,button:focus-visible{outline:3px solid var(--outline);outline-offset:3px}
    footer{margin-top:18px;font-size:.8rem;letter-spacing:.08em;text-transform:uppercase;color:var(--outline)}
    </style>
    </head>
    <body>
    <main>
    <div class="eq@@STILL@@" aria-hidden="true"><i></i><i></i><i></i><i></i><i></i></div>
    @@BODY@@
    <footer id="app">@@APP@@</footer>
    </main>
    <script>
    var T=@@TEXTS@@;
    var A=@@TITLES@@;
    var lang=((navigator.language||"en").slice(0,2));
    if(!T[lang]){lang="en";}
    var app=A[lang]||A.Default;
    document.documentElement.lang=lang;
    document.title=app;
    document.getElementById("app").textContent=app;
    function expiry(t,iso,l){
      var d=new Date(iso),ms=d-new Date(),m=Math.round(ms/60000),u="minute",v=m;
      if(Math.abs(m)>=1440){u="day";v=Math.round(m/1440);}else if(Math.abs(m)>=60){u="hour";v=Math.round(m/60);}
      var left=new Intl.RelativeTimeFormat(l,{numeric:"auto"}).format(v,u);
      return t.replace("{date}",d.toLocaleString(l,{dateStyle:"medium",timeStyle:"short"})).replace("{left}",left);
    }
    document.querySelectorAll("[data-i]").forEach(function(e){
      var t=T[lang][e.dataset.i]||T.en[e.dataset.i];
      if(e.dataset.exp){t=expiry(t,e.dataset.exp,lang);}
      e.textContent=t.replace("{n}",e.dataset.n||"").replace("{app}",app);
    });
    @@SCRIPT@@
    </script>
    </body>
    </html>
    """;

  /// <summary>
  /// Creates the page, which starts the download of the records of a link.
  /// </summary>
  /// <param name="count">Number of the records of the link.</param>
  /// <param name="singleUse">True, if the link can be used only once.</param>
  /// <param name="expiresAt">Date when the link expires.</param>
  /// <returns>The html of the page.</returns>
  public string Download(int count, bool singleUse, DateTime expiresAt)
  {
    return Create($"""
      {Text("downloadTitle", "h1")}
      {Text("downloadText", "p")}
      <span class="count" data-i="downloadCount" data-n="{count}">{Texts["downloadCount"][0].Replace("{n}", count.ToString())}</span>
      <p class="validity" data-i="downloadValidUntil" data-exp="{Expiry(expiresAt)}"></p>
      {Text(singleUse ? "downloadSingleUse" : "downloadMultiUse", "p", "validity")}
      <form method="post"><button type="submit" data-i="download">{Texts["download"][0]}</button></form>
      """, string.Empty, false);
  }

  /// <summary>
  /// Creates the page for links which are expired, used or unknown.
  /// </summary>
  /// <returns>The html of the page.</returns>
  public string Invalid()
  {
    return Create($"""
      {Text("invalidTitle", "h1")}
      {Text("invalidText", "p")}
      """, string.Empty, true);
  }

  /// <summary>
  /// Creates the page, which opens the app with the search or record of a link.
  /// </summary>
  /// <returns>The html of the page.</returns>
  public string Open()
  {
    return Create($"""
      {Text("openTitle", "h1")}
      {Text("openText", "p")}
      <a class="button" id="open" href="#" data-i="open">{Texts["open"][0]}</a>
      """, """
      var q=location.search?location.search+"&":"?";
      var u="mml://open"+q+"server="+encodeURIComponent(location.host);
      document.getElementById("open").href=u;
      location.href=u;
      """, false);
  }

  private static string Expiry(DateTime expiresAt)
  {
    var utc = expiresAt.Kind == DateTimeKind.Local ? expiresAt.ToUniversalTime() : DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc);
    return utc.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
  }

  private string Text(string key, string tag, string cssClass = "")
  {
    var attribute = cssClass.Length > 0 ? $" class=\"{cssClass}\"" : string.Empty;
    return $"<{tag}{attribute} data-i=\"{key}\">{WebUtility.HtmlEncode(Texts[key][0].Replace("{app}", styling.Title["Default"]))}</{tag}>";
  }

  private string Create(string body, string script, bool still)
  {
    var languages = new[] { "en", "de", "ru" };
    var texts = languages
      .Select((language, index) => (language, texts: Texts.ToDictionary(t => t.Key, t => t.Value[index])))
      .ToDictionary(l => l.language, l => l.texts);

    return Page
      .Replace("@@COLORS@@", Colors())
      .Replace("@@STILL@@", still ? " still" : string.Empty)
      .Replace("@@BODY@@", body)
      .Replace("@@SCRIPT@@", script)
      .Replace("@@TEXTS@@", Json(texts))
      .Replace("@@TITLES@@", Json(styling.Title))
      .Replace("@@APP@@", WebUtility.HtmlEncode(styling.Title["Default"]));
  }

  private static string Json(object value)
  {
    return JsonSerializer.Serialize(value).Replace("<", "\\u003c");
  }

  private string Colors()
  {
    return $":root{{{Variables(styling.Light)}}}@media (prefers-color-scheme:dark){{:root{{{Variables(styling.Dark)}}}}}";
  }

  private static string Variables(ShareColors colors)
  {
    return $"--surface:{colors.Surface};--on-surface:{colors.OnSurface};--surface-high:{colors.SurfaceContainerHighest};"
      + $"--on-surface-variant:{colors.OnSurfaceVariant};--primary:{colors.Primary};--on-primary:{colors.OnPrimary};"
      + $"--primary-container:{colors.PrimaryContainer};--on-primary-container:{colors.OnPrimaryContainer};--outline:{colors.Outline};";
  }
}
