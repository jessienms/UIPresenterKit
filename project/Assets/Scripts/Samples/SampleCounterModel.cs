using R3;

namespace Samples
{
    public class SampleCounterModel
    {
        public ReactiveProperty<int> Count { get; } = new(0);

        public void Increment() => Count.Value++;
        public void Reset() => Count.Value = 0;
    }
}
