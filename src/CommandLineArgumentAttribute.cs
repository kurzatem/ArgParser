// /*
//    Copyright 2022 CommandLineArgumentAttribute.cs
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

public class CommandLineArgumentAttribute : Attribute
{
    public string Name { get; init; }
    
    public string Description { get; init; }
    
    public Delegate ToDoIfFound { get; init; }
    
    public int Priority { get; init; }
    
    public IReadOnlyList<string> Aliases { get; init; }

    public Type TypeExpected { get; init; }
    
    public Func<string[]?, object[]?> Converter { get; init; }
    
    public StringComparison ComparisonOption { get; init; }

    public CommandLineArgumentAttribute(string name, string description, Delegate toDoIfFound, int priority, IReadOnlyList<string> aliases, Type typeExpected, Func<string[]?, object[]?> converter, StringComparison comparisonOption)
    {
        this.Name = name;
        this.Description = description;
        this.ToDoIfFound = toDoIfFound;
        this.Priority = priority;
        this.Aliases = aliases;
        this.TypeExpected = typeExpected;
        this.Converter = converter;
        this.ComparisonOption = comparisonOption;
    }
}