// Helpers/TimeHelper.cs
using System;

namespace Matchletic.Helpers
{
    public static class TimeHelper
    {
        public static string GetTimeAgo(DateTime dateTime)
        {
            TimeSpan timeSince = DateTime.Now.Subtract(dateTime);

            if (timeSince.TotalSeconds < 60)
                return "upravo sad";
            if (timeSince.TotalMinutes < 60)
                return $"prije {(int)timeSince.TotalMinutes} min";
            if (timeSince.TotalHours < 24)
                return $"prije {(int)timeSince.TotalHours} h";
            if (timeSince.TotalDays < 7)
                return $"prije {(int)timeSince.TotalDays} d";

            return dateTime.ToString("dd.MM.yyyy");
        }
    }
}
