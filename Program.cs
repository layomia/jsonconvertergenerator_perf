using System;
using System.Buffers;
using System.Text;
using System.Text.Json.Serialization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace JsonConverterGenerator
{
    [MemoryDiagnoser]
    [MinColumn, MaxColumn]
    [HtmlExporter]
    [GenericTypeArguments(typeof(LoginViewModel))]
    [GenericTypeArguments(typeof(Location))]
    [GenericTypeArguments(typeof(IndexViewModel))]
    [GenericTypeArguments(typeof(MyEventsListerViewModel))]
    [GenericTypeArguments(typeof(CollectionsOfPrimitives))]
    public class DeserializationBenchmark<T>
    {
        private readonly T value;
        private string serialized;
        private JsonConverter<T> aotConverter;

        public DeserializationBenchmark() => value = DataGenerator.Generate<T>();

        [GlobalSetup(Target = nameof(Jil_))]
        public void SerializeJil()
        {
            serialized = Jil.JSON.Serialize<T>(value, Jil.Options.ISO8601);

            Jil_(); // workaround for https://github.com/dotnet/BenchmarkDotNet/issues/837
        }

        [GlobalSetup(Target = nameof(JsonNet_))]
        public void SerializeJsonNet() => serialized = Newtonsoft.Json.JsonConvert.SerializeObject(value);

        [GlobalSetup(Target = nameof(Utf8Json_))]
        public void SerializeUtf8Json_() => serialized = Utf8Json.JsonSerializer.ToJsonString(value);

        [GlobalSetup(Target = nameof(SystemTextJson_))]
        public void SerializeSystemTextJson_() => serialized = System.Text.Json.JsonSerializer.Serialize(value);

        [GlobalSetup(Target = nameof(SystemTextJson_AOT))]
        public void SerializeSystemTextJson_AOT()
        {
            serialized = System.Text.Json.JsonSerializer.Serialize(value);

            if (typeof(T) == typeof(LoginViewModel))
            {
                aotConverter = (JsonConverter<T>)(object)JsonConverterForLoginViewModel.Instance;
            }
            else if (typeof(T) == typeof(Location))
            {
                aotConverter = (JsonConverter<T>)(object)JsonConverterForLocation.Instance;
            }
            else if (typeof(T) == typeof(IndexViewModel))
            {
                aotConverter = (JsonConverter<T>)(object)JsonConverterForIndexViewModel.Instance;
            }
            else if (typeof(T) == typeof(MyEventsListerViewModel))
            {
                aotConverter = (JsonConverter<T>)(object)JsonConverterForMyEventsListerViewModel.Instance;
            }
            else if (typeof(T) == typeof(CollectionsOfPrimitives))
            {
                aotConverter = (JsonConverter<T>)(object)JsonConverterForCollectionsOfPrimitives.Instance;
            }
        }

        [Benchmark(Description = "Jil")]
        public T Jil_() => Jil.JSON.Deserialize<T>(serialized, Jil.Options.ISO8601);

        [Benchmark(Description = "JSON.NET")]
        public T JsonNet_() => Newtonsoft.Json.JsonConvert.DeserializeObject<T>(serialized);

        [Benchmark(Description = "Utf8Json")]
        public T Utf8Json_() => Utf8Json.JsonSerializer.Deserialize<T>(serialized);

        [Benchmark(Description = "SystemTextJson")]
        public T SystemTextJson_() => System.Text.Json.JsonSerializer.Deserialize<T>(serialized);

        [Benchmark(Description = "SystemTextJson_AOT")]
        public T SystemTextJson_AOT()
        {
            const long ArrayPoolMaxSizeBeforeUsingNormalAlloc = 1024 * 1024;

            // In the worst case, a single UTF-16 character could be expanded to 3 UTF-8 bytes.
            // Only surrogate pairs expand to 4 UTF-8 bytes but that is a transformation of 2 UTF-16 characters goign to 4 UTF-8 bytes (factor of 2).
            // All other UTF-16 characters can be represented by either 1 or 2 UTF-8 bytes.
            const int MaxExpansionFactorWhileTranscoding = 3;

            byte[] tempArray = null;

            // For performance, avoid obtaining actual byte count unless memory usage is higher than the threshold.
            Span<byte> utf8 = serialized.Length <= (ArrayPoolMaxSizeBeforeUsingNormalAlloc / MaxExpansionFactorWhileTranscoding) ?
            // Use a pooled alloc.
                tempArray = ArrayPool<byte>.Shared.Rent(serialized.Length * MaxExpansionFactorWhileTranscoding) :
                // Use a normal alloc since the pool would create a normal alloc anyway based on the threshold (per current implementation)
                // and by using a normal alloc we can avoid the Clear().
                new byte[Encoding.UTF8.GetByteCount(serialized.AsSpan())];

            int actualByteCount = Encoding.UTF8.GetBytes(serialized.AsSpan(), utf8);
            utf8 = utf8.Slice(0, actualByteCount);

            var reader = new System.Text.Json.Utf8JsonReader(utf8);

            if (reader.Read())
            {
                T returnValue = aotConverter.Read(ref reader, typeof(T), null);

                if (tempArray != null)
                {
                    utf8.Clear();
                    ArrayPool<byte>.Shared.Return(tempArray);
                }

                return returnValue;
            }

            return default;
        }
    }

    [MemoryDiagnoser]
    [MinColumn, MaxColumn]
    [HtmlExporter]
    [GenericTypeArguments(typeof(LoginViewModel))]
    [GenericTypeArguments(typeof(Location))]
    [GenericTypeArguments(typeof(IndexViewModel))]
    [GenericTypeArguments(typeof(MyEventsListerViewModel))]
    [GenericTypeArguments(typeof(CollectionsOfPrimitives))]
    public class SerializationBenchmark<T>
    {
        private readonly T value;

        private JsonConverter<T> aotConverter;

        public SerializationBenchmark() => value = DataGenerator.Generate<T>();

        [GlobalSetup(Target = nameof(Jil_))]
        public void WarmupJil() => Jil_(); // workaround for https://github.com/dotnet/BenchmarkDotNet/issues/837

        [GlobalSetup(Target = nameof(SystemTextJson_AOT_))]
        public void SerializeSystemTextJson_AOT()
        {
            if (typeof(T) == typeof(LoginViewModel))
            {
                aotConverter = (JsonConverter<T>)(object)JsonConverterForLoginViewModel.Instance;
            }
            else if (typeof(T) == typeof(Location))
            {
                aotConverter = (JsonConverter<T>)(object)JsonConverterForLocation.Instance;
            }
            else if (typeof(T) == typeof(IndexViewModel))
            {
                aotConverter = (JsonConverter<T>)(object)JsonConverterForIndexViewModel.Instance;
            }
            else if (typeof(T) == typeof(MyEventsListerViewModel))
            {
                aotConverter = (JsonConverter<T>)(object)JsonConverterForMyEventsListerViewModel.Instance;
            }
            else if (typeof(T) == typeof(CollectionsOfPrimitives))
            {
                aotConverter = (JsonConverter<T>)(object)JsonConverterForCollectionsOfPrimitives.Instance;
            }
        }

        [Benchmark(Description = "Jil")]
        public string Jil_() => Jil.JSON.Serialize<T>(value, Jil.Options.ISO8601);

        [Benchmark(Description = "JSON.NET")]
        public string JsonNet_() => Newtonsoft.Json.JsonConvert.SerializeObject(value);

        [Benchmark(Description = "Utf8Json")]
        public string Utf8Json_() => Utf8Json.JsonSerializer.ToJsonString(value);

        [Benchmark(Description = "SystemTextJson")]
        public string SystemTextJson_() => System.Text.Json.JsonSerializer.Serialize(value);

        [Benchmark(Description = "SystemTextJson_AOT")]
        public string SystemTextJson_AOT_()
        {
            const int DefaultBufferSize = 16384;

            string result;

            using (var output = new PooledByteBufferWriter(DefaultBufferSize))
            {
                using (var writer = new System.Text.Json.Utf8JsonWriter(output))
                {
                    aotConverter.Write(writer, value, null);
                }

                result = Encoding.UTF8.GetString(output.WrittenMemory.Span);
            }

            return result;
        }
    }

    class Program
    {
        static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
