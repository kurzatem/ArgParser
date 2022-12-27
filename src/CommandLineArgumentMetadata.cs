// /*
//    Copyright 2022 CommandLineArgumentMetadata.cs
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// */

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace ArgParser;

public class CommandLineArgumentMetadata<TEnum> : IEqualityComparer<string>
    where TEnum : Enum
{
    public IReadOnlyList<string> Aliases { get; }
    
    public StringComparison ComparisonOption { get; }
    
    public Func<string[]?, object[]?> Converter { get; }
    
    public Delegate Delegate { get; }
        
    public TEnum Name { get; }
    
    public int Priority { get; internal set; }
    
    public int UnnamedAliasIndex { get; }
    
    internal CommandLineArgumentMetadata(CommandLineArgumentMetadata<TEnum> original) :
        this (original.Name, original.Delegate, original.Aliases, original.Priority, original.Converter, original.ComparisonOption, original.UnnamedAliasIndex) { }

    public CommandLineArgumentMetadata(TEnum name, Delegate @delegate, IEnumerable<string> aliases, int priority, Func<string[]?, object[]?> converter, StringComparison comparisonOption, int unnamedAliasIndex)
    {
        this.Name = name;
        this.Delegate = @delegate;
        this.Aliases = aliases.ToArray();
        this.Priority = priority;
        this.Converter = converter;
        this.ComparisonOption = comparisonOption;
        this.UnnamedAliasIndex = unnamedAliasIndex;
    }
    
    public CommandLineArgumentMetadata(TEnum name, Delegate @delegate, IEnumerable<string> aliases, int priority, Func<string[]?, object[]?> converter, StringComparison comparisonOption) :
        this (name, @delegate, aliases, priority, converter, comparisonOption, -1) { }

    public CommandLineArgumentMetadata(TEnum name, Delegate @delegate, IEnumerable<string> aliases, int priority, Func<string[]?, object[]?> converter) :
        this(name, @delegate, aliases, priority, converter, StringComparison.CurrentCultureIgnoreCase, -1) { }

    public CommandLineArgumentMetadata(TEnum name, Delegate @delegate, IEnumerable<string> aliases, int priority, Func<string[]?, object[]?> converter, int unnamedListingOrder) :
        this(name, @delegate, aliases, priority, converter, StringComparison.CurrentCultureIgnoreCase, unnamedListingOrder) { }
    
    public CommandLineArgumentMetadata(TEnum name, Delegate @delegate, IEnumerable<string> aliases, int priority, KnownPrimitives convertibleType) :
        this (name, @delegate, aliases, priority, GetKnownPrimitiveConverter(convertibleType), -1) { }
    
    public CommandLineArgumentMetadata(TEnum name, Delegate @delegate, IEnumerable<string> aliases, int priority, KnownPrimitives convertibleType, int unnamedListingOrder) :
        this (name, @delegate, aliases, priority, GetKnownPrimitiveConverter(convertibleType), unnamedListingOrder) { }
    
    public CommandLineArgumentMetadata(TEnum name, Delegate @delegate, IEnumerable<string> aliases, int priority, KnownPrimitives convertibleType, StringComparison comparisonOption) :
        this (name, @delegate, aliases, priority, GetKnownPrimitiveConverter(convertibleType), comparisonOption, -1) { }
    
    public CommandLineArgumentMetadata(TEnum name, Delegate @delegate, IEnumerable<string> aliases, int priority, KnownPrimitives convertibleType, StringComparison comparisonOption, int unnamedAliasIndex) :
        this (name, @delegate, aliases, priority, GetKnownPrimitiveConverter(convertibleType), comparisonOption, unnamedAliasIndex) { }

    private static Func<string[]?, object[]?> GetKnownPrimitiveConverter(KnownPrimitives type)
    {
        switch (type)
        {
            case KnownPrimitives.Byte: return KnownPrimitiveParser.Create(byte.Parse).ParseAll;
            case KnownPrimitives.Char: return KnownPrimitiveParser.Create(char.Parse).ParseAll;
            case KnownPrimitives.Double: return KnownPrimitiveParser.Create(double.Parse).ParseAll;
            case KnownPrimitives.Int: return KnownPrimitiveParser.Create(int.Parse).ParseAll;
            case KnownPrimitives.Long: return KnownPrimitiveParser.Create(long.Parse).ParseAll;
            case KnownPrimitives.Short: return KnownPrimitiveParser.Create(short.Parse).ParseAll;
            case KnownPrimitives.Single: return KnownPrimitiveParser.Create(float.Parse).ParseAll;
            case KnownPrimitives.String: return KnownPrimitiveParser.Create((s) => s).ParseAll;
            case KnownPrimitives.SByte: return KnownPrimitiveParser.Create(sbyte.Parse).ParseAll;
            case KnownPrimitives.UInt: return KnownPrimitiveParser.Create(uint.Parse).ParseAll;
            case KnownPrimitives.ULong: return KnownPrimitiveParser.Create(ulong.Parse).ParseAll;
            case KnownPrimitives.UShort: return KnownPrimitiveParser.Create(ushort.Parse).ParseAll;
            default: throw new InvalidEnumArgumentException(nameof(type));
        }
    }

    public bool Equals(string? x, string? y)
    {
        if (x != null)
        {
            return x.Equals(y, this.ComparisonOption);
        }

        return y == null;
    }

    public int GetHashCode(string obj)
    {
        return obj.GetHashCode();
    }
}