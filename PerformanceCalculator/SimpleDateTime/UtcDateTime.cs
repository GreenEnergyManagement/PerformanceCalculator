using System;
using System.Globalization;
using System.Linq;

namespace PerformanceCalculator
{
    /// <summary>
    /// The UtcDateTime is a simple wrapper around the BCL DateTime which ensures that the wrapped DateTime instance
    /// must be in UTC time zone.
    /// 
    /// This struct is able to create corresponding IsoDateTime instances by invoking the ToIsoDateTime method and passing in
    /// the time zone you want to convert the UtcDateTime instance into.
    /// 
    /// On the other hand, the IsoDateTime struct is not capable of creating UtcDateTime instances. This is a restriction
    /// by design as we do not want to have a two way relationship between the types.
    /// </summary>
    
    public struct UtcDateTime : IComparable<UtcDateTime>
    {
        private DateTime utcUtcTime;

        public UtcDateTime(int year, int month, int day, int hour = 0, int minute = 0, int second = 0) : this(new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc)) { }

        public UtcDateTime(long ticks) : this(new DateTime(ticks, DateTimeKind.Utc)) { }

        public UtcDateTime(DateTime utcTime)
        {
            if (utcTime.Kind == DateTimeKind.Utc) this.utcUtcTime = utcTime;
            else throw new Exception("Unable to create UtcDateTime instance when DateTimeKind is not set to Utc.");

            // We will not manipulate the DateTime that is passed in here, we only validate it and throw if it does not meet our requirements.
            // This is because we want the programmer to become explicitly aware of what he/she is doing. Making it possible to pass in 
            // a date time which has unspecified Kind does not make this explicit anymore, and mistakes can be done.
            //
            // So, if you have a DateTime, that you know is in UTC but the kind is missing, then you cannot new this up but have to do the following:
            // UtcDateTime utcTime = UtcDateTime.CreateFromUnspecifiedDateTime(dateTimeInstance);
            // 
            // In this code, there is no doubt about what you are doing.
        }

        public static UtcDateTime CreateFromUnspecifiedDateTime(DateTime unspecifiedTime)
        {
            if (unspecifiedTime.Kind == DateTimeKind.Utc) return new UtcDateTime(unspecifiedTime);
            if (unspecifiedTime.Kind == DateTimeKind.Unspecified) return new UtcDateTime(DateTime.SpecifyKind(unspecifiedTime, DateTimeKind.Utc));
            throw new Exception("Unable to create UtcDateTime instance from unspecified date time as the DateTimeKind is not set to unspecified and will not be converted to utc.");
        }

        public UtcDateTime Subtract(TimeSpan span)
        {
            return new UtcDateTime(UtcTime.Subtract(span));
        }

        public UtcDateTime Add(TimeSpan span)
        {
            return new UtcDateTime(UtcTime.Add(span));
        }

        public UtcDateTime AddHours(int hours)
        {
            return new UtcDateTime(UtcTime.AddHours(hours));
        }

        public UtcDateTime AddMinutes(int value)
        {
            return new UtcDateTime(UtcTime.AddMinutes(value));
        }

        public UtcDateTime AddSeconds(int value)
        {
            return new UtcDateTime(UtcTime.AddSeconds(value));
        }

        /// <summary>
        /// Gets the number of ticks that represent the date and time of this instance.
        /// </summary>
        /// <returns>
        /// The number of ticks that represent the date and time of this instance. 
        /// The value is between DateTime.MinValue.Ticks and DateTime.MaxValue.Ticks.
        /// </returns>
        public long Ticks
        {
            get { return UtcTime.Ticks; }
        }

        public DateTime UtcTime
        {
            get { return utcUtcTime; }
            //set { DateTime v = value; } This is only to see how the JsonConverter is working on instances...
        }

        public static UtcDateTime Now { get { return new UtcDateTime(DateTime.UtcNow); } }

        public string ConvertToJson()
        {
            string stringUtcTime = utcUtcTime.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK", CultureInfo.InvariantCulture);
            return stringUtcTime;
        }

