"""Read the TRX files of one gate run and judge them as a RED or a GREEN state.

Usage:
  python failed_tests.py --trx-dir DIR --expect green
  python failed_tests.py --trx-dir DIR --expect red --base SHA [--allow-green Name1,Name2]

Exit code: 0 = PASS, 1 = FAIL, 2 = no results (the build or the test run did not happen).
"""

import argparse
import pathlib
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

TRX_NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}

# A failure with one of these words comes from the machine, not from the code under test.
ENVIRONMENT_FAILURE = re.compile(
    r"docker|testcontainers|connection refused|could not connect|PendingModelChangesWarning",
    re.IGNORECASE,
)

TEST_ATTRIBUTE = re.compile(r"\[(Fact|Theory)\b")
METHOD_SIGNATURE = re.compile(
    r"^\s*(?:public|internal|private)?\s*(?:static\s+)?(?:async\s+)?(?:Task|ValueTask|void)\s+(\w+)\s*\("
)
CLASS_DECLARATION = re.compile(r"\bclass\s+(\w+)")


def read_results(trx_dir):
    results = []
    for trx in sorted(pathlib.Path(trx_dir).rglob("*.trx")):
        root = ET.parse(trx).getroot()
        classes = {}
        for unit_test in root.iterfind(".//t:UnitTest", TRX_NS):
            method = unit_test.find("t:TestMethod", TRX_NS)
            if method is not None:
                classes[unit_test.get("id")] = (method.get("className", ""), method.get("name", ""))
        for result in root.iterfind(".//t:UnitTestResult", TRX_NS):
            class_name, method_name = classes.get(result.get("testId"), ("", ""))
            if not method_name:
                bare = result.get("testName", "").split("(")[0]
                class_name, _, method_name = bare.rpartition(".")
            message = result.findtext(".//t:ErrorInfo/t:Message", default="", namespaces=TRX_NS)
            results.append(
                {
                    "name": result.get("testName", ""),
                    "class": class_name.rpartition(".")[2].split("+")[-1],
                    "method": method_name,
                    "outcome": result.get("outcome", ""),
                    "message": message.strip().splitlines()[0] if message.strip() else "",
                }
            )
    return results


def git(*args):
    return subprocess.run(["git", *args], capture_output=True, text=True, encoding="utf-8", check=True).stdout


def changed_test_classes(base):
    classes = set()
    for path in git("diff", "--name-only", base, "HEAD", "--", "tests/").splitlines():
        if path.endswith(".cs") and pathlib.Path(path).exists():
            classes.update(CLASS_DECLARATION.findall(pathlib.Path(path).read_text(encoding="utf-8")))
    return classes


def added_test_methods(base):
    methods = []
    waiting_for_signature = False
    for line in git("diff", "-U0", base, "HEAD", "--", "tests/").splitlines():
        if not line.startswith("+") or line.startswith("+++"):
            continue
        added = line[1:]
        if TEST_ATTRIBUTE.search(added):
            waiting_for_signature = True
            continue
        signature = METHOD_SIGNATURE.match(added)
        if waiting_for_signature and signature:
            methods.append(signature.group(1))
            waiting_for_signature = False
    return methods


def judge_green(results):
    failed = [r for r in results if r["outcome"] == "Failed"]
    return [f"FAILED {r['name']} :: {r['message']}" for r in failed]


def judge_red(results, base, allowed_green):
    problems = []
    failed = [r for r in results if r["outcome"] == "Failed"]
    if not failed:
        problems.append("NO_RED: no test fails. The new tests do not check new behaviour.")

    touched_classes = changed_test_classes(base)
    for r in failed:
        if ENVIRONMENT_FAILURE.search(r["message"]):
            problems.append(f"ENVIRONMENT {r['name']} :: {r['message']}")
        elif r["class"] not in touched_classes:
            problems.append(f"OLD_TEST_BROKEN {r['name']} :: {r['message']}")

    failed_methods = {r["method"] for r in failed}
    run_methods = {r["method"] for r in results}
    for method in added_test_methods(base):
        if method in allowed_green:
            continue
        if method not in run_methods:
            problems.append(f"NEW_TEST_NOT_RUN {method}: it is not in any gate test project.")
        elif method not in failed_methods:
            problems.append(f"NEW_TEST_GREEN {method}: it passes against the stub.")
    return problems


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--trx-dir", required=True)
    parser.add_argument("--expect", choices=["red", "green"], required=True)
    parser.add_argument("--base")
    parser.add_argument("--allow-green", default="")
    args = parser.parse_args()

    results = read_results(args.trx_dir)
    if not results:
        print("NO_RESULTS: no TRX file with test results. Did the build fail?")
        return 2

    if args.expect == "red":
        if not args.base:
            parser.error("--expect red needs --base")
        allowed = {name.strip() for name in args.allow_green.split(",") if name.strip()}
        problems = judge_red(results, args.base, allowed)
    else:
        problems = judge_green(results)

    counts = {o: sum(1 for r in results if r["outcome"] == o) for o in ("Passed", "Failed", "NotExecuted")}
    print(f"total={len(results)} passed={counts['Passed']} failed={counts['Failed']} skipped={counts['NotExecuted']}")
    for r in results:
        if r["outcome"] == "Failed":
            print(f"  failed: {r['name']} :: {r['message']}")
    for problem in problems:
        print(problem)
    print("PASS" if not problems else "FAIL")
    return 0 if not problems else 1


if __name__ == "__main__":
    sys.exit(main())
