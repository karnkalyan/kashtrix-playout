using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace BroadcastPlayout.Models;

/// <summary>
/// A user-configured conditional rule that triggers automated CG graphics when a playlist
/// item matching specified conditions (Category, Title, Description, etc.) starts playing on a channel.
/// Multi-channel: Applied strictly per playout channel, never globally.
/// </summary>
public sealed class ConditionalCgRule : INotifyPropertyChanged
{
    private Guid _id = Guid.NewGuid();
    private bool _enabled = true;
    private string _name = "Music Lower Third Trigger";
    private string _channelId = string.Empty;
    private string _conditionExpression = "category = music and title = arijit";
    private string _targetCgProjectName = "Music · Now Playing Lower Third";
    private int _targetLayerNumber = 1;
    private double _delaySeconds = 1.0;
    private double _durationSeconds = 12.0;
    private string _action = "PLAY";

    public Guid Id { get => _id; set => Set(ref _id, value); }
    public bool Enabled { get => _enabled; set => Set(ref _enabled, value); }
    public string Name { get => _name; set => Set(ref _name, string.IsNullOrWhiteSpace(value) ? "Condition Rule" : value.Trim()); }
    public string ChannelId { get => _channelId; set => Set(ref _channelId, value ?? string.Empty); }
    public string ConditionExpression { get => _conditionExpression; set => Set(ref _conditionExpression, value ?? string.Empty); }
    public string TargetCgProjectName { get => _targetCgProjectName; set => Set(ref _targetCgProjectName, value ?? string.Empty); }
    public int TargetLayerNumber { get => _targetLayerNumber; set => Set(ref _targetLayerNumber, Math.Clamp(value, 1, 8)); }
    public double DelaySeconds { get => _delaySeconds; set => Set(ref _delaySeconds, Math.Max(0.0, value)); }
    public double DurationSeconds { get => _durationSeconds; set => Set(ref _durationSeconds, Math.Max(0.0, value)); }
    public string Action { get => _action; set => Set(ref _action, string.IsNullOrWhiteSpace(value) ? "PLAY" : value.Trim().ToUpperInvariant()); }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public bool Matches(PlaylistItem? item)
    {
        if (!Enabled || item is null || string.IsNullOrWhiteSpace(ConditionExpression)) return false;
        return ConditionalCgEvaluator.Evaluate(ConditionExpression, item);
    }
}

/// <summary>
/// Evaluates conditional expressions (supports IF, AND, OR, =, !=, contains, wildcards)
/// against a playing playlist item's metadata (Category, Title, Description, Artist, etc.).
/// </summary>
public static class ConditionalCgEvaluator
{
    public static bool Evaluate(string expression, PlaylistItem item)
    {
        if (string.IsNullOrWhiteSpace(expression)) return false;
        var exp = expression.Trim();

        // Strip leading "if " and trailing "then play..." if present
        if (exp.StartsWith("if ", StringComparison.OrdinalIgnoreCase))
        {
            exp = exp[3..].Trim();
        }
        var thenIdx = exp.IndexOf(" then ", StringComparison.OrdinalIgnoreCase);
        if (thenIdx > 0)
        {
            exp = exp[..thenIdx].Trim();
        }

        // Top-level OR splitting: condition is true if ANY OR clause is true
        var orClauses = Regex.Split(exp, @"\s+or\s+", RegexOptions.IgnoreCase);
        foreach (var orClause in orClauses)
        {
            if (EvaluateAndClause(orClause.Trim(), item))
            {
                return true;
            }
        }
        return false;
    }

    private static bool EvaluateAndClause(string andClause, PlaylistItem item)
    {
        if (string.IsNullOrWhiteSpace(andClause)) return false;

        // AND splitting: condition is true if ALL terms are true
        var terms = Regex.Split(andClause, @"\s+and\s+", RegexOptions.IgnoreCase);
        foreach (var term in terms)
        {
            if (!EvaluateSingleTerm(term.Trim(), item))
            {
                return false;
            }
        }
        return true;
    }

    private static bool EvaluateSingleTerm(string term, PlaylistItem item)
    {
        if (string.IsNullOrWhiteSpace(term)) return true;

        // Pattern matching: Field Operator Value
        // Operators: =, ==, !=, <>, contains, like, startswith
        var match = Regex.Match(term, @"^([a-zA-Z0-9_\-]+)\s*(=|==|!=|<>|contains|like|startswith)\s*(.*)$", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            // Simple keyword presence test
            var kw = term.Trim().Trim('\'', '"');
            return item.Title.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                   item.Category.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                   (item.Description?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false);
        }

        var field = match.Groups[1].Value.Trim().ToLowerInvariant();
        var op = match.Groups[2].Value.Trim().ToLowerInvariant();
        var expected = match.Groups[3].Value.Trim().Trim('\'', '"');

        var actual = GetFieldValue(item, field);

        // Wildcard wildcard '*' matches anything if not empty
        if (expected == "*" || expected.Equals("anything", StringComparison.OrdinalIgnoreCase))
        {
            return !string.IsNullOrWhiteSpace(actual);
        }

        return op switch
        {
            "=" or "==" => string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase) || actual.Contains(expected, StringComparison.OrdinalIgnoreCase),
            "!=" or "<>" => !actual.Contains(expected, StringComparison.OrdinalIgnoreCase),
            "contains" or "like" => actual.Contains(expected, StringComparison.OrdinalIgnoreCase),
            "startswith" => actual.StartsWith(expected, StringComparison.OrdinalIgnoreCase),
            _ => actual.Contains(expected, StringComparison.OrdinalIgnoreCase)
        };
    }

    private static string GetFieldValue(PlaylistItem item, string field)
    {
        return field switch
        {
            "category" or "cat" or "genre" => item.Category ?? string.Empty,
            "title" or "name" or "song" => item.Title ?? string.Empty,
            "description" or "desc" or "summary" => item.Description ?? string.Empty,
            "artist" or "singer" or "presenter" => item.Artist ?? string.Empty,
            "filename" or "file" or "path" => item.FilePath ?? string.Empty,
            "notes" or "note" => item.Notes ?? string.Empty,
            _ => item.Title ?? string.Empty
        };
    }
}
