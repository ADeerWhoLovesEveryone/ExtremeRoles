﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ExtremeRoles.Module.CustomOption;

public sealed class TypeOptionHolder<T> : IEnumerable<KeyValuePair<int, IValueOption<T>>>
    where T :
        struct, IComparable, IConvertible,
        IComparable<T>, IEquatable<T>
{
    public ICollection<IValueOption<T>> Values => this.option.Values;
    private Dictionary<int, IValueOption<T>> option = new Dictionary<int, IValueOption<T>>();

    public IValueOption<T> Get(int id) => this.option[id];

    public bool ContainsKey(int id) => this.option.ContainsKey(id);

    public void Add(int id, IValueOption<T> newOption)
        => this.option.Add(id, newOption);
    public T GetValue(int index)
        => this.option[index].GetValue();

    public bool TryGetValue<GetType>(int id, out IValueOption<GetType> option)
        where GetType :
            struct, IComparable, IConvertible,
            IComparable<GetType>, IEquatable<GetType>
    {
        bool result = this.option.TryGetValue(id, out var outOption);
        option = Unsafe.As<IValueOption<T>, IValueOption<GetType>>(ref outOption);
        return result;
    }

    public void Update(int id, int selectionIndex)
    {
        lock (this.option)
        {
            this.option[id].UpdateSelection(selectionIndex);
        }
    }

    public IEnumerator<KeyValuePair<int, IValueOption<T>>> GetEnumerator() =>
        this.option.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw null;
    }
}
