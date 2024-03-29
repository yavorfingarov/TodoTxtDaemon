﻿global using System.Globalization;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.Logging;
global using Moq;
global using Xunit;

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names can contain underscores.")]
[assembly: SuppressMessage("Usage", "CA2201:Do not raise reserved exception types", Justification = "Plain Exception is used in tests.")]
