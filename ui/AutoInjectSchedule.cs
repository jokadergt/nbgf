using System;

namespace RblxExecutorUI;

// Connection attempts share one cooldown; a new process can connect immediately.
public sealed class AutoInjectSchedule
{
    private uint _lastPid;
    private DateTime _nextAttempt = DateTime.MinValue;
    public void Reset() { _lastPid = 0; _nextAttempt = DateTime.MinValue; }
    public bool TrySchedule(uint pid, uint connectedPid, DateTime utcNow)
    {
        if (pid == 0) { Reset(); return false; }
        if (pid == connectedPid) return false;
        if (pid == _lastPid && utcNow < _nextAttempt) return false;
        _lastPid = pid;
        _nextAttempt = utcNow.AddSeconds(30);
        return true;
    }
}
