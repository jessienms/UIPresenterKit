using ObservableCollections;

namespace UIPresenterKit.Samples.Model
{
    public sealed class MobListModel
    {
        private readonly ObservableList<MobModel> mobs = new();
        public IReadOnlyObservableList<MobModel> Mobs => mobs;

        internal MobModel First => mobs.Count > 0 ? mobs[0] : null;

        internal void Add(MobModel _mob) => mobs.Add(_mob);

        internal void Remove(MobModel _mob) => mobs.Remove(_mob);
    }
}
