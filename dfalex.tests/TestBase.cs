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

        public TestBase(ITestOutputHelper helper)
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

        internal void CheckDfa<T>(DfaState<T> start, string resource, bool doStdout)
        {
            string have;
            {
                var printer = new PrettyPrinter<T>();
                var w = new StringWriter();
                printer.Print(w, start);
                have = w.ToString();
            }
            if (doStdout)
            {
                helper.WriteLine(have);
            }

            var want = ReadResource(resource);
            Assert.Equal(want, have);
        }

        internal void PrintDot<T>(DfaState<T> start)
        {
            var printer = new PrettyPrinter<T>();
            var w = new StringWriter();
            printer.PrintDot(w, start);
            helper.WriteLine(w.ToString());
        }

        protected string ReadResource(string resource)
        {
            var type = GetType();
            var assembly = type.GetTypeInfo().Assembly;
            var filename = type.Namespace + "." + resource;

            using (var stream = assembly.GetManifestResourceStream(filename))
            {
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
