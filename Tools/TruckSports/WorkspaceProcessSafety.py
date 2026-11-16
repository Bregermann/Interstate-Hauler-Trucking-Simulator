"""Scope exclusive validation to this workspace; never close unrelated projects."""
import json
from pathlib import Path
import re
import subprocess


def workspace_process(process, root, processes=None):
    root = Path(root).resolve()
    if process['Name'] == 'Unity.exe':
        command = process.get('CommandLine') or ''
        project = re.search(r'(?:^|\s)"?-projectPath"?\s+(?:"([^"]+)"|(\S+))', command, re.I)
        if project:
            return Path(project[1] or project[2]).resolve().is_relative_to(root)
        # Asset import workers have no project argument of their own. Only a
        # verified ancestor Editor with an explicit project can establish their
        # ownership; an unknown process or ordinary external launcher cannot.
        seen = {process.get('ProcessId')}
        parent = process.get('ParentProcessId')
        while processes and parent in processes and parent not in seen:
            seen.add(parent)
            ancestor = processes[parent]
            if ancestor.get('Name') == 'Unity.exe':
                inherited = re.search(r'(?:^|\s)"?-projectPath"?\s+(?:"([^"]+)"|(\S+))', ancestor.get('CommandLine') or '', re.I)
                if inherited:
                    return Path(inherited[1] or inherited[2]).resolve().is_relative_to(root)
            parent = ancestor.get('ParentProcessId')
        # Unknown Editors still conservatively block.
        return True
    path = process.get('ExecutablePath')
    return not path or Path(path).resolve().is_relative_to(root)


def inspect_workspace(root):
    raw = subprocess.check_output([
        'powershell', '-NoProfile', '-Command',
        "Get-CimInstance Win32_Process | Where-Object { $_.Name -in @('Unity.exe','TruckSports.exe','DeadAir.exe') } | Select-Object ProcessId,ParentProcessId,Name,ExecutablePath,CommandLine | ConvertTo-Json -Compress",
    ], text=True).strip()
    rows = json.loads(raw) if raw else []
    if isinstance(rows, dict):
        rows = [rows]
    # Do not expose raw command lines: unrelated Hub processes can contain tokens.
    processes = {p['ProcessId']: p for p in rows}
    return dict(owned=[dict(pid=p['ProcessId'], name=p['Name']) for p in rows if workspace_process(p, root, processes)],
                unrelated=[dict(pid=p['ProcessId'], name=p['Name']) for p in rows if not workspace_process(p, root, processes)])


def require_idle_workspace(root):
    state = inspect_workspace(root)
    assert not state['owned'], 'Wait for workspace Editor/player: ' + json.dumps(state['owned'])
    return state
