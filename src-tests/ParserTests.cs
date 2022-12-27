// /*
//    Copyright 2022 ParserTests.cs
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

using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Runtime.CompilerServices;

using ArgParser;
using NUnit.Framework.Constraints;

namespace ArgParserTests;

public class ParserTests
{
    private const string _are_not_equal_for_case = " are not equal for case: "; 
    
    private static Delegate _emptyMethod = () => { };

    private static object[] _noArgs = Array.Empty<object>();
    
    /* Outline of what this initial tests are to cover.
     * [done]
     * 1) Throws an argument exception if there are multiple keys inserted during construction.
     *      (Rationale: launch arguments should not have duplicates.)
     * [done]
     * 2) Should sort the allowed arguments according to a custom priorities list.
     *      (Rationale: allows for arguments to be placed in any order, but executed in a specified order.)
     * [done]
     * 3) Should parse arguments and return the appropriate delegate in the previously stated order.
     *      (Rationale: same as stated for sorting according to custom priorities.)
     * [done]
     * 4) Should parse duplicate launch arguments and return the appropriate delegate.
     *      (Rationale: the consuming code should deal with the duplicates.)
     * [done]
     * 5) Should be able to detect the same alias being used across multiple metadatas.
     *      (Rationale: consuming code helper function.)
     * [done]
     * 6) Should be able to work with multiple aliases in a metadata.
     *      (Rationale: basic functionality test for cases like "--count" & "-c" meaning the same thing.)
     * [done]
     * 7) Should be given fewer arguments to parse than the metadatas provided at construction.
     *      (Rationale: a common occurence.)
     * [done]
     * 8) Should parse a set of commands and values combinations such as "--count 5".
     *      (Rationale: this is a common set up to pass in.)
     * [done]
     * 9) Should be able to handle positional arguments that must come before any named arguments. 
     *      (Rationale: allows for a default command that must be used every time, such as a pointing to a config file.)
     * [done]
     * 10) Should be able to handle a list of just positional arguments.
     *      (Rationale: logical extension of #9.)
     */

    private static TestArg[] Control => new[]
    {
        new TestArg(0, 0, TestKeys.One, _emptyMethod, _noArgs),
        new TestArg(1, 1, TestKeys.Two, _emptyMethod, _noArgs),
        new TestArg(2, 2, TestKeys.Three, _emptyMethod, _noArgs),
        new TestArg(3, 3, TestKeys.Four, _emptyMethod, _noArgs)
    };

    private static TestArg[] EnumUnordered => new[]
    {
        new TestArg(0, 0, TestKeys.Two, _emptyMethod, _noArgs),
        new TestArg(1, 1, TestKeys.Four, _emptyMethod, _noArgs),
        new TestArg(2, 2, TestKeys.One, _emptyMethod, _noArgs),
        new TestArg(3, 3, TestKeys.Three, _emptyMethod, _noArgs)
    };

    private static TestArg[] ReversedPriorities => new[]
    {
        new TestArg(3, 3, TestKeys.One, _emptyMethod, _noArgs),
        new TestArg(2, 2, TestKeys.Two, _emptyMethod, _noArgs),
        new TestArg(1, 1, TestKeys.Three, _emptyMethod, _noArgs),
        new TestArg(0, 0, TestKeys.Four, _emptyMethod, _noArgs)
    };

    private static TestArg[] EqualPriorities => new[]
    {
        new TestArg(0, 0, TestKeys.One, _emptyMethod, _noArgs),
        new TestArg(1, 0, TestKeys.Two, _emptyMethod, _noArgs),
        new TestArg(2, 0, TestKeys.Three, _emptyMethod, _noArgs),
        new TestArg(3, 0, TestKeys.Four, _emptyMethod, _noArgs)
    };

    public static IEnumerable DuplicateKeysCases
    {
        get
        {
            object[] ConcatAndConvert(TestArg[] args, bool includeNames, string caseName) =>
                Convert(args.Concat(args).ToArray(), includeNames, caseName);
            
            // Test sig: (string caseName, (TestKeys, int)[] expected, CommandLineArgumentMetadata<TestKeys>[] ctorArgs)
            
            yield return ConcatAndConvert(Control, false, nameof(Control));
            yield return ConcatAndConvert(EnumUnordered, false, nameof(EnumUnordered));
            yield return ConcatAndConvert(ReversedPriorities, false, nameof(ReversedPriorities));
            yield return ConcatAndConvert(EqualPriorities, false, nameof(EqualPriorities));
        }
    }

