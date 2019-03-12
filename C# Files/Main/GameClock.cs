using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace GameServerExample
{
    public class GameClock : IMonotonicClock
    {
        float currTime;

        public GameClock()
        {
            currTime = 0;
        }

        public float GetNow()
        {
            return Stopwatch.GetTimestamp() / (float)Stopwatch.Frequency;
        }

        public void Tick()
        {
            currTime = Stopwatch.GetTimestamp() / (float)Stopwatch.Frequency;
        }
    }
}
