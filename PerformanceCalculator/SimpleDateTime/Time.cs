using System;
using System.Globalization;

namespace PerformanceCalculator
{
    /// <summary>
    /// The values in this struct represents the time part in a date time.
    /// </summary>
    /// <remarks>
    /// Type is not meant to be exported to the database, only for representing time for time zone calculations.
    /// </remarks>
    [Serializable]
    public struct Time
    {
        private const char AM = 'A';
        private const char PM = 'P';
        private byte hour;
        private byte minute;
        private byte second;

        /// <summary>
        /// Initializes the time struct to a given time.
        /// </summary>
        /// <param name="hour">The hour in the day.</param>
        /// <param name="minute">The minute in the hour.</param>
        /// <param name="second">The second in the minute.</param>
        /// <param name="isAm">True if time is AM and false if time is PM, if TimeType is digit, then isAm will be calculated.</param>
        /// <param name="timeType">Enum describing if AM/PM should be used instead of 24 hours in a day.</param>
        private Time(byte hour, byte minute, byte second, bool isAm, TimeType timeType)
            : this()
        {
            // validation...
            if (hour < 0 || hour > 23) throw new Exception("When using 24 hours a day, legal hour values range from 0 to 23. Given hour value is: " + hour);
            if (minute < 0 || minute > 59) throw new Exception("Legal minute values range from 0 to 59. Given hour value is: " + minute);
            if (second < 0 || second > 59) throw new Exception("Legal minute values range from 0 to 59. Given hour value is: " + minute);
            if (timeType == TimeType.UseAmPm && hour > 12) hour = (byte)(hour - 12);
            else
            {	// Applies only if TimeType is digit.
                if (hour >= 12) isAm = false;
                else isAm = true;
            }

            this.hour = hour;
            this.minute = minute;
            this.second = second;
            IsAm = isAm;
            TimeType = timeType;
        }

        /// <summary>
        /// Initializes the time struct to a given time.
        /// </summary>
        /// <param name="hour">The hour in the day.</param>
        /// <param name="minute">The minute in the hour.</param>
        /// <param name="second">The second in the minute.</param>
        /// <param name="isAm">True if time is AM and false if time is PM, if TimeType is digit, then isAm will be calculated.</param>
        /// <param name="timeType">Enum describing if AM/PM should be used instead of 24 hours in a day.</param>
        public Time(int hour, int minute, int second, bool isAm, TimeType timeType)
            : this()
        {
            // validation...
            if (hour < 0 || hour > 23) throw new Exception("When using 24 hours a day, legal hour values range from 0 to 23. Given hour value is: " + hour);
            if (minute < 0 || minute > 59) throw new Exception("Legal minute values range from 0 to 59. Given hour value is: " + minute);
            if (second < 0 || second > 59) throw new Exception("Legal minute values range from 0 to 59. Given hour value is: " + minute);
            if (timeType == TimeType.UseAmPm && hour > 12) hour = hour - 12;
            else
            {	// Applies only if TimeType is digit.
                if (hour >= 12) isAm = false;
                else isAm = true;
            }

            this.hour = (byte)hour;
            this.minute = (byte)minute;
            this.second = (byte)second;
            IsAm = isAm;
            TimeType = timeType;
        }

        /// <summary>
        /// The hour property.
        /// </summary>
        public int Hour { get { return hour; } }

        /// <summary>
        /// The minute property.
        /// </summary>
        public int Minute { get { return minute; } }

        /// <summary>
        /// The second property.
        /// </summary>
        public int Second { get { return second; } }

        /// <summary>
        /// The isAm property, if set to true, indicates that the hour value is before noon.
        /// </summary>
        public bool IsAm { get; private set; }

        /// <summary>
        /// The time type, used to switch between 12 and 24 hours in a day.
        /// </summary>
        public TimeType TimeType { get; private set; }