    public static IEnumerable SortInputsCases
    {
        get
        {
            // Test sig: (string caseName, (TestKeys, int)[] expected, CommandLineArgumentMetadata<TestKeys>[] ctorArgs)
            
            yield return Convert(Control, false, nameof(Control));
            yield return Convert(EnumUnordered, false, nameof(EnumUnordered));
            yield return Convert(ReversedPriorities, false, nameof(ReversedPriorities));
            yield return Convert(EqualPriorities, false, nameof(EqualPriorities));
        }
    }

    public static IEnumerable ParseInOrderCases
    {
        get
        {
            // Test sig: (string caseName, (TestKeys, int)[] expected, CommandLineArgumentMetadata<TestKeys>[] ctorArgs, string[] parseArgs)
            
            yield return Convert(Control, true, nameof(Control));
            yield return Convert(EnumUnordered, true, nameof(EnumUnordered));
            yield return Convert(ReversedPriorities, true, nameof(ReversedPriorities));
            yield return Convert(EqualPriorities, true, nameof(EqualPriorities));

            var result = Convert(Control, true, nameof(Control) + ", but just one.");
            var expected = result[1] as (TestKeys, int)[];
            var ctorArgs = result[2] as CommandLineArgumentMetadata<TestKeys>[];
            var parseArgs = result[3] as string[];

            yield return new object[] { result[0] as string, new [] { expected[0] }, new [] { ctorArgs[0] } , new[] { parseArgs[0] } };
        }
    }

    public static IEnumerable ParseInOrderWithDuplicatesCases
    {
        get
        {
            object[] DoubleUpConversion(TestArg[] args, string caseName)
            {
                var result = Convert(args, true, caseName);
                var casted = ((TestKeys, int)[])result[1];
                var tempE = new (TestKeys, int)[casted.Length * 2];
                var increment = false;
                var c = 0;
                for (var i = 0; i < tempE.Length; i++)
                {
                    tempE[i] = casted[c];
                    if (increment)
                        c++;

                    increment = !increment;
                }

                var tempC = (CommandLineArgumentMetadata<TestKeys>[])result[2];

                var tempP = (string[])result[3];
                tempP = tempP.Concat(tempP).ToArray();

                caseName = (string)result[0] + "WithDuplicates";

                // Test sig: (string caseName, (TestKeys, int)[] expected, CommandLineArgumentMetadata<TestKeys>[] ctorArgs, string[] parseArgs)
                
                return new object[] { caseName, tempE, tempC, tempP };
            }

            yield return DoubleUpConversion(Control, nameof(Control));
            yield return DoubleUpConversion(EnumUnordered, nameof(Control));
            yield return DoubleUpConversion(ReversedPriorities, nameof(Control));
            yield return DoubleUpConversion(EqualPriorities, nameof(Control));
        }
    }

    public static IEnumerable DuplicateAliasesCases
    {
        get
        {
            var masterList = new[] { "One", "one", "A" };
            var aliases = new List<string>{ "A" };
            
            var a = new CommandLineArgumentMetadata<TestKeys>(TestKeys.One, _emptyMethod, masterList, 0, EmptyConverter);
            var b = new CommandLineArgumentMetadata<TestKeys>(TestKeys.Two, _emptyMethod, aliases, 1, EmptyConverter);

            // Test sig: (string caseName, CommandLineArgumentMetadata<TestKeys>[] ctorArgs)
            yield return new object[] { "1 of 1 aliases in b.", new [] { a, b } };
            
            aliases.Insert(0, "two");
            b = new CommandLineArgumentMetadata<TestKeys>(TestKeys.Two, _emptyMethod, aliases, 1, EmptyConverter);
            yield return new object[] { "1 of 2 aliases in b.", new [] { a, b } };
            
            aliases.Insert(0, "Two");
            b = new CommandLineArgumentMetadata<TestKeys>(TestKeys.Two, _emptyMethod, aliases, 1, EmptyConverter);
            yield return new object[] { "1 of 3 aliases in b.", new [] { a, b } };
            
            aliases.Clear();
            aliases.AddRange(new []{ "Two", "A", "two" });
            b = new CommandLineArgumentMetadata<TestKeys>(TestKeys.Two, _emptyMethod, aliases, 1, EmptyConverter);
            yield return new object[] { "Middle of 3 aliases in b.", new [] { a, b } };
        }
    }

