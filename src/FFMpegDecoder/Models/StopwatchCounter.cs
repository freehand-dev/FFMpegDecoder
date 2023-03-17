using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFMpegDecoder.Models
{
    public class StopwatchCounter
    {
        private int _value;
        private int _threshold;
        private TimeSpan _timeInterval;
        private Stopwatch _stopwatch;

        public StopwatchCounter(int threshold, TimeSpan timeInterval)
        {
            this._value = 0;
            this._threshold = threshold;
            this._timeInterval = timeInterval;  
            this._stopwatch = new Stopwatch();
            this._stopwatch.Start();
        }

        public void Increment(out bool brokenThreshold, out int brokenThresholdValue)
        {
            brokenThreshold = false;
            brokenThresholdValue = 0;
            if (this._stopwatch.ElapsedMilliseconds >= _timeInterval.TotalMilliseconds)
            {
                if (this._value >= this._threshold)
                {
                    brokenThreshold = true;
                    brokenThresholdValue = this._value;
                }
                this._stopwatch.Restart();
                this._value = 0;
            }
            else
            {
                this._value++;
            }
        }

    }
}
