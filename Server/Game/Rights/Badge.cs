using System;
using System.Collections.Generic;

namespace Snowlight.Game.Rights
{
    public class Badge
    {
        private string mCode;
        private uint mId;

        public string Code
        {
            get
            {
                return mCode;
            }
        }

        public uint Id
        {
            get
            {
                return mId;
            }
        }

        public Badge(string Code)
        {
            mCode = Code;
            mId = 0;
        }
    }
}
