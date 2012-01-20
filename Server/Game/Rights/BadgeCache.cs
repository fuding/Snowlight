using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;

using Snowlight.Storage;
using Snowlight.Game.Achievements;
using Snowlight.Game.Sessions;
using Snowlight.Game.Rooms;
using Snowlight.Communication.Outgoing;

namespace Snowlight.Game.Rights
{
    public class BadgeCache
    {
        private uint mUserId;
        private object mSyncRoot;

        private Dictionary<int, Badge> mEquippedBadges;
        private List<Badge> mStaticBadges;
        private Dictionary<string, Badge> mAchievementBadges;
        private List<string> mIndexCache;

        public SortedDictionary<int, Badge> EquippedBadges
        {
            get
            {
                SortedDictionary<int, Badge> Copy = new SortedDictionary<int, Badge>();

                lock (mSyncRoot)
                {
                    foreach (KeyValuePair<int, Badge> Data in mEquippedBadges)
                    {
                        Copy.Add(Data.Key, Data.Value);
                    }
                }

                return Copy;
            }
        }

        public List<Badge> Badges
        {
            get
            {
                List<Badge> Copy = new List<Badge>();

                lock (mSyncRoot)
                {
                    Copy.AddRange(mStaticBadges);
                    Copy.AddRange(mAchievementBadges.Values.ToList());
                }

                return Copy;
            }
        }

        public BadgeCache(SqlDatabaseClient MySqlClient, uint UserId, AchievementCache UserAchievementCache)
        {
            mUserId = UserId;
            mSyncRoot = new object();

            mEquippedBadges = new Dictionary<int, Badge>();
            mStaticBadges = new List<Badge>();
            mAchievementBadges = new Dictionary<string, Badge>();
            mIndexCache = new List<string>();

            ReloadCache(MySqlClient, UserAchievementCache);
        }

        public void ReloadCache(SqlDatabaseClient MySqlClient, AchievementCache UserAchievementCache)
        {
            Dictionary<int, Badge> EquippedBadges = new Dictionary<int, Badge>();
            List<Badge> StaticBadges = new List<Badge>();
            Dictionary<string, Badge> AchievementBadges = new Dictionary<string, Badge>();
            List<string> IndexCache = new List<string>();

            MySqlClient.SetParameter("userid", mUserId);
            DataTable Table = MySqlClient.ExecuteQueryTable("SELECT badge_code,slot_id,source_type,source_data FROM user_badges WHERE user_id = @userid");

            foreach (DataRow Row in Table.Rows)
            {               
                string SourceType = Row["source_type"].ToString();
                string SourceData = Row["source_data"].ToString();

                Badge BadgeToEquip = null;

                if (SourceType == "static")
                {
                    BadgeToEquip = new Badge(Row["badge_code"].ToString());
                    StaticBadges.Add(BadgeToEquip);
                }
                else if (SourceType == "achievement")
                {
                    if (AchievementBadges.ContainsKey(SourceData))
                    {
                        continue;
                    }

                    UserAchievement UserAchievement = UserAchievementCache.GetAchievementData(SourceData);

                    if (UserAchievement == null || UserAchievement.Level < 1)
                    {
                        MySqlClient.SetParameter("userid", mUserId);
                        MySqlClient.SetParameter("badgecode", Row["badge_code"].ToString());
                        MySqlClient.ExecuteNonQuery("DELETE FROM user_badges WHERE user_id = @userid AND badge_id = @badgeid");
                        continue;
                    }

                    string Code = UserAchievement.GetBadgeCodeForLevel();

                    BadgeToEquip = new Badge(Code);
                    AchievementBadges.Add(SourceData, BadgeToEquip);
                }

                if (BadgeToEquip != null)
                {
                    int SlotId = (int)Row["slot_id"];

                    if (!EquippedBadges.ContainsKey(SlotId) && SlotId >= 1 && SlotId <= 5)
                    {
                        EquippedBadges.Add(SlotId, BadgeToEquip);
                    }

                    IndexCache.Add(BadgeToEquip.Code);
                }
            }

            lock (mSyncRoot)
            {
                mEquippedBadges = EquippedBadges;
                mStaticBadges = StaticBadges;
                mAchievementBadges = AchievementBadges;
                mIndexCache = IndexCache;
            }
        }
     

        public bool ContainsCode(string BadgeCode)
        {
            lock (mSyncRoot)
            {
                return mIndexCache.Contains(BadgeCode);
            }
        }


