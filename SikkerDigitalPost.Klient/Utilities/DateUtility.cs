﻿using System;

namespace SikkerDigitalPost.Klient.Utilities
{
    internal static class DateUtility
    {
        public const string DateFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffZ";

        public static string DateForFile()
        {
            return DateTime.Now.ToString("yyyy'-'MM'-'dd HH'.'mm'.'1'.'ss");
        } 
    }
}
