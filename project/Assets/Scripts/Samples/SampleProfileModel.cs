using R3;

namespace Samples
{
    public class SampleProfileModel
    {
        static readonly string[] Names = { "Alice", "Bob", "Charlie", "Diana" };

        int nameIndex = 0;

        public ReactiveProperty<string> UserName { get; } = new(Names[0]);
        public ReactiveProperty<int> Level { get; } = new(1);

        public void NextProfile()
        {
            nameIndex = (nameIndex + 1) % Names.Length;
            UserName.Value = Names[nameIndex];
            Level.Value = nameIndex + 1;
        }
    }
}
