#!/usr/bin/env python3
"""Pull the structured output out of a `claude -p --output-format json` response.

Used by release.yml for both the changelog and the release notes.

`claude -p --output-format json` can return the whole event stream as a JSON array. Some events
carry raw tab characters inside strings, which a strict parser such as jq refuses. `strict=False`
accepts them, so the parsing happens here and jq gets a clean object afterwards.

Usage: extract-structured.py <response.json> <out.json>
Logs the cost and the turns, exits 1 when the agent reported an error, and writes the structured
output to <out.json>.
"""

import json
import sys


def main(resp_path, out_path):
    with open(resp_path, encoding="utf-8") as fh:
        data = json.loads(fh.read(), strict=False)

    events = data if isinstance(data, list) else [data]
    results = [m for m in events if isinstance(m, dict) and m.get("type") == "result"]
    result = results[-1] if results else (data if isinstance(data, dict) else {})

    cost = result.get("total_cost_usd", "n/a")
    duration = result.get("duration_ms", "n/a")
    turns = result.get("num_turns", "n/a")
    is_error = result.get("is_error", False)
    print(f"[claude] cost=${cost} duration={duration}ms turns={turns} is_error={is_error}")

    if is_error:
        print(f"::error::claude failed: {result.get('result')}")
        return 1

    output = result.get("structured_output")
    if output is None:
        raw = result.get("result", "")
        try:
            output = json.loads(raw)
        except (TypeError, ValueError):
            print("::error::no structured output, and the result is not JSON")
            return 1

    with open(out_path, "w", encoding="utf-8") as fh:
        json.dump(output, fh, ensure_ascii=False, indent=2)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1], sys.argv[2]))
