#!/usr/bin/env python3
"""Summarize a claude -p stream-json log (plain or .gz): duration, turns, cost, tools, research, errors, result.
Usage: summarize-log.py <log> [--commands]"""
import collections
import gzip
import json
import sys

path = sys.argv[1]
show_commands = "--commands" in sys.argv
opener = gzip.open if path.endswith(".gz") else open
tools = collections.Counter()
research, errors, commands, questions = [], [], [], []
result = None
pending = {}
with opener(path, "rt") as f:
    for line in f:
        try:
            o = json.loads(line)
        except json.JSONDecodeError:
            continue
        if o.get("type") == "result":
            result = o
        msg = o.get("message") if isinstance(o.get("message"), dict) else {}
        content = msg.get("content") if isinstance(msg.get("content"), list) else []
        if o.get("type") == "assistant":
            for c in content:
                if c.get("type") != "tool_use":
                    continue
                name, inp = c["name"], c.get("input", {})
                tools[name] += 1
                pending[c["id"]] = (name, inp)
                if name == "Bash":
                    cmd = inp.get("command", "")
                    commands.append(cmd.replace("\n", " ")[:160])
                    if any(k in cmd for k in ("ctx7", "learn-cli", "mslearn", "llms.txt", "curl -s", "git clone", "WebFetch")):
                        research.append("Bash: " + cmd.replace("\n", " ")[:160])
                elif name in ("WebFetch", "WebSearch"):
                    research.append(f"{name}: {inp.get('url') or inp.get('query')}")
                elif name == "Skill":
                    research.append(f"Skill: {inp.get('skill')} {str(inp.get('args', ''))[:100]}")
                elif name == "AskUserQuestion":
                    questions.append(json.dumps(inp)[:300])
        elif o.get("type") == "user":
            for c in content:
                if c.get("type") == "tool_result" and c.get("is_error"):
                    name, inp = pending.get(c.get("tool_use_id"), ("?", {}))
                    text = c.get("content")
                    text = text if isinstance(text, str) else json.dumps(text)
                    errors.append(f"{name}: {text[:200]}".replace("\n", " "))

if result:
    secs = (result.get("duration_ms") or 0) / 1000
    print(f"{result.get('subtype')}  duration {secs / 60:.1f} min  turns {result.get('num_turns')}  cost ${result.get('total_cost_usd', 0):.2f}")
else:
    print("NO RESULT LINE (run aborted?)")
print("tools:", ", ".join(f"{k} {v}" for k, v in tools.most_common()))
print(f"research ({len(research)}):")
for r in research[:40]:
    print("  ", r)
print(f"tool errors ({len(errors)}):")
for e in errors[:25]:
    print("  ", e)
if questions:
    print("questions:", *questions, sep="\n  ")
if show_commands:
    print("bash commands:")
    for c in commands:
        print("  ", c)
if result:
    print("--- result:")
    print((result.get("result") or "")[:4000])
