using System.Collections.ObjectModel;

namespace Module.HeroVirtualTabletop.Characters;

/// <summary>
/// An ordered, keyed collection that enforces name-uniqueness.
/// Extracted from ViewModel plumbing: the uniqueness invariant, ordering, and
/// CollectionChanged all live here so ViewModels can bind to it directly.
///
/// Canonical example from the reference: Character.Identities, .Abilities, .Movements
/// are all OptionGroup instances — the ViewModel holds no parallel dictionaries.
/// </summary>
public class OptionGroup<T> : ObservableCollection<T> where T : class
{
    private readonly Func<T, string> _key;
    private readonly HashSet<string> _keys = new(StringComparer.OrdinalIgnoreCase);

    public OptionGroup(Func<T, string> keySelector) => _key = keySelector;

    protected override void InsertItem(int index, T item)
    {
        if (!_keys.Add(_key(item))) return;   // silently reject duplicates
        base.InsertItem(index, item);
    }

    protected override void RemoveItem(int index)
    {
        _keys.Remove(_key(this[index]));
        base.RemoveItem(index);
    }
}
