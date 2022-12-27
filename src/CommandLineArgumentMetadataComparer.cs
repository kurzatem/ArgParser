// /*
//    Copyright 2022 CommandLineArgumentMetadataComparer.cs
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

public class CommandLineArgumentMetadataComparer<TEnum> : Comparer<CommandLineArgumentMetadata<TEnum>>
    where TEnum: Enum
{
    public override int Compare(CommandLineArgumentMetadata<TEnum>? x, CommandLineArgumentMetadata<TEnum>? y)
    {
        if (x != null)
            return y != null ? Comparer<int>.Default.Compare(x.Priority, y.Priority) : 1;

        return y != null ? -1 : 0;
    }
}