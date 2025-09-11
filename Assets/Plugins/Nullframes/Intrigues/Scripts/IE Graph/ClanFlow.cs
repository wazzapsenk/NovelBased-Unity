using System.Collections.Generic;
using System.Linq;
using Nullframes.Intrigues.Graph;

namespace Nullframes.Intrigues
{
    public class ClanFlow
    {
        private static Dictionary<ClanGroupData, List<ClanMemberData>> clans;

        public static IReadOnlyDictionary<ClanGroupData, List<ClanMemberData>> Clans => clans;

        private static void Init()
        {
            clans = new Dictionary<ClanGroupData, List<ClanMemberData>>();
        }

        public static void LoadClans()
        {
            Init();

            foreach (var group in IM.IEDatabase.groupDataList.Where(g => g is ClanGroupData).OfType<ClanGroupData>())
            {
                var members = IM.IEDatabase.nodeDataList.Where(n => n is ClanMemberData && n.GroupId == group.ID).OfType<ClanMemberData>().ToList();
                clans.Add(group, members);
            }
        }
    }
}