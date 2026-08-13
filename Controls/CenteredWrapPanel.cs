using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace IpScopePro.Controls;

public class CenteredWrapPanel : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width)
            ? double.PositiveInfinity
            : availableSize.Width;

        double lineWidth = 0;
        double lineHeight = 0;
        double totalWidth = 0;
        double totalHeight = 0;

        foreach (UIElement child in InternalChildren)
        {
            child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var size = child.DesiredSize;

            if (double.IsInfinity(width) || lineWidth + size.Width <= width)
            {
                lineWidth += size.Width;
                lineHeight = Math.Max(lineHeight, size.Height);
            }
            else
            {
                totalWidth = Math.Max(totalWidth, lineWidth);
                totalHeight += lineHeight;
                lineWidth = size.Width;
                lineHeight = size.Height;
            }
        }

        totalWidth = Math.Max(totalWidth, lineWidth);
        totalHeight += lineHeight;

        return new Size(
            double.IsInfinity(width) ? totalWidth : width,
            totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = finalSize.Width;
        double lineWidth = 0;
        double lineHeight = 0;
        double y = 0;
        var line = new List<UIElement>();

        foreach (UIElement child in InternalChildren)
        {
            var size = child.DesiredSize;

            if (line.Count > 0 && lineWidth + size.Width > width)
            {
                y += ArrangeLine(line, lineHeight, y, width, lineWidth);
                line.Clear();
                lineWidth = 0;
                lineHeight = 0;
            }

            lineWidth += size.Width;
            lineHeight = Math.Max(lineHeight, size.Height);
            line.Add(child);
        }

        if (line.Count > 0)
            y += ArrangeLine(line, lineHeight, y, width, lineWidth);

        return finalSize;
    }

    private static double ArrangeLine(List<UIElement> line, double lineHeight, double y, double totalWidth, double lineWidth)
    {
        var x = Math.Max(0.0, (totalWidth - lineWidth) / 2.0);

        foreach (var child in line)
        {
            var width = child.DesiredSize.Width;
            child.Arrange(new Rect(x, y, width, lineHeight));
            x += width;
        }

        return lineHeight;
    }
}
