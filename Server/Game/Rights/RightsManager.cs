using System;
using System.Collections.Generic;
using System.Data;

using Snowlight.Storage;
using Snowlight.Game.Sessions;

namespace Snowlight.Game.Rights
{
    public static class RightsManager
    {
        private static Dictionary<uint, List<int>> mRights;
        private static Dictionary<uint, String> mRightsBages;
        private static Dictionary<uint, List<string>> mRightSets;

        public static void Initialize(SqlDatabaseClient MySqlClient)
        {
            mRights = new Dictionary<uint, List<int>>();
            mRightSets = new Dictionary<uint, List<string>>();
            mRightsBages = new Dictionary<uint, String>();

            RebuildCache(MySqlClient);
        }

        public static void RebuildCache(SqlDatabaseClient MySqlClient)
        {
            lock (mRights)
            {
                mRights.Clear();
                mRightSets.Clear();
                mRightsBages.Clear();

                DataTable RankTable = MySqlClient.ExecuteQueryTable("SELECT rank_id,rights_sets,badge_code FROM ranks");
                DataTable RightsTable = MySqlClient.ExecuteQueryTable("SELECT set_id,right_id FROM rights");

                foreach (DataRow Row in RankTable.Rows)
                {
                    List<uint> Sets = new List<uint>();
                    string[] SetBits = ((string)Row["rights_sets"]).Split(',');
                    

                    uint rank = (uint)Row["rank_id"];

                    if (!mRights.ContainsKey(rank))
                    {
                        mRights.Add(rank, new List<int>());
                    }

                    if (!mRightsBages.ContainsKey(rank))
                    {
                        mRightsBages.Add(rank, (String)Row["badge_code"]);
                    }

                    foreach (string SetBit in SetBits)
                    {
                        int Set = 0;

                        int.TryParse(SetBit, out Set);

                        if (Set > 0)
                        {
                            mRights[rank].Add(Set);
                        }
                    }
                }

                foreach (DataRow Row in RightsTable.Rows)
                {
                    uint SetId = (uint)Row["set_id"];

                    if (!mRightSets.ContainsKey(SetId))
                    {
                        mRightSets.Add(SetId, new List<string>());
                    }

                    mRightSets[SetId].Add((string)Row["right_id"]);
                }
            }
        }

        public static void CleanBadges(uint Rank, Session Session)
        {
            Boolean changed = false;

            foreach (String badge in mRightsBages.Values)
            {
                if (mRightsBages.ContainsValue(badge) && !mRightsBages.ContainsKey(Rank) || mRightsBages[Rank] != badge)
                {
                    Session.BadgeCache.RemoveBadge(badge);
                    changed = true;
                }

                if (mRightsBages.ContainsKey(Rank) && mRightsBages[Rank] == badge && !Session.BadgeCache.ContainsCode(badge))
                {
                    Session.BadgeCache.AddBadge(badge);
                    changed = true;
                }
            }

            if (changed)
            {
                using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                {
                    Session.BadgeCache.ReloadCache(MySqlClient, Session.AchievementCache);
                }
            }
        }

        public static List<string> GetRightsForRank(uint Rank, ClubSubscriptionLevel level, bool IsPremium)
        {
            List<string> Rights = new List<string>();

            if (level == ClubSubscriptionLevel.BasicClub)
            {
                Rights.Add("club_regular");
            }

            if (level == ClubSubscriptionLevel.VipClub)
            {
                Rights.Add("club_vip");
            }

            if (IsPremium)
            {
                Rights.Add("club_premium");
            }

            foreach (String Right in mRightSets[0])
            {
                if (Rights.Contains(Right))
                {
                    continue;
                }
                Rights.Add(Right);
            }

            foreach (uint set in mRights[Rank])
            {
                if (mRightSets.ContainsKey(set))
                {
                    foreach (string Right in mRightSets[set])
                    {
                        if (Rights.Contains(Right))
                        {
                            continue;
                        }

                        Rights.Add(Right);
                    }
                }
            }
            
            return Rights;
        }
    }
}
