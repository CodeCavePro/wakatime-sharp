﻿#if NET35

// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

// ReSharper disable once CheckNamespace

namespace System
{
    /// <summary>
    /// Defines a provider for progress updates.
    /// </summary>
    /// <typeparam name="T">The type of progress update value.</typeparam>
    public interface IProgress<in T>
    {
        /// <summary>
        /// Reports a progress update.
        /// </summary>
        /// <param name="value">The value of the updated progress.</param>
        void Report(T value);
    }
}

#endif