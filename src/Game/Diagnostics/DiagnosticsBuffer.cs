using System;
using System.Collections.Generic;

namespace Game.Diagnostics;

public sealed class DiagnosticsBuffer
{
    private readonly int _capacity;
    private readonly Queue<DiagnosticEvent> _events;

    public DiagnosticsBuffer(int capacity = 4096)
    {
        if (capacity < 128) throw new ArgumentOutOfRangeException(nameof(capacity));
        _capacity = capacity;
        _events = new Queue<DiagnosticEvent>(capacity);
    }

    public void Add(string category, string message, DiagnosticSeverity severity = DiagnosticSeverity.Info)
    {
        if (_events.Count >= _capacity)
            _events.Dequeue();

        _events.Enqueue(new DiagnosticEvent(DateTimeOffset.UtcNow, category, severity, message));
    }

    public IReadOnlyList<DiagnosticEvent> Snapshot() => _events.ToArray();
}

public enum DiagnosticSeverity
{
    Trace,
    Info,
    Warning,
    Error,
    Critical,
}

public sealed record DiagnosticEvent(
    DateTimeOffset Timestamp,
    string Category,
    DiagnosticSeverity Severity,
    string Message
);
