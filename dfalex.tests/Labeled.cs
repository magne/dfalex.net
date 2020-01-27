namespace CodeHive.DfaLex.Tests
{
    public class Labeled<T>
    {
        public Labeled(T data, string label)
        {
            Data = data;
            Label = label;
        }

        public T Data { get; }
        public string Label { get; }

        public override string ToString()
        {
            return Label;
        }
    }

    public static class LabeledExtensions
    {
        public static Labeled<T> Labeled<T>(this T source, string label) => new Labeled<T>(source, label);
    }
}
