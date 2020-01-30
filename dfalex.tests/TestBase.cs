using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit.Abstractions;

namespace CodeHive.DfaLex.Tests
{
    public class TestBase
    {
        private static readonly Regex ResourcePattern = new Regex(@"^(?<path>.+?)(?:#(?<section>.+))?$", System.Text.RegularExpressions.RegexOptions.Compiled);

        private static readonly Regex SectionPattern = new Regex(@"\s*^\[(?<section>.+?)\]\s+(?<content>[^[]*)",
            System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.Multiline);

        private static readonly IDictionary<string, string> Resources = new Dictionary<string, string>();

        private readonly ITestOutputHelper helper;

        protected TestBase(ITestOutputHelper helper)
        {
            this.helper = helper;
        }

        internal int CountStates<T>(params DfaState<T>[] starts)
        {
            var togo = new Queue<DfaState<T>>();
            var checkSet = new HashSet<DfaState<T>>();
            foreach (var start in starts)
            {
                if (checkSet.Add(start))
                {
                    togo.Enqueue(start);
                }
            }

            while (togo.Any())
            {
                var scanst = togo.Dequeue();
                scanst.EnumerateTransitions((c1, c2, newstate) =>
                {
                    if (checkSet.Add(newstate))
                    {
                        togo.Enqueue(newstate);
                    }
                });
            }

            return checkSet.Count;
        }

        internal void CheckNfa<T>(Nfa<T> nfa, int start, string resource, bool doStdout = false)
        {
            var have = PrettyPrinter.Print(nfa, start);
            CheckStates(have, resource, doStdout);
        }

        internal void CheckDfa<T>(RawDfa<T> dfa, string resource, bool doStdout = false)
        {
            var have = PrettyPrinter.Print(dfa);
            CheckStates(have, resource, doStdout);
        }

        internal void CheckDfa<T>(DfaState<T> start, string resource, bool doStdout = false)
        {
            var have = PrettyPrinter.Print(start);
            CheckStates(have, resource, doStdout);
        }

        private void CheckStates(string states, string resource, bool doStdout)
        {
            if (doStdout)
            {
                helper.WriteLine(states);
            }

            var expected = ReadResource(resource);
            states.Should().Be(expected);
        }

        internal void PrintDot<T>(Nfa<T> nfa, int start)
        {
            helper.WriteLine(PrettyPrinter.PrintDot(nfa, start));
        }

        internal void PrintDot<T>(DfaState<T> start)
        {
            helper.WriteLine(PrettyPrinter.PrintDot(start));
        }

        internal void PrintDot<T>(RawDfa<T> rawDfa)
        {
            helper.WriteLine(PrettyPrinter.PrintDot(rawDfa));
        }

        protected string ReadResource(string resource)
        {
            if (!Resources.TryGetValue(resource, out var result))
            {
                var match = ResourcePattern.Match(resource);
                if (!match.Success)
                {
                    throw new ArgumentException(nameof(resource), $"Could not parse: {resource}");
                }

                var path = match.Groups["path"].Value;
                var section = match.Groups["section"];
                if (!Resources.TryGetValue(path, out result))
                {
                    result = ReadAssemblyResource(path);
                    Resources[path] = result ?? throw new InvalidOperationException($"Could not find resource: {resource}");

                    if (section.Success)
                    {
                        SplitInSections(result, path);
                    }
                }

                if (section.Success && !Resources.TryGetValue(resource, out result))
                {
                    throw new InvalidOperationException($"Could not find section '{section.Value}' in resource: {path}");
                }
            }

            return result;
        }

        private static string ReadAssemblyResource(string resource)
        {
            var type = typeof(TestBase);
            var assembly = type.GetTypeInfo().Assembly;
            var filename = type.Namespace + "." + resource;

            using var stream = assembly.GetManifestResourceStream(filename);
            if (stream == null)
            {
                return null;
            }

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private static void SplitInSections(string resource, string path)
        {
            var matches = SectionPattern.Matches(resource);
            foreach (Match match in matches)
            {
                var section = match.Groups["section"].Value;
                var content = match.Groups["content"].Value.Trim(' ', '\t', '\n', '\r') + "\n";
                Resources.Add($"{path}#{section}", content);
            }
        }
    }
}
