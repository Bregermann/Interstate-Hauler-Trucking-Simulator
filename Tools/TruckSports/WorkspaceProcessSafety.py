"""Scope exclusive validation to this workspace; never close unrelated projects."""
import json
from pathlib import Path
import re
import subprocess


def workspace_process(process, root):
    root = Path(root).resolve()
    if process['Name'] == 'Unity.exe':
        command = process.get('CommandLine') or ''
        project = re.search(r'(?:^|\s)"?-projectPath"?\s+(?:"([^"]+)"|(\S+))', command, re.I)
        # Unknown Editors conservatively block. Explicit unrelated projects do not.
        return not project or Path(project[1] or project[2]).resolve().is_relative_to(root)
    path = process.get('ExecutablePath')
    return not path or Path(path).resolve().is_relative_to(root)


def inspect_workspace(root):
    raw = subprocess.check_output([
        'powershell', '-NoProfile', '-Command',
        "Get-CimInstance Win32_Process | Where-Object { $_.Name -in @('Unity.exe','TruckSports.exe','DeadAir.exe') } | Select-Object ProcessId,Name,ExecutablePath,CommandLine | ConvertTo-Json -Compress",
    ], text=True).strip()
    rows = json.loads(raw) if raw else []
    if isinstance(rows, dict):
        rows = [rows]
    # Do not expose raw command lines: unrelated Hub processes can contain tokens.
    return dict(owned=[dict(pid=p['ProcessId'], name=p['Name']) for p in rows if workspace_process(p, root)],
                unrelated=[dict(pid=p['ProcessId'], name=p['Name']) for p in rows if not workspace_process(p, root)])


def require_idle_workspace(root):
    state = inspect_workspace(root)
    assert not state['owned'], 'Wait for workspace Editor/player: ' + json.dumps(state['owned'])
    return state
