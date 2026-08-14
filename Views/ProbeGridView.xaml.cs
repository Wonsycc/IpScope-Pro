using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using IpScopePro.ViewModels;

namespace IpScopePro.Views;

public partial class ProbeGridView : UserControl
{
    private const int CardSpacing = 4;
    private const int MinItemWidth = 100;
    private const int MinItemHeight = 50;
    private readonly Dictionary<string, ProbeCardView> _cardCache = new();
    private MainViewModel? _vm;

    public ProbeGridView()
    {
        InitializeComponent();
        FixedGrid.SizeChanged += FixedGrid_SizeChanged;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainViewModel oldVm)
        {
            oldVm.Probes.CollectionChanged -= OnProbesChanged;
        }
        if (e.NewValue is MainViewModel newVm)
        {
            _vm = newVm;
            newVm.Probes.CollectionChanged += OnProbesChanged;
            RebuildGrid();
        }
    }

    private void OnProbesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
        {
            foreach (ProbeViewModel vm in e.OldItems)
                _cardCache.Remove(vm.Model.Id);
        }
        RebuildGrid();
    }

    private void FixedGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        RebuildGrid();
    }

    private void RebuildGrid()
    {
        if (_vm == null) return;

        var availableWidth = FixedGrid.ActualWidth;
        var availableHeight = FixedGrid.ActualHeight;
        var probeCount = _vm.Probes.Count;

        if (availableWidth <= 0 || probeCount == 0)
        {
            _vm.IsFixedOverflowing = false;
            FixedGrid.Children.Clear();
            FixedGrid.RowDefinitions.Clear();
            FixedGrid.ColumnDefinitions.Clear();
            return;
        }

        var cols = probeCount <= 3
            ? probeCount
            : Math.Max(1, (int)Math.Ceiling(Math.Sqrt(probeCount)));
        var rows = (int)Math.Ceiling((double)probeCount / cols);

        var itemWidth = (availableWidth - (cols + 1) * CardSpacing) / cols;
        var itemHeight = availableHeight > 0
            ? (availableHeight - (rows + 1) * CardSpacing) / rows
            : 0;

        _vm.IsFixedOverflowing = itemWidth < MinItemWidth || (itemHeight > 0 && itemHeight < MinItemHeight);

        bool needsRebuild = FixedGrid.ColumnDefinitions.Count != cols ||
                            FixedGrid.RowDefinitions.Count != rows ||
                            FixedGrid.Children.Count != probeCount;

        if (!needsRebuild) return;

        FixedGrid.Children.Clear();
        FixedGrid.RowDefinitions.Clear();
        FixedGrid.ColumnDefinitions.Clear();

        for (var c = 0; c < cols; c++)
            FixedGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (var r = 0; r < rows; r++)
            FixedGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        for (var i = 0; i < probeCount; i++)
        {
            var probeVm = _vm.Probes[i];
            var id = probeVm.Model.Id;

            if (!_cardCache.TryGetValue(id, out var card))
            {
                card = new ProbeCardView { DataContext = probeVm };
                _cardCache[id] = card;
            }
            else if (card.DataContext != probeVm)
            {
                card.DataContext = probeVm;
            }

            card.HorizontalAlignment = HorizontalAlignment.Stretch;
            card.VerticalAlignment = VerticalAlignment.Stretch;
            card.Margin = new Thickness(CardSpacing);

            Grid.SetRow(card, i / cols);
            Grid.SetColumn(card, i % cols);
            FixedGrid.Children.Add(card);
        }

        var toRemove = _cardCache.Keys.Except(_vm.Probes.Select(p => p.Model.Id)).ToList();
        foreach (var key in toRemove)
            _cardCache.Remove(key);
    }

    private void MaximizedRemoveProbe_Click(object sender, RoutedEventArgs e)
    {
        if (_vm?.MaximizedProbeVm is not { } probeVm) return;

        var vm = probeVm;
        _vm.MaximizedProbeVm = null;
        _vm.RemoveProbeCommand.Execute(vm);
    }
}