        /// <summary>
        /// Parses a standard time formatted string on this format:
        /// '02:00:00 A.M.'
        /// '02:00:00 AM'
        /// '02:00:00 P.M.'
        /// '02:00:00 PM'
        /// '14:00:00'
        /// </summary>
        /// <remarks>
        ///  t     12:00 AM
        ///  h:mm tt (ShortTimePattern)
        ///  T     12:00:00 AM
        ///  h:mm:ss tt (LongTimePattern) 
        /// </remarks>
        /// <param name="timeFormattedString">A string on the format shown above.</param>
        /// <returns>A Time instance representing the passed in time.</returns>
        public static Time ParseAsTime(string timeFormattedString)
        {
            byte hour = 0;
            byte minute = 0;
            byte seconds = 0;
            bool isAm = false;
            TimeType timeType;

            timeFormattedString = timeFormattedString.Trim(' ', 's', 'u');
            string[] hourMinuteSecondAndAmPmInfo = timeFormattedString.Split(' ');
            if (hourMinuteSecondAndAmPmInfo[0] != null)
            {
                string[] hourMinuteSecond = hourMinuteSecondAndAmPmInfo[0].Split(':');
                hour = Convert.ToByte(hourMinuteSecond[0]);
                if (hour == 24) hour = 0;
                if (hourMinuteSecond.Length > 1) minute = Convert.ToByte(hourMinuteSecond[1]);
                if (hourMinuteSecond.Length > 2) seconds = Convert.ToByte(hourMinuteSecond[2]);
            }

            if (hourMinuteSecondAndAmPmInfo.Length > 1 && hourMinuteSecondAndAmPmInfo[1] != null)
            {
                if (hourMinuteSecondAndAmPmInfo[1].StartsWith("A")) isAm = true;
                timeType = TimeType.UseAmPm;
            }
            else
            {
                timeType = TimeType.Use24Hours;
                isAm = (hour < 12);
            }

            return new Time(hour, minute, seconds, isAm, timeType);
        }

        /// <summary>
        /// Checks if to Time instances represents the same time within a day.
        /// </summary>
        /// <param name="obj">The other time instance to compare this to.</param>
        /// <returns>True if the instances represent the same time unit within a day.</returns>
        public override bool Equals(object obj)
        {
            if (obj is Time)
            {
                return ToString("S").Equals(((Time)obj).ToString("S"));
            }
            return false;
        }

        /// <summary>
        /// The hash code representing the LongSortableTimePattern string produced by this instance.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return ToString("S").GetHashCode();
        }

        public TimeSpan ToTimeSpan() { return new TimeSpan(Hour, Minute, Second); }

        /// <summary>
        /// Printing the time as a normal LongTimePattern(T).
        /// </summary>
        /// <returns>A string representation of the time.</returns>
        public override string ToString()
        {
            DateTimeFormatInfo dtfi = DateTimeFormatInfo.CurrentInfo;
            string am = GetAmPmValue();
            return GetDigitString(Hour) + dtfi.TimeSeparator + GetDigitString(Minute) + dtfi.TimeSeparator + GetDigitString(Second) + " " + am;
        }

        /// <summary>
        /// Printing the time as a normal LongTimePattern(T) or as a ShortTimePattern(t);
        /// </summary>
        /// <param name="format">The format string describing how the Time instance should be printed.</param>
        /// <remarks>
        ///  t  ->   12:00 AM
        ///  h:mm tt (ShortTimePattern)
        /// 
        ///  T  ->   12:00:00 AM
        ///  h:mm:ss tt (LongTimePattern)
        /// 
        ///  s  ->   00:00
        ///  HH':'mm (ShortSortableTimePattern)
        /// 
        ///  S  ->   00:00:00
        ///  HH':'mm':'ss (LongSortableTimePattern)
        /// </remarks>
        /// <returns>A string representation of the time.</returns>
        public string ToString(string format)
        {
            if (format.Equals("T")) return ToString();
            DateTimeFormatInfo dtfi = DateTimeFormatInfo.CurrentInfo;
            if (format.Equals("t"))
            {
                string am = GetAmPmValue();
                return GetDigitString(Hour) + dtfi.TimeSeparator + GetDigitString(Minute) + " " + am;
            }
            if (format.Equals("s") || format.Equals("S"))
            {
                int h = Hour;
                if (TimeType == TimeType.UseAmPm && !IsAm) h += 12;

                if (format.Equals("s")) return GetDigitString(h) + dtfi.TimeSeparator + GetDigitString(Minute);
                return GetDigitString(h) + dtfi.TimeSeparator + GetDigitString(Minute) + dtfi.TimeSeparator + GetDigitString(Second);
            }

            return ToString();
        }

        private string GetDigitString(int digit)
        {
            return digit < 10 ? "0" + digit : "" + digit;
        }

        private string GetAmPmValue()
        {
            if (TimeType == TimeType.UseAmPm) return IsAm ? AM + "M" : PM + "M";
            return string.Empty;
        }

        public static bool operator <(Time left, Time right)
        {
            return left.ToTimeSpan() < right.ToTimeSpan();
        }

        public static bool operator >(Time left, Time right)
        {
            return left.ToTimeSpan() > right.ToTimeSpan();
        }

        public static bool operator >=(Time left, Time right)
        {
            return left.ToTimeSpan() >= right.ToTimeSpan();
        }

        public static bool operator <=(Time left, Time right)
        {
            return left.ToTimeSpan() <= right.ToTimeSpan();
        }
    }
}