﻿// Guids.cs
// MUST match guids.h
using System;

namespace WakaTime
{
    static class GuidList
    {
        public const string GuidWakaTimePkgString = "52d9c3ff-c893-408e-95e4-d7484ec7fa47";

        public const string GuidWakaTimeCmdSetString = "054caf12-7fba-40d1-8dc8-bd69f838b910";
        public static readonly Guid GuidWakaTimeCmdSet = new Guid(GuidWakaTimeCmdSetString);

        public static readonly Guid GuidWakatimeOutputPane = new Guid("4d63f03a-724f-40b4-8dd8-60028b61fa93");
    };
}