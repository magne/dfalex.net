using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;
using Xunit.Abstractions;

namespace CodeHive.DfaLex.Tests
{
    public class BuilderCacheTest : TestBase
    {
        public BuilderCacheTest(ITestOutputHelper helper)
            : base(helper)
        { }

        [Fact]
        public void Test()
        {
            var cache = new InMemoryBuilderCache();

            var builder = new DfaBuilder<JavaToken>(cache);
            Build(builder);
            Assert.Equal(1, cache.Cache.Count);
            Assert.Equal(0, cache.Hits);

            builder.Clear();
            Build(builder);
            Assert.Equal(1, cache.Cache.Count);
            Assert.Equal(1, cache.Hits);

            builder = new DfaBuilder<JavaToken>(cache);
            Build(builder);
            Assert.Equal(1, cache.Cache.Count);
            Assert.Equal(2, cache.Hits);
        }

        private void Build(DfaBuilder<JavaToken> builder)
        {
            foreach (JavaToken tok in Enum.GetValues(typeof(JavaToken)))
            {
                builder.AddPattern(tok.Pattern(), tok);
            }

            var lang = new HashSet<JavaToken>(Enum.GetValues(typeof(JavaToken)).Cast<JavaToken>());
            var start = builder.Build(lang, null);
            CheckDfa(start, "JavaTest.out.txt");
        }

        private class InMemoryBuilderCache : IBuilderCache
        {
            internal readonly IDictionary<String, byte[]> Cache = new Dictionary<string, byte[]>();
            internal          int                         Hits;

            public object GetCachedItem(string key)
            {
                object ret = null;
                if (Cache.TryGetValue(key, out var bytes))
                {
                    using (var ms = new MemoryStream(bytes))
                    {
                        var bf = new BinaryFormatter();
                        ret = bf.Deserialize(ms);
                    }
                }

                if (ret != null)
                {
                    ++Hits;
                }

                return ret;
            }

            public void MaybeCacheItem(string key, object item)
            {
                using (var ms = new MemoryStream())
                {
                    var bf = new BinaryFormatter();
                    bf.Serialize(ms, item);
                    Cache.Add(key, ms.ToArray());
                }
            }
        }
    }
}
