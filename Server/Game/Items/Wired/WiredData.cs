using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Snowlight.Storage;
using System.Data;

namespace Snowlight.Game.Items.Wired
{
    public class WiredData
    {
        private uint mItemId;
        private int mType;

        private String mData1;
        private Int32 mData2;
        private Int32 mData3;

        public uint ItemId
        {
            get
            {
                return mItemId;
            }
        }


        public int Type
        {
            get
            {
                return mType;
            }
        }

        public String Data1
        {
            get
            {
                return mData1;
            }

            set
            {
                mData1 = value;
            }
        }

        public Int32 Data2
        {
            get
            {
                return mData2;
            }

            set
            {
                mData2 = value;
            }
        }

        public Int32 Data3
        {
            get
            {
                return mData3;
            }

            set
            {
                mData3 = value;
            }
        }

        public WiredData(uint ItemId, int Type)
        {
            mItemId = ItemId;
            mType = Type;

            using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
            {
                MySqlClient.SetParameter("id", ItemId);
                DataRow Row = MySqlClient.ExecuteQueryRow("SELECT * FROM wired_items WHERE item_id = @id LIMIT 1");

                if (Row != null)
                {
                    GenerateWiredFromRow(Row);
                }
                else
                {
                    MySqlClient.SetParameter("id", ItemId);
                    MySqlClient.ExecuteNonQuery("INSERT INTO wired_items (item_id, data1, data2, data3) VALUES (@id, '','0','0')");
                }
            }
        }

        public void GenerateWiredFromRow(DataRow Row)
        {
            mData1 = (string)Row["data1"];
            mData2 = (Int32)Row["data2"];
            mData3 = (Int32)Row["data3"];
        }

        public void SynchronizeDatabase(SqlDatabaseClient MySqlClient)
        {
            MySqlClient.SetParameter("id", mItemId);
            MySqlClient.SetParameter("data1", mData1);
            MySqlClient.SetParameter("data2", mData2);
            MySqlClient.SetParameter("data3", mData3);
            MySqlClient.ExecuteNonQuery("UPDATE wired_items SET data1 = @data1, data2 = @data2, data3 = @data3 WHERE item_id = @id LIMIT 1");
        }
    }
}