    public static IEnumerable MultipleAliasesCases
    {
        get
        {
            ParserResult<TestKeys>[] MetadataToResults(CommandLineArgumentMetadata<TestKeys> meta, int count)
            {
                var results = new ParserResult<TestKeys>[count];
                for (var i = 0; i < count; i++)
                {
                    results[i] = new ParserResult<TestKeys>(meta.Name, meta.Delegate, null);
                }
                
                return results;
            }
            
            // This can get away with having 2 metadata. One with the aliases. Then the parse args can just have duplicates.
            CommandLineArgumentMetadata<TestKeys> a = new TestArg(0, 0, TestKeys.One, _emptyMethod, Array.Empty<object>());
            var b = new CommandLineArgumentMetadata<TestKeys>(TestKeys.Two, _emptyMethod, new[] { "Two", "two", "TWO", "2" }, 1, KnownPrimitives.Int);

            const string suffix = " has multiple aliases.";

            var expected = MetadataToResults(b, 4);
            // Test sig: (string caseName, ParserResult<TestKeys>[] expected, CommandLineArgumentMetadata<TestKeys>[] ctorArgs, string[] parseArgs)
            yield return new object[] { "\"Two\"" + suffix, expected, new[] { a, b }, b.Aliases.ToArray() };

            var c = new CommandLineArgumentMetadata<TestKeys>(TestKeys.Three, _emptyMethod, new[] { "Three", "three", "THREE", "3" }, 2, KnownPrimitives.Int);
            expected = expected.Concat(MetadataToResults(c, 4)).ToArray();
            yield return new object[] { "\"Two\" and \"Three\"" + suffix, expected, new[] { a, b, c }, b.Aliases.Concat(c.Aliases).ToArray() };
        }
    }

    public static IEnumerable UnusedMetadataCases
    {
        get
        {
            var a = new CommandLineArgumentMetadata<TestKeys>(TestKeys.One, _emptyMethod, new[] { "One" }, 0, KnownPrimitives.Byte);
            var b = new CommandLineArgumentMetadata<TestKeys>(TestKeys.Four, _emptyMethod, new[] { "Four" }, 1, KnownPrimitives.Byte);

            var expected = new [] { new ParserResult<TestKeys>(TestKeys.Four, _emptyMethod, Array.Empty<string>()) };
            var parseArgs = new[] { "Four" };
            
            // Test sig: (string caseName, ParserResult<TestKeys>[] expected, CommandLineArgumentMetadata<TestKeys>[] ctorArgs, string[] parseArgs)
            yield return new object[] { "Unused metadata case.", expected, new[] { a, b }, parseArgs };
        }
    }

    public static IEnumerable CommandValuePairCases
    {
        get
        {
            object[] ConvertAndAppendValue(TestArg[] args, string caseName)
            {
                var objArrs = Convert(args, true, caseName);
                var converts = (CommandLineArgumentMetadata<TestKeys>[])objArrs[2];

                var cE = ((TestKeys, int)[])objArrs[1];
                
                var expected = new ParserResult<TestKeys>[cE.Length];
                var parserArgs = new string[objArrs.Length * 2];

                for (var i = 0; i < cE.Length; i++)
                {
                    var key = cE[i].Item1;
                    parserArgs[i * 2] = key.ToString();
                    parserArgs[i * 2 + 1] = cE[i].Item2.ToString();
                    expected[i] = new ParserResult<TestKeys>(key, _emptyMethod, new string[] { cE[i].Item2.ToString() });
                }

                // Test sig: (string caseName, ParserResult<TestKeys>[] expected, CommandLineArgumentMetadata<TestKeys>[] ctorArgs, string[] parseArgs)
                return new object[] { caseName, expected, converts, parserArgs };
            }
            
            yield return ConvertAndAppendValue(Control, nameof(Control));
            yield return ConvertAndAppendValue(EnumUnordered, nameof(Control));
            yield return ConvertAndAppendValue(ReversedPriorities, nameof(Control));
            yield return ConvertAndAppendValue(EqualPriorities, nameof(Control));
        }
    }

