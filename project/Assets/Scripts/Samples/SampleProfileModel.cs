using R3;
using System.Collections.Generic;

namespace Samples
{
    public class SampleProfileModel
    {
        private static readonly string[] Names = { "Alice", "Bob", "Charlie", "Diana" };

        private int nameIndex = 0;

        public ReactiveProperty<string> UserName { get; } = new(Names[0]);
        public ReactiveProperty<int> Level { get; } = new(1);
        public List<SampleProfileData> Profiles { get; } = CreateProfiles();

        public void NextProfile()
        {
            nameIndex = (nameIndex + 1) % Names.Length;
            UserName.Value = Names[nameIndex];
            Level.Value = nameIndex + 1;
        }

        private static List<SampleProfileData> CreateProfiles()
        {
            var names = new[]
            {
                "Alice", "Bob", "Charlie", "Diana", "Ethan", "Fiona", "George", "Hana",
                "Ian", "Jina", "Kai", "Luna", "Mina", "Noah", "Olivia", "Paul"
            };
            var roles = new[] { "Warrior", "Ranger", "Mage", "Healer", "Guardian" };
            var statuses = new[] { "Online", "Away", "In Match", "Offline" };

            var profiles = new List<SampleProfileData>(100);
            for (var i = 0; i < 100; i++)
            {
                profiles.Add(new SampleProfileData(
                    i + 1,
                    $"{names[i % names.Length]} #{i + 1:000}",
                    1 + (i * 7) % 80,
                    roles[i % roles.Length],
                    statuses[i % statuses.Length]));
            }

            return profiles;
        }
    }

    public sealed class SampleProfileData
    {
        public int Id { get; }
        public string Name { get; }
        public int Level { get; }
        public string Role { get; }
        public string Status { get; }

        public SampleProfileData(int _id, string _name, int _level, string _role, string _status)
        {
            Id = _id;
            Name = _name;
            Level = _level;
            Role = _role;
            Status = _status;
        }
    }
}
