using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace GameServerExample.Test
{
    class FakeClock : IMonotonicClock
    {
        float currTime;

        public FakeClock(float timeStamp = 0)
        {
            currTime = timeStamp;
        }

        public float GetNow()
        {
            return currTime;
        }

        public void IncreaseTimeStamp(float delta)
        {
            if (delta <= 0)
            {
                throw new Exception("Invalid delta value");
            }
            currTime += delta;
        }
    }
}
