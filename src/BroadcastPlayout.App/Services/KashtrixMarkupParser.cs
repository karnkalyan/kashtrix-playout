using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using BroadcastPlayout.Models;

namespace BroadcastPlayout.Services;

/// <summary>
/// Parses dynamic Kashtrix custom markup tags (&lt;kcg...&gt;, &lt;kcg-${var}&gt;, &lt;kcg-bgcolor="..." font="..."&gt;)
/// from text files, Excel cells, XML, JSON feeds, or user-authored layer text, extracting
/// formatting overrides (font, bgcolor, color, size) and dynamic variables while producing clean broadcast text.
/// </summary>
public static class KashtrixMarkupParser
{
    private static readonly Regex TagRegex = new(@"<kcg(?:-([a-zA-Z0-9_\-]+))?([^>]*?)(?:/>|>(.*?)</kcg(?:-[a-zA-Z0-9_\-]+)?>)", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex AttrRegex = new(@"([a-zA-Z0-9_\-]+)\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public readonly record struct MarkupRun(string Text, string? TextColor, string? BackgroundColor, string? ImagePath = null, string? FallbackText = null);

    public sealed class ParsedResult
    {
        public string CleanText { get; set; } = string.Empty;
        public string? OverrideFontFamily { get; set; }
        public string? OverrideBackgroundColor { get; set; }
        public string? OverrideTextColor { get; set; }
        public float? OverrideFontSize { get; set; }
        public Dictionary<string, string> Variables { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<MarkupRun> Runs { get; } = new();
    }

    public static ParsedResult Parse(string? input, CgLayer? layer = null)
    {
        var result = new ParsedResult();
        if (string.IsNullOrEmpty(input))
        {
            return result;
        }

        // Quick check: if no '<kcg' tag present, return text with image symbol runs for market arrows
        if (!input.Contains("<kcg", StringComparison.OrdinalIgnoreCase))
        {
            result.CleanText = input;
            AddRunsWithImageSymbols(result.Runs, input, null, null);
            return result;
        }

        var sb = new StringBuilder();
        var lastIndex = 0;
        var matches = TagRegex.Matches(input);

        if (matches.Count == 0)
        {
            // Fallback for self-closing or unclosed tag syntax like <kcg-bgcolor="red" font="aakriti"/>
            var singleTagRegex = new Regex(@"<kcg(?:-([a-zA-Z0-9_\-]+))?([^>]*)>", RegexOptions.IgnoreCase);
            var mSingle = singleTagRegex.Matches(input);
            if (mSingle.Count > 0)
            {
                foreach (Match sm in mSingle)
                {
                    ParseAttributes(sm.Groups[2].Value, result);
                    var varName = sm.Groups[1].Value;
                    if (!string.IsNullOrWhiteSpace(varName) && !sm.Groups[2].Value.Contains('='))
                    {
                        result.Variables[varName] = string.Empty;
                    }
                }
                var clean = singleTagRegex.Replace(input, string.Empty);
                clean = Regex.Replace(clean, @"</kcg(?:-[a-zA-Z0-9_\-]+)?>", string.Empty, RegexOptions.IgnoreCase);
                result.CleanText = clean.Trim();
                AddRunsWithImageSymbols(result.Runs, result.CleanText, result.OverrideTextColor, result.OverrideBackgroundColor);
                return result;
            }

            result.CleanText = input;
            AddRunsWithImageSymbols(result.Runs, input, null, null);
            return result;
        }

        foreach (Match m in matches)
        {
            if (m.Index > lastIndex)
            {
                var pre = input.Substring(lastIndex, m.Index - lastIndex);
                sb.Append(pre);
                AddRunsWithImageSymbols(result.Runs, pre, null, null);
            }

            var varName = m.Groups[1].Value;
            var attrs = m.Groups[2].Value;
            var innerContent = m.Groups[3].Success ? m.Groups[3].Value : string.Empty;

            // Recurse inner content if nested tags exist (e.g. <kcg-<kcg-items>...>)
            if (innerContent.Contains("<kcg", StringComparison.OrdinalIgnoreCase))
            {
                var innerParsed = Parse(innerContent, layer);
                innerContent = innerParsed.CleanText;
                foreach (var kvp in innerParsed.Variables) result.Variables[kvp.Key] = kvp.Value;
                if (!string.IsNullOrWhiteSpace(innerParsed.OverrideFontFamily)) result.OverrideFontFamily = innerParsed.OverrideFontFamily;
                if (!string.IsNullOrWhiteSpace(innerParsed.OverrideBackgroundColor)) result.OverrideBackgroundColor = innerParsed.OverrideBackgroundColor;
                if (!string.IsNullOrWhiteSpace(innerParsed.OverrideTextColor)) result.OverrideTextColor = innerParsed.OverrideTextColor;
                if (innerParsed.OverrideFontSize.HasValue) result.OverrideFontSize = innerParsed.OverrideFontSize;
            }

            // Extract tag-specific colors & media
            ExtractTagColors(attrs, out var tagColor, out var tagBg);
            ExtractTagMedia(attrs, out var tagImg, out var tagFallback);

            // Parse attributes in opening tag
            ParseAttributes(attrs, result);

            // If tag was <kcg-category>world news</kcg>, record variable 'category' = 'world news'
            if (!string.IsNullOrWhiteSpace(varName))
            {
                if (varName.Contains('='))
                {
                    ParseAttributes(varName, result);
                }
                else
                {
                    result.Variables[varName] = innerContent.Trim();
                }
            }

            if (!string.IsNullOrWhiteSpace(tagImg))
            {
                var fallback = tagFallback ?? (string.IsNullOrEmpty(innerContent) ? (tagImg.Contains("down", StringComparison.OrdinalIgnoreCase) ? "▼" : "▲") : innerContent);
                sb.Append(fallback);
                result.Runs.Add(new MarkupRun(fallback, tagColor, tagBg, tagImg, fallback));
            }
            else
            {
                sb.Append(innerContent);
                AddRunsWithImageSymbols(result.Runs, innerContent, tagColor, tagBg);
            }

            lastIndex = m.Index + m.Length;
        }

        if (lastIndex < input.Length)
        {
            var post = input.Substring(lastIndex);
            sb.Append(post);
            AddRunsWithImageSymbols(result.Runs, post, null, null);
        }

        result.CleanText = sb.ToString();
        return result;
    }

    private static void AddRunsWithImageSymbols(List<MarkupRun> runs, string text, string? textColor, string? bgColor)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (text.Contains('▲') || text.Contains('▼'))
        {
            var parts = Regex.Split(text, @"([▲▼])");
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;
                if (part == "▲")
                {
                    runs.Add(new MarkupRun("▲", textColor, bgColor, "market-up.png", "▲"));
                }
                else if (part == "▼")
                {
                    runs.Add(new MarkupRun("▼", textColor, bgColor, "market-down.png", "▼"));
                }
                else
                {
                    runs.Add(new MarkupRun(part, textColor, bgColor));
                }
            }
        }
        else
        {
            runs.Add(new MarkupRun(text, textColor, bgColor));
        }
    }

    private static void ExtractTagMedia(string attrString, out string? imgPath, out string? fallback)
    {
        imgPath = null;
        fallback = null;
        if (string.IsNullOrWhiteSpace(attrString)) return;
        var matches = AttrRegex.Matches(attrString);
        foreach (Match m in matches)
        {
            var key = m.Groups[1].Value.Trim().ToLowerInvariant();
            var val = m.Groups[2].Value.Trim();
            if (key is "img" or "image" or "icon" or "src") imgPath = val;
            else if (key is "fallback" or "symbol" or "alt") fallback = val;
        }
    }

    private static void ExtractTagColors(string attrString, out string? textColor, out string? bgColor)
    {
        textColor = null;
        bgColor = null;
        if (string.IsNullOrWhiteSpace(attrString)) return;
        var matches = AttrRegex.Matches(attrString);
        foreach (Match m in matches)
        {
            var key = m.Groups[1].Value.Trim().ToLowerInvariant();
            var val = m.Groups[2].Value.Trim();
            if (key is "color" or "colour" or "textcolor" or "fgcolor" or "fontcolor") textColor = val;
            else if (key is "bgcolor" or "background" or "bg") bgColor = val;
        }
    }

    private static void ParseAttributes(string attrString, ParsedResult result)
    {
        if (string.IsNullOrWhiteSpace(attrString)) return;

        var matches = AttrRegex.Matches(attrString);
        foreach (Match m in matches)
        {
            var key = m.Groups[1].Value.Trim().ToLowerInvariant();
            var val = m.Groups[2].Value.Trim();

            switch (key)
            {
                case "bgcolor":
                case "background":
                case "bg":
                    result.OverrideBackgroundColor = NormalizeColorHex(val);
                    break;
                case "font":
                case "fontfamily":
                case "face":
                    result.OverrideFontFamily = val;
                    break;
                case "color":
                case "textcolor":
                case "fill":
                    result.OverrideTextColor = NormalizeColorHex(val);
                    break;
                case "size":
                case "fontsize":
                    if (float.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var sz) && sz > 2)
                    {
                        result.OverrideFontSize = sz;
                    }
                    break;
                default:
                    // Any custom parameters after kcg-${variables}
                    result.Variables[key] = val;
                    break;
            }
        }
    }

    public static string NormalizeColorHex(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "#FFFFFFFF";
        input = input.Trim();

        // Check named standard broadcast colors
        return input.ToLowerInvariant() switch
        {
            "red" => "#FFFF2244",
            "darkred" => "#FF990000",
            "green" => "#FF00E676",
            "darkgreen" => "#FF007722",
            "blue" => "#FF00B0FF",
            "darkblue" => "#FF081422",
            "navy" => "#FF0B1728",
            "yellow" => "#FFFFD600",
            "gold" => "#FFFFB300",
            "orange" => "#FFFF6D00",
            "cyan" => "#FF00E5FF",
            "magenta" or "pink" => "#FFFF4081",
            "purple" => "#FF7C4DFF",
            "white" => "#FFFFFFFF",
            "black" => "#FF000000",
            "gray" or "grey" => "#FF78909C",
            "transparent" => "#00000000",
            _ => input.StartsWith('#') ? input : (input.Length is 6 or 8 ? $"#{input}" : input)
        };
    }
}