    public static IEnumerable UnnamedAndNamedCommandCases
    {
        get
        {
            // Test sig: (string caseName, ParserResult<TestKeys>[] expected, CommandLineArgumentMetadata<TestKeys>[] ctorArgs, string[] parseArgs)
            
            // these cases should have 1 or more arguments before any named arguments
            // So something like "ping 5 8.8.8.8 --delay 1000ms" where ping is the program names
            // So let's just throw "5" and "8.8.8.8" as the first 2 arguments before the normal ones.
            // The metadatas have a separate positional index property.

            var a = new CommandLineArgumentMetadata<TestKeys>(TestKeys.One, _emptyMethod, new[] { "One" }, 0, KnownPrimitives.Byte);
            var b = new CommandLineArgumentMetadata<TestKeys>(TestKeys.Two, _emptyMethod, new[] { "Two" }, 1, KnownPrimitives.Byte, 1);
            var c = new CommandLineArgumentMetadata<TestKeys>(TestKeys.Three, _emptyMethod, new[] { "Three" }, 2, KnownPrimitives.Byte, 0);

            var expected = new []
            {
                new ParserResult<TestKeys>(a.Name, a.Delegate, Array.Empty<string>()),
                new ParserResult<TestKeys>(b.Name, b.Delegate, new [] { "2.5f" }),
                new ParserResult<TestKeys>(c.Name, c.Delegate, new [] { "2" })
            };
            
            var ctorArgs = new [] { a, b, c };
            var parseArgs = new [] { "2", "2.5f", a.Name.ToString() };

            yield return new object[] { "The first case.", expected, ctorArgs, parseArgs };
        }
    }

    public static IEnumerable UnnamedOnlyCommandCases
    {
        get
        {
            // these cases should have 1 or more arguments before any named arguments
            // So something like "ping 5 8.8.8.8" where ping is the program name
            // So let's just throw 1, 2 and 3 random values at it to see if it can handle it.

            var expected = new List<ParserResult<TestKeys>>();
            var ctorArgs = new List<CommandLineArgumentMetadata<TestKeys>>();
            var parseArgs = new List<string>();
            
            expected.Add(new ParserResult<TestKeys>(TestKeys.One, _emptyMethod, new [] { "1" }));
            var temp = new TestArg(0, 0, TestKeys.One, _emptyMethod, _noArgs);
            ctorArgs.Add(new CommandLineArgumentMetadata<TestKeys>(temp.Key, temp.Del, new [] { temp.Key.ToString() }, temp.Priority, KnownPrimitives.Int, 0));
            parseArgs.Add("1");

            // Test sig: (string caseName, ParserResult<TestKeys>[] expected, CommandLineArgumentMetadata<TestKeys>[] ctorArgs, string[] parseArgs)
            yield return new object[] { "1 arg", expected.ToArray(), ctorArgs.ToArray(), parseArgs.ToArray() };
            
            expected.Add(new ParserResult<TestKeys>(TestKeys.Two, _emptyMethod, new [] { "String" }));
            temp = new TestArg(1, 1, TestKeys.Two, _emptyMethod, _noArgs);
            ctorArgs.Add(new CommandLineArgumentMetadata<TestKeys>(temp.Key, temp.Del, new [] { temp.Key.ToString() }, temp.Priority, KnownPrimitives.String, 1));
            parseArgs.Add("String");
            
            yield return new object[] { "2 args", expected.ToArray(), ctorArgs.ToArray(), parseArgs.ToArray() };
            
            expected.Add(new ParserResult<TestKeys>(TestKeys.Three, _emptyMethod, new [] { "2.5f" }));
            temp = new TestArg(2, 2, TestKeys.Three, _emptyMethod, _noArgs);
            ctorArgs.Add(new CommandLineArgumentMetadata<TestKeys>(temp.Key, temp.Del, new [] { temp.Key.ToString() }, temp.Priority, KnownPrimitives.Single, 2));
            parseArgs.Add("2.5f");
            
            yield return new object[] { "3 args", expected.ToArray(), ctorArgs.ToArray(), parseArgs.ToArray() };
        }
    }

