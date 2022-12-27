// /*
//    Copyright 2022 KnownPrimitveParser.cs
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

using System.Dynamic;

namespace ArgParser;

internal readonly struct KnownPrimitiveParser
{
    private readonly Delegate _parser;

    public KnownPrimitiveParser(Delegate parser)
    {
        this._parser = parser ?? throw new ArgumentNullException(nameof(parser));
    }

    public static KnownPrimitiveParser Create<T>(Func<string, T> func)
    {
        return new KnownPrimitiveParser(func);
    }
    
    public object[]? ParseAll(string[]? strings)
    {
        if (strings is null)
            return null;

        var result = new object[strings.Length];
        for (var i = 0; i < strings.Length; i++)
        {
            result[i] = this._parser.DynamicInvoke(strings[i]) ?? throw new InvalidOperationException();
        }

        return result;
    }
}