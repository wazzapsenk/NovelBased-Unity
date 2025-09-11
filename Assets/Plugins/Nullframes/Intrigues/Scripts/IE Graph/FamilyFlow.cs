using System.Collections.Generic;
using System.Linq;
using Nullframes.Intrigues.Graph;

namespace Nullframes.Intrigues
{
    public class FamilyFlow
    {
        public static Dictionary<string, List<FamilyMemberData>> Parents;
        public static Dictionary<string, List<FamilyMemberData>> Childs;
        public static Dictionary<string, List<FamilyMemberData>> Partners;
        public static Dictionary<FamilyGroupData, List<FamilyMemberData>> Families;
        private static Dictionary<string, List<FamilyMemberData>> memberNodes;
        private static List<FamilyMemberData> nodes;

        private static void Init()
        {
            Parents = new Dictionary<string, List<FamilyMemberData>>();
            Childs = new Dictionary<string, List<FamilyMemberData>>();
            Partners = new Dictionary<string, List<FamilyMemberData>>();
            memberNodes = new Dictionary<string, List<FamilyMemberData>>();
            Families = new Dictionary<FamilyGroupData, List<FamilyMemberData>>();
            nodes = new List<FamilyMemberData>();

            foreach (var group in IM.IEDatabase.groupDataList.Where(g => g is FamilyGroupData)
                         .OfType<FamilyGroupData>())
            {
                var members = IM.IEDatabase.nodeDataList.Where(n => n is FamilyMemberData && n.GroupId == group.ID)
                    .OfType<FamilyMemberData>().ToList();
                memberNodes.Add(group.ID, members);
                nodes.AddRange(members);
            }
        }

        public static void LoadActors()
        {
            Init();

            foreach (var familyGroup in memberNodes)
            {
                foreach (var member in familyGroup.Value)
                {
                    var childOutput = member.Outputs.ElementAtOrDefault(0); //Child
                    var partnerOutput = member.Outputs.ElementAtOrDefault(1); //Partner

                    if (childOutput != null)
                    {
                        var childnodes = new List<FamilyMemberData>();

                        foreach (var nextNode in childOutput.DataCollection
                                     .Select(output => nodes.Find(n => n.ID == output.NextID))
                                     .Where(nextNode => nextNode != null))
                        {
                            childnodes.Add(nextNode);
                            if (Parents.ContainsKey(nextNode.ActorID))
                                Parents[nextNode.ActorID].Add(member);
                            else
                                Parents.Add(nextNode.ActorID, new List<FamilyMemberData>() { { member } });
                        }

                        if (childnodes.Count > 0) Childs.Add(member.ActorID, childnodes);
                    }

                    if (partnerOutput != null)
                    {
                        var partnerNodes = new List<FamilyMemberData>();

                        foreach (var nextNode in partnerOutput.DataCollection
                                     .Select(output => nodes.Find(n => n.ID == output.NextID))
                                     .Where(nextNode => nextNode != null))
                        {
                            partnerNodes.Add(nextNode);
                            if (Partners.ContainsKey(nextNode.ActorID))
                                Partners[nextNode.ActorID].Add(member);
                            else
                                Partners.Add(nextNode.ActorID, new List<FamilyMemberData>() { { member } });
                        }

                        if (partnerNodes.Count > 0) Partners.Add(member.ActorID, partnerNodes);
                    }
                }

                if (IM.IEDatabase.groupDataList.FirstOrDefault(g =>
                        g is FamilyGroupData family && family.ID == familyGroup.Key) is FamilyGroupData fGroup)
                    Families.Add(fGroup, familyGroup.Value);
            }
        }
    }
}