    /// <summary>
    /// Converts an array of <see cref="TestArg"/> values into an object array for use as a basis for creating test cases.
    /// </summary>
    /// <param name="args">The array of <see cref="TestArg"/> values to convert.</param>
    /// <param name="includeNames">Whether to include <paramref name="args"/> names in the returned array.</param>
    /// <param name="caseName">The name of the test case to include.</param>
    /// <returns>An array in this order: { <paramref name="caseName"/>, a (TestKeys, int) tuple array, a <see cref="CommandLineArgumentMetadata{TEnum}"/> object array, (optional based on <paramref name="includeNames"/>) an array of the argument names } </returns>
    private static object[] Convert(TestArg[] args, bool includeNames, string caseName)
    {
        var expected = new (TestKeys, int)[args.Length];
        var ctorArgs = new CommandLineArgumentMetadata<TestKeys>[args.Length];
        var names = new string[args.Length];
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            expected[i] = new(arg.Key, arg.ExpectedIndex);
            ctorArgs[i] = new(arg.Key, arg.Del, new [] { arg.Key.ToString() }, arg.Priority, EmptyConverter);

            if (includeNames)
                names[i] = arg.Key.ToString().ToLower();
        }

        Array.Sort(expected, (x, y) => x.Item2 < y.Item2 ? -1 : x.Item2 > y.Item2 ? 1 : 0);