        public void UpdateAchievementBadge(SqlDatabaseClient MySqlClient, string AchievementGroup, Badge NewBadge)
        {
            MySqlClient.SetParameter("userid", mUserId);
            MySqlClient.SetParameter("sourcetype", "achievement");
            MySqlClient.SetParameter("sourcedata", AchievementGroup);
            MySqlClient.SetParameter("badgecode", NewBadge.Code);

            lock (mSyncRoot)
            {
                if (mAchievementBadges.ContainsKey(AchievementGroup))
                {
                    Badge OldBadge = mAchievementBadges[AchievementGroup];

                    if (OldBadge == NewBadge)
                    {
                        MySqlClient.ClearParameters();
                        return;
                    }

                    mIndexCache.Remove(OldBadge.Code);
                    mAchievementBadges[AchievementGroup] = NewBadge;

                    MySqlClient.ExecuteNonQuery("UPDATE badges SET badge_code = @badgecode WHERE user_id = @userid AND source_type = @sourcetype AND source_data = @sourcedata LIMIT 1");

                    foreach (KeyValuePair<int, Badge> Badge in mEquippedBadges)
                    {
                        if (Badge.Value.Code == OldBadge.Code)
                        {
                            mEquippedBadges[Badge.Key] = NewBadge;
                            break;
                        }
                    }
                }
                else
                {
                    mAchievementBadges.Add(AchievementGroup, NewBadge);
                    MySqlClient.ExecuteNonQuery("INSERT INTO user_badges (user_id,badge_code,source_type,source_data) VALUES (@userid,@badgecode,@sourcetype,@sourcedata)");
                }

                mIndexCache.Add(NewBadge.Code);
            }
        }

        public void UpdateBadgeOrder(SqlDatabaseClient MySqlClient, Dictionary<int, Badge> NewSettings)
        {
            MySqlClient.SetParameter("userid", mUserId);
            MySqlClient.ExecuteNonQuery("UPDATE user_badges SET slot_id = 0 WHERE user_id = @userid");

            foreach (KeyValuePair<int, Badge> EquippedBadge in NewSettings)
            {
                MySqlClient.SetParameter("userid", mUserId);
                MySqlClient.SetParameter("slotid", EquippedBadge.Key);
                MySqlClient.SetParameter("badgecode", EquippedBadge.Value.Code);
                MySqlClient.ExecuteNonQuery("UPDATE user_badges SET slot_id = @slotid WHERE user_id = @userid AND badge_code = @badgecode LIMIT 1");
            }

            lock (mSyncRoot)
            {
                mEquippedBadges = NewSettings;
            }
        }

        public void DisableSubscriptionBadge(string BadgeCodePrefix) 
        {
            lock (mSyncRoot)
            {
                foreach (KeyValuePair<string, Badge> Data in mAchievementBadges)
                {
                    if (Data.Value.Code.StartsWith(BadgeCodePrefix))
                    {
                        mIndexCache.Remove(Data.Value.Code);
                        mAchievementBadges.Remove(Data.Key);

                        foreach (KeyValuePair<int, Badge> EquipData in mEquippedBadges)
                        {
                            if (EquipData.Value.Code.StartsWith(BadgeCodePrefix))
                            {
                                mEquippedBadges.Remove(EquipData.Key);
                                break;
                            }
                        }

                        break;
                    }
                }
            }
        }

        public void RemoveBadge(string BadgeCode)
        {
            lock (mSyncRoot)
            {
                foreach (Badge badge in mStaticBadges)
                {
                    if (badge.Code == BadgeCode)
                    {
                        mIndexCache.Remove(badge.Code);
                        mStaticBadges.Remove(badge);

                        foreach (KeyValuePair<int, Badge> EquipData in mEquippedBadges)
                        {
                            if (EquipData.Value.Code == BadgeCode)
                            {
                                mEquippedBadges.Remove(EquipData.Key);
                                break;
                            }
                        }

                        using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                        {
                            MySqlClient.SetParameter("userid", mUserId);
                            MySqlClient.SetParameter("badgecode", BadgeCode);
                            MySqlClient.ExecuteNonQuery("DELETE FROM user_badges WHERE user_id = @userid AND badge_code = @badgecode");
                        }

                        break;
                    }
                }
            }
        }


        public void AddBadge(string BadgeCode)
        {
            lock (mSyncRoot)
            {
                if (!mStaticBadges.Contains(new Badge(BadgeCode)))
                {
                    mStaticBadges.Add(new Badge(BadgeCode));

                    using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                    {
                        MySqlClient.SetParameter("userid", mUserId);
                        MySqlClient.SetParameter("badgecode", BadgeCode);
                        MySqlClient.ExecuteNonQuery("INSERT INTO user_badges (user_id, badge_code) VALUES (@userid,@badgecode)");
                    }
                }
            }
        }


        public bool ContainsCodeWith(string Code)
        {
            lock (mSyncRoot)
            {
                foreach (string Item in mIndexCache)
                {
                    if (Item.StartsWith(Code))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
