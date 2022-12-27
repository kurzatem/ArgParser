// /*
//    Copyright 2022 Parser.cs
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

namespace ArgParser;

public class Parser<TEnum>
    where TEnum: struct, Enum
{
    private static readonly TEnum[] _allKeys = Enum.GetValues<TEnum>();

    private static readonly string[] _allNames = _allKeys.Select(k => k.ToString().ToLower()).ToArray();
    
    private readonly List<string> _argumentPrefixes;

    private readonly List<string> _argumentAndValueDelimiters;

    internal PrioritizedList<CommandLineArgumentMetadata<TEnum>> Metadatas;

    internal List<int> PositionalArgumentMetadataIndexes;
    
    public IList<string> ArgumentPrefixes => _argumentPrefixes;

    public IList<string> ArgumentAndValueDelimiters => _argumentAndValueDelimiters;

    public bool IsConfigured =>
        this._argumentPrefixes.Count > 0 &&
        this._argumentAndValueDelimiters.Count > 0;
    
    public bool OnParseThrowOnUnknownCommandName { get; set; }

    public Parser(IEnumerable<CommandLineArgumentMetadata<TEnum>> metadatas)
    {
        this.Metadatas = new(new CommandLineArgumentMetadataComparer<TEnum>());
        this._argumentPrefixes = new();
        this._argumentAndValueDelimiters = new();
        this.PositionalArgumentMetadataIndexes = new List<int>();
        var positionalMetadataKeys = new List<TEnum?>();
        foreach (var item in metadatas)
        {
            var count = this.Metadatas.Count;
            
            for (var i = 0; i < count; i++)
            {
                // check if the key already has been added.
                if (item.Name.Equals(this.Metadatas[i].Name))
                    throw new ArgumentException($"Key \"{item.Name}\" has already been added.");
                
                // check if the aliases are already claimed or allowed.
                for (var n = 0; n < item.Aliases.Count; n++)
                {
                    if (this.Metadatas[i].Aliases.Contains(item.Aliases[n]))
                    {
                        var message = string.IsNullOrEmpty(item.Aliases[n])
                            ? "Empty strings as aliases are not allowed."
                            : "Duplicate aliases are not permitted.";
                        
                        throw new ArgumentException(message, nameof(metadatas));
                    }
                }
            }

            if (item.UnnamedAliasIndex > -1)
            {
                while (item.UnnamedAliasIndex >= positionalMetadataKeys.Count)
                    positionalMetadataKeys.Add(null);

                var stored = positionalMetadataKeys[item.UnnamedAliasIndex];
                if (stored.HasValue)
                {
                    if (stored.Value.Equals(item.Name))
                        throw new ArgumentException("Attempted to add a positional metadata to an index that is already used.", nameof(metadatas));
                }
                else
                    positionalMetadataKeys[item.UnnamedAliasIndex] = item.Name;
            }

            var meta = new CommandLineArgumentMetadata<TEnum>(item);
            this.Metadatas.InsertOrAdd(meta);
        }

        for (var i = 0; i < this.Metadatas.Count; i++)
            this.Metadatas[i].Priority = i;

        for (var i = 0; i < positionalMetadataKeys.Count; i++)
        {
            if (!positionalMetadataKeys[i].HasValue)
                throw new ArgumentException("Positional arguments must be contiguous.", nameof(metadatas));

            var index = -1;
            for (var m = 0; m < this.Metadatas.Count; m++)
                if (this.Metadatas[m].Name.Equals(positionalMetadataKeys[i].Value))
                {
                    index = m;
                    break;
                }
            
            this.PositionalArgumentMetadataIndexes.Add(index);
        }
    }

    public Parser(params CommandLineArgumentMetadata<TEnum>[] items) :
        this(items as IEnumerable<CommandLineArgumentMetadata<TEnum>>) { }
    
    private SortedList<int, ParserResult<TEnum>> Group(string[] args)
    {
        var result = new SortedList<int, ParserResult<TEnum>>();
        var values = new List<string>();
        TEnum name = default;
        Delegate del = () => { };
        Func<string[]?, object[]?> argConverter = s => null;
        var foundFirst = false;
        foreach (var arg in args)
        {
            if (this.IsNewCommand(arg, out var indexOf))
            {
                var temp = this.Metadatas[indexOf];
                if (foundFirst)
                {
                    var r = new ParserResult<TEnum>(name, del, values.ToArray());
                    result.Add(this.Metadatas[indexOf].Priority, r);
                }

                foundFirst = true;
                name = temp.Name;
                del = temp.Delegate;
                argConverter = temp.Converter;
                values.Clear();
            }
            else
            {
                values.Add(arg);
            }
        }

        return result;
    }

    private bool IsNewCommand(string arg, out int index)
    {
        bool Worker (string name, out int index)
        {
            index = -1;
            for (var i = 0; i < this.Metadatas.Count; i++)
                if (this.Metadatas[i].Aliases.Contains(name, this.Metadatas[i]))
                {
                    index = i;
                    break;
                }
                    
            if (index == -1)
                if (this.OnParseThrowOnUnknownCommandName)
                    throw new ArgumentException($"Unknown command {name} provided.");
                else
                    return false;

            return true;
        }
        
        if (this._argumentPrefixes.Count == 0)
            return Worker(arg, out index);
        
        foreach (var prefix in this._argumentPrefixes)
        {
            if (arg.StartsWith(prefix))
            {
                var name = arg.Remove(0, prefix.Length);
                return Worker(name, out index);
            }
        }

        index = -1;
        return false;
    }

    public IEnumerable<ParserResult<TEnum>> Parse(string[] args)
    {
        /* This should take the array of strings passed into the Main method and return the commands in the order
         * that was set by the priority value of each key during construction. "args" is going to be a collection of
         * strings as defined by .Net. This means that "program.exe --foo bar" will have { "--foo", "bar" } passed in. 
         */

        var result = new PrioritizedList<Prioritizer<ParserResult<TEnum>>>(new PrioritizerComparer<ParserResult<TEnum>>());
        var values = new List<string>();
        TEnum name = default;
        Delegate del = () => { };
        var foundFirst = false;
        int indexOf = -1, priority = 0, unnamedValuePosition = 0;
        var r = default(Prioritizer<ParserResult<TEnum>>);
        foreach (var arg in args)
        {
            if (this.IsNewCommand(arg, out var newIndexOf))
            {
                if (foundFirst)
                {
                    r = new Prioritizer<ParserResult<TEnum>>(priority, new ParserResult<TEnum>(name, del, values.ToArray()));
                    result.InsertOrAdd(r);
                }
                else
                {
                    indexOf = newIndexOf;
                    foundFirst = true;
                }

                var temp = this.Metadatas[newIndexOf];
                indexOf = newIndexOf;
                priority = temp.Priority;
                name = temp.Name;
                del = temp.Delegate;
                values.Clear();
            }
            else
            {
                if (foundFirst)
                    values.Add(arg);
                else
                {
                    if (unnamedValuePosition < this.PositionalArgumentMetadataIndexes.Count)
                    {
                        var meta = this.Metadatas[this.PositionalArgumentMetadataIndexes[unnamedValuePosition]];
                        r = new Prioritizer<ParserResult<TEnum>>(meta.Priority, new ParserResult<TEnum>(meta.Name, meta.Delegate, new [] { arg }));
                        result.InsertOrAdd(r);
                    }
                    
                    unnamedValuePosition++;
                }
            }
        }
        
        if (result.Count > 0 && unnamedValuePosition == 0 || indexOf > -1)
        {
            r = new Prioritizer<ParserResult<TEnum>>(indexOf, new ParserResult<TEnum>(name, del, values.ToArray()));
            result.InsertOrAdd(r);
        }
        else
        {
            if (values.Count > 0)
            {
                for (var i = 0; i < this.PositionalArgumentMetadataIndexes.Count; i++)
                {
                    var meta = this.Metadatas[this.PositionalArgumentMetadataIndexes[unnamedValuePosition]];
                    r = new Prioritizer<ParserResult<TEnum>>(meta.Priority, new ParserResult<TEnum>(meta.Name, meta.Delegate, new [] { args[i] }));
                    result.InsertOrAdd(r);
                }
            }
        }

        return result.Select(v => v.Value);
    }
}