        return includeNames
            ? new object[] { caseName, expected, ctorArgs, names }
            : new object[] { caseName, expected, ctorArgs };
    }

    private static object[]? EmptyConverter(string[]? strings) => null;

    private static void TestHelper(string caseName, (TestKeys, int)[] expected, CommandLineArgumentMetadata<TestKeys>[] ctorArgs, string[] parseArgs)
    {
        var testee = new Parser<TestKeys>(ctorArgs);

        var actual = testee.Parse(parseArgs).ToArray();
        
        Assert.That(actual, Has.Length.EqualTo(expected.Length));

        for (var i = 0; i < actual.Length; i++)
        {
            Assert.That(actual[i].ArgumentName, Is.EqualTo(expected[i].Item1), $"Keys{_are_not_equal_for_case}{caseName}.");
        }
    }
    
    private static void TestHelper(string caseName, ParserResult<TestKeys>[] expected, CommandLineArgumentMetadata<TestKeys>[] ctorArgs, string[] parseArgs)
    {
        var testee = new Parser<TestKeys>(ctorArgs);

        var actual = testee.Parse(parseArgs).ToArray();
        
        Assert.That(actual, Has.Length.EqualTo(expected.Length));

        for (var i = 0; i < actual.Length; i++)
        {
            Assert.Multiple(() =>
            {
                Assert.That(actual[i].ArgumentName, Is.EqualTo(expected[i].ArgumentName), $"Keys{_are_not_equal_for_case}{caseName}.");
                Assert.That(actual[i].DelegateParameters, Is.EqualTo(expected[i].DelegateParameters), $"Values{_are_not_equal_for_case}{caseName}.");
            });
        }
    }

    [Test]
    [TestCaseSource(nameof(DuplicateKeysCases))]
    [Order(1)]
    public void ShouldThrowWithDuplicateKeys(string caseName, (TestKeys, int)[] expected, CommandLineArgumentMetadata<TestKeys>[] ctorArgs)
    {
        Assert.That(() => new Parser<TestKeys>(ctorArgs), Throws.ArgumentException, $"Either no ArgumentException was thrown or no duplicate keys are present in {nameof(ctorArgs)}.");
    }
    
    [Test]
    [TestCaseSource(nameof(SortInputsCases))]
    [Order(2)]
    public void ShouldSortInputs(string caseName, (TestKeys, int)[] expected, CommandLineArgumentMetadata<TestKeys>[] ctorArgs)
    {
        var testee = new Parser<TestKeys>(ctorArgs);

        var items = testee.Metadatas;

        Assert.That(items.Count, Is.EqualTo(expected.Length));

        for (var i = 0; i < items.Count; i++)
        {
            Assert.Multiple(() =>
            {
                Assert.That(items[i].Name, Is.EqualTo(expected[i].Item1), $"Keys{_are_not_equal_for_case}{caseName}.");
                Assert.That(i, Is.EqualTo(expected[i].Item2), $"Index does not match for case: {caseName}.");
            });
        }
    }

    [Test]
    [TestCaseSource(nameof(ParseInOrderCases))]
    [Order(3)]
    public void ShouldReturnDelegatesInOrder(string caseName, (TestKeys, int)[] expected, CommandLineArgumentMetadata<TestKeys>[] ctorArgs, string[] parseArgs)
    {
        TestHelper(caseName, expected, ctorArgs, parseArgs);
    }

    [Test]
    [TestCaseSource(nameof(ParseInOrderWithDuplicatesCases))]
    [Order(4)]
    public void ShouldReturnDelegatesInOrderWithDuplicates(string caseName, (TestKeys, int)[] expected, CommandLineArgumentMetadata<TestKeys>[] ctorArgs, string[] parseArgs)
    {
        TestHelper(caseName, expected, ctorArgs, parseArgs);
    }

    [Test]
    [TestCaseSource(nameof(DuplicateAliasesCases))]
    [Order(5)]
    public void ShouldDetectIdenticalAliasInMultipleMetadatas(string caseName, CommandLineArgumentMetadata<TestKeys>[] ctorArgs)
    {
        Assert.That(() => new Parser<TestKeys>(ctorArgs), Throws.ArgumentException, "Constructor did not catch duplicate aliases.");
    }

    [Test]
    [TestCaseSource(nameof(MultipleAliasesCases))]
    [Order(6)]
    public void ShouldReturnTheSameDelegateDueToMultipleAliasesBeingUsed(string caseName, ParserResult<TestKeys>[] expected, CommandLineArgumentMetadata<TestKeys>[] ctorArgs, string[] parseArgs)
    {
        TestHelper(caseName, expected, ctorArgs, parseArgs);
    }

    [Test]
    [TestCaseSource(nameof(UnusedMetadataCases))]
    [Order(7)]
    public void ShouldNotReturnedUnusedMetadata(string caseName, ParserResult<TestKeys>[] expected, CommandLineArgumentMetadata<TestKeys>[] ctorArgs, string[] parseArgs)
    {
        TestHelper(caseName, expected, ctorArgs, parseArgs);
    }

    [Test]
    [TestCaseSource(nameof(CommandValuePairCases))]
    [Order(8)]
    public void ShouldCombineCommandsAndValues(string caseName, ParserResult<TestKeys>[] expected, CommandLineArgumentMetadata<TestKeys>[] ctorArgs, string[] parseArgs)
    {
        TestHelper(caseName, expected, ctorArgs, parseArgs);
    }

    [Test]
    [TestCaseSource(nameof(UnnamedAndNamedCommandCases))]
    [Order(9)]
    public void ShouldReturnDelegatesWithPositionalAndAliasedArguments(string caseName, ParserResult<TestKeys>[] expected, CommandLineArgumentMetadata<TestKeys>[] ctorArgs, string[] parseArgs)
    {
        TestHelper(caseName, expected, ctorArgs, parseArgs);
    }

    [Test]
    [TestCaseSource(nameof(UnnamedOnlyCommandCases))]
    [Order(10)]
    public void ShouldReturnDelegateForPositionalArguments(string caseName, ParserResult<TestKeys>[] expected, CommandLineArgumentMetadata<TestKeys>[] ctorArgs, string[] parseArgs)
    {
        TestHelper(caseName, expected, ctorArgs, parseArgs);
    }

    public record struct TestArg(int ExpectedIndex, int Priority, TestKeys Key, Delegate Del, object[]? DelArgs)
    {
        public readonly int ExpectedIndex = ExpectedIndex, Priority = Priority;
        
        public readonly TestKeys Key = Key;
        
        public readonly Delegate Del = Del;
        
        public readonly object[]? DelArgs = DelArgs;

        public static implicit operator ((TestKeys, int), (TestKeys, Delegate, object[]?, int))(TestArg arg)
        {
            return new((arg.Key, arg.ExpectedIndex), (arg.Key, arg.Del, arg.DelArgs, arg.Priority));
        }

        public static implicit operator CommandLineArgumentMetadata<TestKeys>(TestArg arg)
        {
            IEnumerable<string> AliasBuilder(TestKeys key)
            {
                var k = key.ToString();
                yield return k;
                var l = k.ToLower();
                yield return l;

                yield return "-" + k;
                yield return "-" + l;

                yield return "--" + k;
                yield return "--" + l;
            }
            
            return new CommandLineArgumentMetadata<TestKeys>(arg.Key, arg.Del, AliasBuilder(arg.Key).ToArray(), arg.Priority, EmptyConverter);
        }
    }

    public enum TestKeys
    {
        One,
        Two,
        Three,
        Four
    }
}