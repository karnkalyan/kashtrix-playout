using System;
using System.Collections.Generic;
using System.Linq;
using BroadcastPlayout.Models;

namespace BroadcastPlayout.Services;

/// <summary>
/// Intelligent Automatic Layer Adjustment and Adaptive Non-Overlapping Layer Management System.
/// Handles dynamic resizing, collision prevention, priority-based space negotiation,
/// and smooth cinematic transitions between on-air graphics.
/// </summary>
public static class CgSmartLayerManager
{
    private static readonly Dictionary<Guid, DateTime> _retriggerTimes = new();

    /// <summary>
    /// Retriggers animation for a specific layer.
    /// </summary>
    public static void RetriggerLayer(CgLayer layer, double playhead = 0.0)
    {
        _retriggerTimes[layer.Id] = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the elapsed time since the layer's animation was retriggered (or 0 if not retriggered).
    /// </summary>
    public static double GetRetriggerElapsed(CgLayer layer, double playhead = 0.0)
    {
        if (_retriggerTimes.TryGetValue(layer.Id, out var dt))
        {
            return (DateTime.UtcNow - dt).TotalSeconds;
        }
        return -1.0;
    }

    /// <summary>
    /// Gets the effective runtime (Y, Height) with layer defaults.
    /// </summary>
    public static (double Y, double Height) GetEffectiveYAndHeight(CgLayer layer)
    {
        return GetEffectiveYAndHeight(layer, layer.Y, layer.Height);
    }

    /// <summary>
    /// Evaluates Breaking News animation tuple for compositor and preview rendering.
    /// </summary>
    public static (double PushOffsetX, double Opacity, double Scale, double GlowIntensity, bool YellowGlow) EvaluateBreakingNews(CgLayer layer, double playhead)
    {
        if (layer == null) return (0, 1, 1, 0, false);
        var isBreaking = (layer.Role?.Equals("BreakingNews", StringComparison.OrdinalIgnoreCase) ?? false) ||
                         (layer.LayerAnimationPreset != null && !layer.LayerAnimationPreset.Equals("None", StringComparison.OrdinalIgnoreCase) && layer.LayerAnimationPreset.Contains("Breaking", StringComparison.OrdinalIgnoreCase));
        if (!isBreaking) return (0, 1, 1, 0, false);

        var elapsed = GetRetriggerElapsed(layer, playhead);
        var dur = Math.Max(1.0, layer.EndSeconds - layer.StartSeconds);
        EvaluateBreakingNews(layer, elapsed > 0 ? elapsed : playhead, dur, out var pushX, out var sc, out var glow, out _, out _, out _);
        return (pushX, 1.0, sc, glow, glow > 5.0);
    }

    /// <summary>
    /// Updates all active layers in the composition, negotiating screen space and smoothly
    /// adjusting heights and Y positions for non-overlapping layout.
    /// </summary>
    public static void UpdateLayout(CgProject? project, double deltaSeconds = 0.016, double playhead = 0.0)
    {
        if (project?.Layers == null || project.Layers.Count == 0) return;

        var canvasH = project.Height > 0 ? project.Height : 1080.0;
        var canvasW = project.Width > 0 ? project.Width : 1920.0;

        // Find active layers based on timecode and playback flag where smart layout is explicitly enabled
        var activeLayers = project.Layers
            .Where(l => l.Visible && l.IsOnAirPlaying && (l.IsFlexible || l.AutoShift || l.AutoResize) && (playhead < 0 || (playhead >= l.StartSeconds - 0.05 && playhead <= l.EndSeconds + 0.05)))
            .ToList();

        // Separate bottom zone layers (crawls, tickers, lower thirds, breaking news)
        var bottomZoneLayers = activeLayers
            .Where(l => l.CollisionPrevention && (
                l.ReservedZone.Equals("BottomZone", StringComparison.OrdinalIgnoreCase) ||
                l.ReservedZone.Equals("LowerThirdZone", StringComparison.OrdinalIgnoreCase) ||
                l.AnchorPosition.Equals("Bottom", StringComparison.OrdinalIgnoreCase) ||
                l.Role.Equals("LowerThird", StringComparison.OrdinalIgnoreCase) ||
                l.Role.Equals("BreakingNews", StringComparison.OrdinalIgnoreCase)
            ))
            .OrderByDescending(l => l.LayerPriority)
            .ToList();

        // 1. Process Bottom Zone conflict negotiation
        if (bottomZoneLayers.Count > 0)
        {
            // Detect Crawl (lowest priority in bottom zone, e.g. continuous news ticker crawl)
            var crawl = bottomZoneLayers.FirstOrDefault(l =>
                string.Equals(l.Type, "Ticker", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(l.TickerMode, "Continuous", StringComparison.OrdinalIgnoreCase));

            // Detect Stacked Ticker / Indices (above crawl)
            var stackedTicker = bottomZoneLayers.FirstOrDefault(l =>
                !ReferenceEquals(l, crawl) &&
                string.Equals(l.Type, "Ticker", StringComparison.OrdinalIgnoreCase));

            // Detect Lower Third or Breaking News
            var breakingNews = bottomZoneLayers.FirstOrDefault(l =>
                l.Role.Equals("BreakingNews", StringComparison.OrdinalIgnoreCase) ||
                (l.LayerAnimationPreset != null && !l.LayerAnimationPreset.Equals("None", StringComparison.OrdinalIgnoreCase) && l.LayerAnimationPreset.Contains("Breaking", StringComparison.OrdinalIgnoreCase)));

            var lowerThird = bottomZoneLayers.FirstOrDefault(l =>
                !ReferenceEquals(l, breakingNews) &&
                l.Role.Equals("LowerThird", StringComparison.OrdinalIgnoreCase));

            double currentBottomY = canvasH;

            // Arrange from bottom to top:
            // Layer 1: Crawl at the very bottom
            if (crawl != null && !crawl.LockPosition)
            {
                var preferredH = crawl.DefaultHeight > 0 ? crawl.DefaultHeight : crawl.Height;
                var minH = crawl.MinHeight > 0 ? crawl.MinHeight : 65.0;

                // If ticker or breaking news is playing above crawl, adaptively compress crawl
                if (stackedTicker != null || breakingNews != null)
                {
                    var targetH = Math.Max(minH, preferredH * 0.58);
                    crawl.TargetAdjustedHeight = targetH;
                    crawl.TargetAdjustedY = canvasH - targetH;
                }
                else
                {
                    // Full configured height when playing alone
                    crawl.TargetAdjustedHeight = preferredH;
                    crawl.TargetAdjustedY = canvasH - preferredH;
                }

                currentBottomY = crawl.TargetAdjustedY;
            }

            // Layer 2: Stacked Ticker directly above Crawl
            if (stackedTicker != null && !stackedTicker.LockPosition)
            {
                var preferredH = stackedTicker.DefaultHeight > 0 ? stackedTicker.DefaultHeight : stackedTicker.Height;
                stackedTicker.TargetAdjustedHeight = preferredH;
                stackedTicker.TargetAdjustedY = currentBottomY - preferredH;
                currentBottomY = stackedTicker.TargetAdjustedY;
            }

            // Layer 3: Lower Third directly above Ticker / Crawl
            if (lowerThird != null && !lowerThird.LockPosition)
            {
                var preferredH = lowerThird.DefaultHeight > 0 ? lowerThird.DefaultHeight : lowerThird.Height;
                lowerThird.TargetAdjustedHeight = preferredH;
                // Leave 10px breathing room
                lowerThird.TargetAdjustedY = currentBottomY - preferredH - 10.0;
                currentBottomY = lowerThird.TargetAdjustedY;
            }

            // Layer 4: Breaking News
            if (breakingNews != null && !breakingNews.LockPosition)
            {
                var preferredH = breakingNews.DefaultHeight > 0 ? breakingNews.DefaultHeight : breakingNews.Height;
                breakingNews.TargetAdjustedHeight = preferredH;
                breakingNews.TargetAdjustedY = currentBottomY - preferredH - 12.0;
            }
        }

        // 2. Universal Dynamic Overlap Height Reduction:
        // Any playing active layers that collide in 2D space adaptively decrease height during playout
        for (int i = 0; i < activeLayers.Count; i++)
        {
            var layerA = activeLayers[i];
            for (int j = i + 1; j < activeLayers.Count; j++)
            {
                var layerB = activeLayers[j];
                // Check horizontal overlap
                var xOverlap = Math.Max(layerA.X, layerB.X) < (Math.Min(layerA.X + layerA.Width, layerB.X + layerB.Width) - 4.0);
                if (!xOverlap) continue;

                var yA = double.IsFinite(layerA.TargetAdjustedY) ? layerA.TargetAdjustedY : layerA.Y;
                var hA = double.IsFinite(layerA.TargetAdjustedHeight) ? layerA.TargetAdjustedHeight : layerA.Height;
                var yB = double.IsFinite(layerB.TargetAdjustedY) ? layerB.TargetAdjustedY : layerB.Y;
                var hB = double.IsFinite(layerB.TargetAdjustedHeight) ? layerB.TargetAdjustedHeight : layerB.Height;

                // Check vertical overlap
                var yOverlap = Math.Max(yA, yB) < (Math.Min(yA + hA, yB + hB) - 2.0);
                if (!yOverlap) continue;

                // Determine yielding layer (lower priority or lower on screen)
                var victim = (layerA.LayerPriority < layerB.LayerPriority) ? layerA :
                             (layerB.LayerPriority < layerA.LayerPriority) ? layerB :
                             (yA + hA >= yB + hB) ? layerA : layerB;
                var dominator = ReferenceEquals(victim, layerA) ? layerB : layerA;

                if (victim.LockPosition) continue;

                var overlapH = Math.Min(yA + hA, yB + hB) - Math.Max(yA, yB);
                if (overlapH > 1.0)
                {
                    var prefH = victim.DefaultHeight > 0 ? victim.DefaultHeight : victim.Height;
                    var minH = victim.MinHeight > 0 ? victim.MinHeight : Math.Max(25.0, prefH * 0.5);
                    var newH = Math.Max(minH, prefH - overlapH);
                    victim.TargetAdjustedHeight = newH;
                    if (victim.AnchorPosition.Equals("Bottom", StringComparison.OrdinalIgnoreCase) ||
                        victim.ReservedZone.Equals("BottomZone", StringComparison.OrdinalIgnoreCase) ||
                        victim.Y + victim.Height >= canvasH * 0.70)
                    {
                        victim.TargetAdjustedY = (victim.Y + prefH) - newH;
                    }
                }
            }
        }

        // 2. Smooth Per-Frame Interpolation for All Layers
        var lerpFactor = Math.Clamp(deltaSeconds * 12.0, 0.04, 1.0);
        foreach (var layer in project.Layers)
        {
            if (double.IsFinite(layer.TargetAdjustedY))
            {
                if (double.IsNaN(layer.CurrentAdjustedY))
                    layer.CurrentAdjustedY = layer.TargetAdjustedY;
                else
                    layer.CurrentAdjustedY += (layer.TargetAdjustedY - layer.CurrentAdjustedY) * lerpFactor;
            }
            else
            {
                // Restore to base Y if no adjustment active
                if (!double.IsNaN(layer.CurrentAdjustedY))
                {
                    layer.CurrentAdjustedY += (layer.Y - layer.CurrentAdjustedY) * lerpFactor;
                    if (Math.Abs(layer.CurrentAdjustedY - layer.Y) < 0.5)
                        layer.CurrentAdjustedY = double.NaN;
                }
            }

            if (double.IsFinite(layer.TargetAdjustedHeight))
            {
                if (double.IsNaN(layer.CurrentAdjustedHeight))
                    layer.CurrentAdjustedHeight = layer.TargetAdjustedHeight;
                else
                    layer.CurrentAdjustedHeight += (layer.TargetAdjustedHeight - layer.CurrentAdjustedHeight) * lerpFactor;
            }
            else
            {
                // Restore to base Height if no adjustment active
                if (!double.IsNaN(layer.CurrentAdjustedHeight))
                {
                    layer.CurrentAdjustedHeight += (layer.Height - layer.CurrentAdjustedHeight) * lerpFactor;
                    if (Math.Abs(layer.CurrentAdjustedHeight - layer.Height) < 0.5)
                        layer.CurrentAdjustedHeight = double.NaN;
                }
            }
        }
    }

    /// <summary>
    /// Gets the effective runtime (Y, Height) for rendering or preview.
    /// Returns adjusted values if smart management is active, otherwise falls back to the motion values.
    /// </summary>
    public static (double Y, double Height) GetEffectiveYAndHeight(CgLayer layer, double motionY, double motionHeight)
    {
        var y = double.IsFinite(layer.CurrentAdjustedY) ? layer.CurrentAdjustedY : motionY;
        var h = double.IsFinite(layer.CurrentAdjustedHeight) ? layer.CurrentAdjustedHeight : motionHeight;
        return (y, Math.Max(4.0, h));
    }

    /// <summary>
    /// Evaluates professional broadcast Breaking News animation attributes:
    /// Push in from left, text flip &amp; scale, temporary motion blur, yellow transition glow,
    /// and continuous shiny highlight sweep.
    /// </summary>
    public static void EvaluateBreakingNews(
        CgLayer layer,
        double localSeconds,
        double duration,
        out double pushOffsetX,
        out double scale,
        out double glowIntensity,
        out string glowColor,
        out bool motionBlur,
        out double shineProgress)
    {
        glowColor = "#FFFFD54F";
        shineProgress = 0.0;

        var inDuration = Math.Max(0.2, layer.AnimationInSeconds > 0 ? layer.AnimationInSeconds : 0.65);
        var outDuration = Math.Max(0.2, layer.AnimationOutSeconds > 0 ? layer.AnimationOutSeconds : 0.65);

        // Entry animation (Push in from left with cubic spring)
        if (localSeconds < inDuration)
        {
            var p = Math.Clamp(localSeconds / inDuration, 0.0, 1.0);
            var cubic = CgAnimationEngine.EvaluateCubicBezier("cubic-bezier(0.16, 1, 0.3, 1)", p);
            pushOffsetX = (1.0 - cubic) * -600.0;
            scale = 1.0 + (1.0 - cubic) * 0.28;
            glowIntensity = Math.Sin(p * Math.PI) * 22.0;
            motionBlur = p < 0.85;
        }
        // Exit animation (Push out to right)
        else if (localSeconds > duration - outDuration)
        {
            var p = Math.Clamp((localSeconds - (duration - outDuration)) / outDuration, 0.0, 1.0);
            var cubic = CgAnimationEngine.EvaluateCubicBezier("cubic-bezier(0.7, 0, 0.84, 0)", p);
            pushOffsetX = cubic * 650.0;
            scale = 1.0 - cubic * 0.15;
            glowIntensity = cubic * 18.0;
            motionBlur = p > 0.15;
        }
        // Sustained on-air state
        else
        {
            pushOffsetX = 0.0;
            scale = 1.0;
            glowIntensity = 4.0 + Math.Sin(localSeconds * 4.0) * 3.0; // gentle broadcast pulse
            motionBlur = false;
        }

        // Continuous shiny BREAKING NEWS sweep across banner
        shineProgress = (localSeconds * 0.65) % 1.0;
    }
}
