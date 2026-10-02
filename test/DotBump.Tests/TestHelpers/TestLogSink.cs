// Copyright © Roby Van Damme.

using Serilog.Core;
using Serilog.Events;

namespace DotBump.Tests.TestHelpers;

public sealed class TestLogSink : ILogEventSink
{
    private readonly List<LogEvent> _events = [];

    public IReadOnlyList<LogEvent> Events => _events;

    public void Emit(LogEvent logEvent)
    {
        _events.Add(logEvent);
    }
}
