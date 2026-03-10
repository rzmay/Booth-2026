using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EventMap<TKey, TAction>
    where TAction : Delegate
{
  private readonly Dictionary<TKey, TAction> _dict = new();

  public TAction this[TKey key]
  {
    get
    {
      if (!_dict.TryGetValue(key, out var value))
      {
        value = null;
        _dict[key] = value;
      }
      return value;
    }
    set
    {
      _dict[key] = value;
    }
  }

  public bool TryGetValue(TKey key, out TAction value)
      => _dict.TryGetValue(key, out value);

  public IEnumerable<KeyValuePair<TKey, TAction>> Entries => _dict;

  public bool ContainsKey(TKey key) => _dict.ContainsKey(key);
}