        public static DateTime ConvertFromJson(string utcTimeString)
        {
            return DateTime.ParseExact(utcTimeString, "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
        }

        public bool Equals(UtcDateTime other)
        {
            return utcUtcTime.Equals(other.utcUtcTime);
        }

        public int CompareTo(UtcDateTime other)
        {
            return utcUtcTime.CompareTo(other.utcUtcTime);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is UtcDateTime && Equals((UtcDateTime)obj);
        }

        public override int GetHashCode()
        {
            return utcUtcTime.GetHashCode();
        }



        public static bool operator ==(UtcDateTime left, UtcDateTime right)
        {
            return left.utcUtcTime.Equals(right.utcUtcTime);
        }

        public static bool operator !=(UtcDateTime left, UtcDateTime right)
        {
            return !left.utcUtcTime.Equals(right.utcUtcTime);
        }

        public static bool operator <(UtcDateTime left, UtcDateTime right)
        {
            return left.utcUtcTime < right.utcUtcTime;
        }

        public static bool operator >(UtcDateTime left, UtcDateTime right)
        {
            return left.utcUtcTime > right.utcUtcTime;
        }

        public static bool operator >=(UtcDateTime left, UtcDateTime right)
        {
            return left.utcUtcTime >= right.utcUtcTime;
        }

        public static bool operator <=(UtcDateTime left, UtcDateTime right)
        {
            return left.utcUtcTime <= right.utcUtcTime;
        }

        public static UtcDateTime MaxValue
        {
            get
            {
                // To make a min/max value safe you have to shift it outside of any conversion crashes
                var d = DateTime.MaxValue.AddHours(-30);
                return new UtcDateTime(d.Ticks);
            }
        }


        public static UtcDateTime MinValue
        {
            get
            {
                // To make a min/max value safe you have to shift it outside of any conversion crashes
                var d = new DateTime(1753, 1, 1).AddHours(30); // This is the start of the Gregorian epoche, SQL server cannot save any date earlier than this.
                return new UtcDateTime(d.Ticks);
            }
        }


        public static implicit operator DateTime(UtcDateTime d)
        {
            return d.UtcTime;
        }

        /// <summary>
        /// Gets the day of the month represented by this instance.
        /// </summary>
        /// <returns>
        /// The day component, expressed as a value between 1 and 31.
        /// </returns>
        public int Day { get { return utcUtcTime.Day; } }

        /// <summary>
        /// Gets the day of the week represented by this instance.
        /// </summary>
        /// <returns>
        /// An enumerated constant that indicates the day of the week of this <see cref="T:IsoDateTimeZone.IsoDateTime"/> value.
        /// </returns>
        public DayOfWeek DayOfWeek { get { return (DayOfWeek)Enum.Parse(typeof(DayOfWeek), utcUtcTime.DayOfWeek.ToString()); } }

        /// <summary>
        /// Gets the day of the year represented by this instance.
        /// </summary>
        /// <returns>
        /// The day of the year, expressed as a value between 1 and 366.
        /// </returns>
        public int DayOfYear { get { return utcUtcTime.DayOfYear; } }

        /// <summary>
        /// Gets the hour component of the date represented by this instance.
        /// </summary>
        /// <returns>
        /// The hour component, expressed as a value between 0 and 23.
        /// </returns>
        public int Hour { get { return utcUtcTime.Hour; } }

        /// <summary>
        /// Gets the milliseconds component of the date represented by this instance.
        /// </summary>
        /// <returns>
        /// The milliseconds component, expressed as a value between 0 and 999.
        /// </returns>
        public int Millisecond { get { return utcUtcTime.Millisecond; } }

        /// <summary>
        /// Gets the minute component of the date represented by this instance.
        /// </summary>
        /// <returns>
        /// The minute component, expressed as a value between 0 and 59.
        /// </returns>
        public int Minute { get { return utcUtcTime.Minute; } }

        /// <summary>
        /// Gets the month component of the date represented by this instance.
        /// </summary>
        /// <returns>
        /// The month component, expressed as a value between 1 and 12.
        /// </returns>
        public int Month { get { return utcUtcTime.Month; } }

        /// <summary>
        /// Gets the seconds component of the date represented by this instance.
        /// </summary>
        /// <returns>
        /// The seconds component, expressed as a value between 0 and 59.
        /// </returns>
        public int Second { get { return utcUtcTime.Second; } }

        /// <summary>
        /// Gets the time of day for this instance.
        /// </summary>
        /// <returns>
        /// A time interval that represents the fraction of the day that has elapsed since midnight.
        /// </returns>
        public TimeSpan TimeOfDay { get { return utcUtcTime.TimeOfDay; } }

        /// <summary>
        /// Gets the year component of the date represented by this instance.
        /// </summary>
        /// <returns>
        /// The year, between 1 and 9999.
        /// </returns>
        public int Year { get { return utcUtcTime.Year; } }

        public Time Time { get { return new Time(Hour, Minute, Second, false, TimeType.Use24Hours); } }

        public static UtcDateTime Min(UtcDateTime left, UtcDateTime right)
        {
            if (left < right) return left;
            return right;
        }

        public static UtcDateTime Max(UtcDateTime left, UtcDateTime right)
        {
            if (left > right) return left;
            return right;
        }

        /// <summary>
        /// Supported formats are ISO 8601 which is yyyy-MM-ddTHH:mm:ssZ, yyyy-MM-ddTHH:mm:ss treated as UTC,
        /// or
        /// yyyy-MM-dd HH:mm:ssZ, yyyy-MM-dd HH:mm:ss treated as UTC.
        /// 
        /// Together with little-endian format such as dd-MM-yyyyTHH:mm:ssZ, dd-MM-yyyyTHH:mm:ss treated as UTC,
        /// or
        /// dd-MM-yyyy HH:mm:ssZ, dd-MM-yyyy HH:mm:ss treated as UTC.
        /// </summary>
        /// <param name="timeStampStr">The string containing one of the above documented leagal formats, either ISO 8601 or little endian format.</param>
        /// <returns></returns>
        public static string GetDateTimePattern(string timeStampStr)
        {
            timeStampStr = timeStampStr.Trim();
            string[] dateAndTimeParts = timeStampStr.Split(' ');
            bool hasTimeSeperator = false;
            if (dateAndTimeParts.Length == 1)
            {
                dateAndTimeParts = timeStampStr.Split('T');
                hasTimeSeperator = true;
            }

            string datePart = dateAndTimeParts[0];
            string timePart = dateAndTimeParts[1];
            string firstPart = datePart.Substring(0, 5);

            char[] seps = new[] { '.', '-', '/', '_' };
            char[] array = firstPart.ToCharArray();

            char deliminator = '.';
            int number = 0;
            for (int i = 0; i < array.Length; i++)
            {
                char c = array[i];
                if (Char.IsNumber(c)) number++;
                else
                {
                    if (seps.Any(sep => sep == c)) { deliminator = c; }
                    break;
                }
            }

            string t = " ";
            if (hasTimeSeperator) t = "T";

            string z = string.Empty;
            if (timeStampStr.EndsWith("Z", StringComparison.InvariantCultureIgnoreCase)) z = "Z";

            string timePattern = string.Empty;
            if (timePart.Length < 3) timePattern = "HH";
            else if (timePart.Length < 6) timePattern = "HH:mm";
            else if (timePart.Length < 10) timePattern = "HH:mm:ss";
            else timePattern = "HH:mm:ss.FFFFFFFK";

            string pattern = string.Empty;
            if (number > 2)
            {
                // Some sort of ISO 8601 format used
                pattern = "yyyy" + deliminator + "MM" + deliminator + "dd" + t + timePattern + z;
            }
            else
            {   // Some sort of little endian format is used
                pattern = "dd" + deliminator + "MM" + deliminator + "yyyy" + t + timePattern + z;
                DateTime timeStampFormatTest;
                if (!DateTime.TryParseExact(timeStampStr, pattern, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out timeStampFormatTest))
                {
                    pattern = "MM" + deliminator + "dd" + deliminator + "yyyy" + t + timePattern + z;
                }
            }

            return pattern;
        }
    }
}