"""Publish reviewed Sports changes without publishing local integration work.

The alternate index starts from the last reviewed public Sports commit. The
working checkout, its private history and its real index remain independent.
No checkout, reset, rebase or force push is performed.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import time


def git(repo, *arguments, env=None):
    return subprocess.check_output(["git", *arguments], cwd=repo, env=env)


def fingerprint(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def validate_public_path(relative):
    """Reject private geometry/animation even if an explicit scope lists it."""
    assert relative and "\\" not in relative and not Path(relative).is_absolute() and ".." not in Path(relative).parts, relative
    assert relative in (".gitignore", "TRUCK_SPORTS_ACCEPTANCE_LEDGER.md") or relative.startswith(("Assets/Truck", "Tools/TruckSports/", "Documentation/TruckSports/")), relative
    assert not relative.startswith(("Assets/TruckTaxi", "Assets/DeadAir", "Assets/LWS")) and "PrivateLicensed" not in relative, relative
    assert not (relative.startswith("Assets/TruckBeerPong/Carnival/") and ".anim" in Path(relative).name), relative
    assert relative not in ("Assets/TruckBeerPong/Carnival/Tailgate humanoid throw.FBX", "Assets/TruckBeerPong/Carnival/Tailgate humanoid throw.FBX.meta"), relative
    assert not (relative.startswith("Assets/TruckCornhole/Carnival/Yard ") and ".anim" in Path(relative).name), relative
    for name in ("truck.fire owned participant livery.asset", "novelty.icecream owned participant livery.asset"):
        assert relative not in ("Assets/TruckSports/Customization/Meshes/" + name, "Assets/TruckSports/Customization/Meshes/" + name + ".meta"), relative


def native_unity_serialization_space(relative, header, added):
    """Recognize only reviewed native empty fields without rewriting assets."""
    if not relative.startswith("Assets/Truck") or relative.startswith(("Assets/TruckPedestrianPlowing", "Assets/TruckPedestrianPlowingFirstPerson")):
        return False
    try:
        validate_public_path(relative)
    except AssertionError:
        return False
    unity_header = b"%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n"
    if not header.startswith((unity_header, unity_header.replace(b"\n", b"\r\n"))):
        return False
    if relative.endswith((".prefab", ".controller")):
        return re.fullmatch(r"\+ +[A-Za-z_][A-Za-z0-9_]*: ", added) is not None
    if relative.endswith(".unity"):
        return re.fullmatch(r"\+ +- [A-Za-z_][A-Za-z0-9_]*: ", added) is not None
    return False


def check_whitespace(repo, parent, paths, env):
    result = subprocess.run(["git", "diff", "--cached", "--check", parent], cwd=repo, env=env,
                            capture_output=True, text=True, encoding="utf-8")
    if result.returncode == 0:
        return dict(unityEmptyFields=0, originalDocumentDiffHeaders=0)
    lines = result.stdout.splitlines()
    assert result.returncode == 2 and not result.stderr and len(lines) % 2 == 0, result.stdout + result.stderr
    review = dict(unityEmptyFields=0, originalDocumentDiffHeaders=0)
    headers = {}
    for header, added in zip(lines[::2], lines[1::2]):
        match = re.fullmatch(r"(.+):[0-9]+: trailing whitespace\.", header)
        assert match and match[1] in paths, header
        path = match[1]
        # Unity writes empty string fields with a space after the colon. Keep
        # the exact natively tested serialization instead of rewriting scenes.
        if path not in headers:
            with (repo / path).open("rb") as stream:
                headers[path] = stream.read(64)
        unity_yaml = path.endswith((".unity", ".asset", ".mat")) and headers[path].startswith(b"%YAML")
        unity_meta = path.startswith("Assets/") and path.endswith(".meta") and headers[path].startswith(b"fileFormatVersion: 2")
        if ((unity_yaml or unity_meta) and re.fullmatch(r"\+ +[A-Za-z_][A-Za-z0-9_]*: +", added)) or native_unity_serialization_space(path, headers[path], added):
            review["unityEmptyFields"] += 1
        # Retain the exact difflib audit, including its unnamed-file headers.
        elif path.startswith("Documentation/TruckSports/Evidence/") and path.endswith("-original-document-diffs.txt") and added in ("+--- ", "++++ "):
            review["originalDocumentDiffHeaders"] += 1
        else:
            raise AssertionError("Unreviewed whitespace error: " + header + "\n" + added)
    return review


def scoped_commit(repo, parent, paths, message, scratch):
    """Create a commit from a public parent using only explicit working files."""
    repo, scratch = Path(repo).resolve(), Path(scratch).resolve()
    assert re.fullmatch(r"[a-f0-9]{40}", parent)
    assert paths and len(paths) == len(set(paths))
    for relative in paths:
        path = repo / relative
        assert not Path(relative).is_absolute() and ".." not in Path(relative).parts
        assert path.is_file() and path.resolve().is_relative_to(repo)
    scratch.mkdir(parents=True, exist_ok=False)
    index = scratch / "publication.index"
    assert index.resolve() != (repo / ".git/index").resolve()
    env = os.environ.copy()
    env["GIT_INDEX_FILE"] = str(index)
    git(repo, "read-tree", parent, env=env)
    spec = scratch / "paths.nul"
    spec.write_bytes(b"\0".join(p.encode("utf-8") for p in paths) + b"\0")
    git(repo, "add", "--pathspec-from-file=" + str(spec), "--pathspec-file-nul", env=env)
    staged = set(filter(None, git(repo, "diff", "--cached", "--name-only", "-z", parent, env=env).decode().split("\0")))
    assert staged == set(paths), ("Review scope must contain exactly changed files", staged ^ set(paths))
    whitespace = check_whitespace(repo, parent, paths, env)
    (scratch / "whitespace-review.json").write_text(json.dumps(whitespace, indent=2))
    tree = git(repo, "write-tree", env=env).decode().strip()
    body = scratch / "message.txt"
    body.write_text(message + "\n", encoding="utf-8")
    commit = git(repo, "commit-tree", tree, "-p", parent, "-F", str(body), env=env).decode().strip()
    assert re.fullmatch(r"[a-f0-9]{40}", commit)
    return commit


def expected_local_changed_paths(repo, integration_parent, public_commit, paths):
    """Compare canonical trees: private checkpoints may already own this patch."""
    changed = set(filter(None, git(repo, "diff", "--no-renames", "--name-only", "-z",
                                   integration_parent, public_commit, "--").decode().split("\0")))
    return set(paths) & changed


def preserve_local_reviewed_paths(repo, integration_parent, paths, expected_changed, spec, message):
    """Preserve only the reviewed delta atop the unchanged private parent."""
    assert expected_changed <= set(paths), "Local preservation must stay inside the reviewed scope"
    assert git(repo, "rev-parse", "HEAD").decode().strip() == integration_parent
    assert not git(repo, "diff", "--cached", "--name-only").strip()
    git(repo, "add", "--pathspec-from-file=" + str(spec), "--pathspec-file-nul")
    staged = set(filter(None, git(repo, "diff", "--cached", "--name-only", "-z").decode().split("\0")))
    assert staged == expected_changed, ("Local reviewed delta changed", staged ^ expected_changed)
    if staged:
        git(repo, "commit", "-F", str(message))
        return git(repo, "rev-parse", "HEAD").decode().strip()
    return integration_parent


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("scope", help="Reviewed scope JSON below Logs/TruckSports/PlaytestRepair")
    parser.add_argument("public_parent")
    parser.add_argument("message")
    parser.add_argument("receipt")
    args = parser.parse_args()
    repo = Path(__file__).resolve().parents[2]
    log = repo / "Logs/TruckSports/PlaytestRepair"
    assert not (log / args.receipt).exists(), "Never overwrite a completed publication receipt"
    boundary = json.loads((log / "parallel-publication-boundary.json").read_text())
    branch = git(repo, "branch", "--show-current").decode().strip()
    assert branch == boundary["integrationBranch"]
    integration_parent = git(repo, "rev-parse", "HEAD").decode().strip()
    assert not git(repo, "diff", "--cached", "--name-only").strip(), "Preserve existing staged work"
    actual_index_entries = git(repo, "ls-files", "--stage", "-z")
    paths = json.loads((log / args.scope).read_text())
    assert paths and len(paths) == len(set(paths))
    for p in paths:
        validate_public_path(p)
    protected = [p for p in json.loads((log / "host-pending-baseline.json").read_text())
                 if not p["path"].startswith("Documentation/TruckSports/Requests/2026-10-21-")]
    assert len(protected) == 925 and not set(paths) & {p["path"] for p in protected}
    assert all(fingerprint(repo / p["path"]) == p["sha256"] for p in protected)
    promotion = json.loads((log / "canonical-build-promotion.json").read_text())
    inputs = {p["path"]: p["sha256"] for p in json.loads((log / ("build-" + promotion["buildContentSha256"] + "-sources.json")).read_text())}
    for p in paths:
        if p.startswith("Assets/") and p.endswith(".cs"):
            assert fingerprint(repo / p) == inputs[p], ("Source must match verified payload", p)
        # All reviewed asset bytes must be the validated workspace bytes,
        # including prefab/material/image content, not just scenes and GUIDs.
        if p.startswith("Assets/") and not p.endswith(".cs"):
            assert fingerprint(repo / p) == fingerprint(repo / ".sports-validation/TruckSportsPublicValidation" / p), p
    before_files = {p: fingerprint(repo / p) for p in paths}
    remote = git(repo, "ls-remote", "origin", "refs/heads/main", "refs/heads/codex/truck-sports-seven-events").decode()
    assert len(remote.splitlines()) == 2 and all(line.split()[0] == args.public_parent for line in remote.splitlines())
    scratch = log / "PublicationIndexes" / str(time.time_ns())
    public_commit = scoped_commit(repo, args.public_parent, paths, args.message, scratch)
    expected_local_changed = expected_local_changed_paths(repo, integration_parent, public_commit, paths)
    assert git(repo, "rev-parse", "HEAD").decode().strip() == integration_parent
    assert git(repo, "ls-files", "--stage", "-z") == actual_index_entries
    assert all(fingerprint(repo / p) == value for p, value in before_files.items())
    # Preserve the reviewed object even if a later network operation fails.
    recovery_ref = "refs/codex/sports-publication/" + str(time.time_ns())
    git(repo, "update-ref", recovery_ref, public_commit)
    journal_path = log / (args.receipt + ".pending.json")
    journal = dict(publicCommit=public_commit, publicParent=args.public_parent,
                   integrationParent=integration_parent, files=paths,
                   expectedLocalChangedFiles=sorted(expected_local_changed),
                   recoveryRef=recovery_ref, publicPushVerified=False)
    journal_path.write_text(json.dumps(journal, indent=2))
    # Never send integration HEAD or its unrelated local commits to the server.
    git(repo, "push", "--atomic", "origin", public_commit + ":refs/heads/main", public_commit + ":refs/heads/codex/truck-sports-seven-events")
    remote = git(repo, "ls-remote", "origin", "refs/heads/main", "refs/heads/codex/truck-sports-seven-events").decode()
    assert len(remote.splitlines()) == 2 and all(line.split()[0] == public_commit for line in remote.splitlines())
    journal["publicPushVerified"] = True
    journal["remote"] = remote
    journal_path.write_text(json.dumps(journal, indent=2))
    # Commit the same reviewed patch locally atop private integration history.
    # This commit is preserved locally and is deliberately never pushed here.
    spec = scratch / "paths.nul"
    local_commit = preserve_local_reviewed_paths(repo, integration_parent, paths, expected_local_changed,
                                                 spec, scratch / "message.txt")
    assert all(fingerprint(repo / p["path"]) == p["sha256"] for p in protected)
    assert all(fingerprint(repo / p) == value for p, value in before_files.items())
    # Public refs may advance only when they are not checked out elsewhere.
    worktrees = git(repo, "worktree", "list", "--porcelain").decode()
    for ref in ["refs/heads/main", "refs/heads/codex/truck-sports-seven-events"]:
        if "branch " + ref + "\n" in worktrees:
            continue
        found = subprocess.run(["git", "rev-parse", "--verify", ref], cwd=repo, capture_output=True, text=True)
        if found.returncode == 0:
            old = found.stdout.strip()
            assert subprocess.run(["git", "merge-base", "--is-ancestor", old, public_commit], cwd=repo).returncode == 0
            git(repo, "update-ref", ref, public_commit, old)
        else:
            git(repo, "update-ref", ref, public_commit, "0" * 40)
    receipt = dict(publicCommit=public_commit, publicParent=args.public_parent, localIntegrationCommit=local_commit,
                   localIntegrationParent=integration_parent, integrationBranch=branch, files=paths, remote=remote,
                   localChangedFiles=sorted(expected_local_changed),
                   buildContentSha256=promotion["buildContentSha256"], unrelatedFilesPreserved=925,
                   privateIntegrationHeadPushed=False, recoveryRef=recovery_ref)
    (log / args.receipt).write_text(json.dumps(receipt, indent=2))
    journal["complete"] = True
    journal["localIntegrationCommit"] = local_commit
    journal_path.write_text(json.dumps(journal, indent=2))
    boundary["publicBase"] = public_commit
    boundary["integrationHead"] = local_commit
    (log / "parallel-publication-boundary.json").write_text(json.dumps(boundary, indent=2))
    print(json.dumps({k: v for k, v in receipt.items() if k not in ("files", "remote")}))


if __name__ == "__main__":
    main()
