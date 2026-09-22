using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskManager.Application.Common.Models
{
    public readonly struct Optional<T>
    {
        public bool IsSet { get; }
        public T? Value { get; }

        public Optional(bool isSet,T? value)
        {
            IsSet = isSet;
            Value = value;
        }

        public static Optional<T> None()=>new(false, default);
        public static Optional<T> Some(T value) => new(true, value);
        
        public T GetValueOrExisting(T existingValue)
        {
            return IsSet ? Value! : existingValue;
        }
    }
}