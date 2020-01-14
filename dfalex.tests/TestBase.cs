using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace CodeHive.DfaLex.Tests
{
    public class TestBase
    {
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
            if (doStdout)
            {
                helper.WriteLine(have);
            }

            var want = ReadResource(resource);
            Assert.Equal(want, have);
        }

        internal void CheckDfa<T>(DfaState<T> start, string resource, bool doStdout = false)
        {
            var have = PrettyPrinter.Print(start);
            if (doStdout)
            {
                helper.WriteLine(have);
            }

            var want = ReadResource(resource);
            Assert.Equal(want, have);
        }

        internal void PrintDot<T>(Nfa<T> nfa, int start)
        {
            helper.WriteLine(PrettyPrinter.PrintDot(nfa, start));
        }

        internal void PrintDot<T>(DfaState<T> start)
        {
            helper.WriteLine(PrettyPrinter.PrintDot(start));
        }

        protected string ReadResource(string resource)
        {
            var type = GetType();
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
    }
}
