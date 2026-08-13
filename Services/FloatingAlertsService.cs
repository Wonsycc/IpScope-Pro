using System.Collections.ObjectModel;
using IpScopePro.Models;

namespace IpScopePro.Services;

public class FloatingAlertsService
{
    private const int MaxEntries = 5;

    public ObservableCollection<StatusChangeLogEntry> Entries { get; } = new();
    public bool IsVisible { get; set; } = true;
    public bool IsMinimized { get; set; }

    public void AddEntry(StatusChangeLogEntry entry)
    {
        if (Entries.Count >= MaxEntries)
            Entries.RemoveAt(0);

        Entries.Add(entry);
    }
}
