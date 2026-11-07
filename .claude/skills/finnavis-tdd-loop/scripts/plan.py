"""Plan which tasks of an epic finnavis-tdd-loop runs, and in which order.

Reads docs/features/<slug>/tasks/tasks.json and tracker.md, and the Task trailer of HEAD.
Prints the plan as text. The last line is always one of:

    NEXT: <id>      the task to run now
    NEXT: none      nothing left that finnavis-tdd can take
    ERROR: <text>   the plan cannot be built; stop

With --dry-run, a stopped run of another feature at HEAD is a warning instead of an ERROR,
so the plan is still printed.

Usage: python plan.py <slug> [--repo <path>] [--dry-run]
"""

import argparse
import json
import re
import subprocess
import sys
from pathlib import Path

TDD_LAYERS = {"domain", "app", "infra", "wiring"}

LAYER_REASON = {
    "docs": "docs task, nothing to test",
    "migration": "migration task, EF Core generates the code",
    "ports": "ports task, there is no HTTP test harness yet",
    "tests": "tests-only task, no production code to drive",
}


def task_number(task_id):
    match = re.search(r"\d+", task_id)
    return int(match.group()) if match else 0


def read_tracker_statuses(tracker_path):
    statuses = {}
    for line in tracker_path.read_text(encoding="utf-8").splitlines():
        cells = [cell.strip() for cell in line.strip().strip("|").split("|")]
        if len(cells) >= 2 and re.fullmatch(r"T\d+", cells[0]):
            statuses[cells[0]] = cells[-1].strip("`* ").lower()
    return statuses


def head_task_trailer(repo):
    result = subprocess.run(
        ["git", "log", "-1", "--format=%(trailers:key=Task,valueonly)"],
        cwd=repo, capture_output=True, text=True, check=False,
    )
    return result.stdout.strip()


def fail(message):
    print(f"ERROR: {message}")
    sys.exit(1)


def build_plan(tasks, statuses, resume_id):
    done = {tid for tid, status in statuses.items() if status == "done"}
    by_id = {task["id"]: task for task in tasks}

    run, wrong_layer, not_todo, blocked = [], [], [], {}

    for task in tasks:
        status = statuses[task["id"]]
        if status == "done":
            continue
        if status != "todo":
            not_todo.append((task["id"], status))
        elif task["layer"] not in TDD_LAYERS:
            wrong_layer.append(task["id"])

    candidates = [
        t["id"] for t in tasks
        if statuses[t["id"]] == "todo" and t["layer"] in TDD_LAYERS
    ]

    # A task a stopped run left half-done goes first, so finnavis-tdd resumes it on top of
    # its own RED or GREEN commit instead of a later task starting on top of it.
    if resume_id in candidates:
        run.append(resume_id)
        done.add(resume_id)
        candidates.remove(resume_id)

    pending = sorted(candidates, key=task_number)
    progress = True
    while pending and progress:
        progress = False
        for tid in pending:
            if all(dep in done for dep in by_id[tid]["deps"]):
                run.append(tid)
                done.add(tid)
                pending.remove(tid)
                progress = True
                break

    for tid in pending:
        missing = [dep for dep in by_id[tid]["deps"] if dep not in done]
        blocked[tid] = missing

    return run, wrong_layer, not_todo, blocked


def describe_dep(dep, by_id, statuses):
    task = by_id.get(dep)
    layer = task["layer"] if task else "?"
    return f"{dep} ({statuses.get(dep, 'unknown')}, {layer})"


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("slug")
    parser.add_argument("--repo", default=".")
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()

    repo = Path(args.repo)
    tasks_dir = repo / "docs" / "features" / args.slug / "tasks"
    tasks_json = tasks_dir / "tasks.json"
    tracker = tasks_dir / "tracker.md"

    if not tasks_json.is_file():
        fail(f"{tasks_json.as_posix()} does not exist")
    if not tracker.is_file():
        fail(f"{tracker.as_posix()} does not exist")

    tasks = json.loads(tasks_json.read_text(encoding="utf-8"))["tasks"]
    statuses = read_tracker_statuses(tracker)
    by_id = {task["id"]: task for task in tasks}

    missing_rows = [t["id"] for t in tasks if t["id"] not in statuses]
    if missing_rows:
        fail(f"tracker.md has no row for {', '.join(missing_rows)}")

    trailer = head_task_trailer(repo)
    resume_id = None
    if trailer:
        trailer_slug, _, trailer_id = trailer.partition("/")
        if trailer_slug == args.slug:
            if trailer_id in by_id and statuses[trailer_id] != "done":
                resume_id = trailer_id
        else:
            other_tracker = repo / "docs" / "features" / trailer_slug / "tasks" / "tracker.md"
            other_status = (
                read_tracker_statuses(other_tracker).get(trailer_id)
                if other_tracker.is_file() else None
            )
            if other_status != "done":
                message = (
                    f"HEAD is a stopped run of {trailer}, not of {args.slug}. "
                    f"Finish it first: /finnavis-tdd {trailer_slug} {trailer_id}"
                )
                if not args.dry_run:
                    fail(message)
                print(f"Warning: {message}")
                print()

    run, wrong_layer, not_todo, blocked = build_plan(tasks, statuses, resume_id)

    done_count = sum(1 for status in statuses.values() if status == "done")
    print(f"Plan for {args.slug}: {done_count} of {len(tasks)} tasks already done")
    print()
    print(f"Run, in this order ({len(run)}):")
    for tid in run:
        note = "  <- resumes a stopped run" if tid == resume_id else ""
        print(f"  {tid}  {by_id[tid]['layer']:<9} {by_id[tid]['title']}{note}")
    if not run:
        print("  none")

    print()
    print(f"Skipped, layer finnavis-tdd does not take ({len(wrong_layer)}):")
    for tid in wrong_layer:
        layer = by_id[tid]["layer"]
        print(f"  {tid}  {layer:<9} {LAYER_REASON.get(layer, 'layer not supported')}")
    if not wrong_layer:
        print("  none")

    print()
    print(f"Skipped, a dependency will not be done ({len(blocked)}):")
    for tid, missing in blocked.items():
        waits = ", ".join(describe_dep(dep, by_id, statuses) for dep in missing)
        print(f"  {tid}  {by_id[tid]['layer']:<9} waits for {waits}")
    if not blocked:
        print("  none")

    print()
    print(f"Left alone, status is not todo ({len(not_todo)}):")
    for tid, status in not_todo:
        print(f"  {tid}  {status}")
    if not not_todo:
        print("  none")

    print()
    print(f"NEXT: {run[0] if run else 'none'}")


if __name__ == "__main__":
    main()
