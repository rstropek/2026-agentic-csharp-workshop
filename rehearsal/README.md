# Rehearsal

Every prompt of [storyboard.md](../storyboard.md) is rehearsed with `claude -p` (Opus 5.5, effort high, like on stage).

| Path | Content |
| --- | --- |
| `prompts/` | the prompts, byte-identical to the prompt blocks in the storyboard |
| `scripts/` | `rehearse.sh` (the whole storyboard: setup scripts, prompts, checks), `run-agent.sh`, the setup scripts of steps 1 and 2 |
| `snapshot/` | the Grader repository after the last rehearsed step. Every step is one commit in this repository ("Rehearsal …: step …"), so `git log -- rehearsal/snapshot` shows what the agent built in each step |
| `logs/` | gzipped agent transcripts (`stepX-<run>.jsonl.gz`, stream-json of `claude -p`) and the console output of the run (`rehearse-<run>.out`) |
| `grader.bundle` | the rehearsal's Grader git repository incl. tags `step1` … `step11c` (fallback on stage) |

Not tracked: `grader/` (the working repository of the rehearsal) and `openrouter.env` (`export OpenRouter__ApiKey=...` for step 11).

Run (from this folder): `./scripts/rehearse.sh <run-suffix> [first-step] [last-step]`, e.g. `./scripts/rehearse.sh r2 7b 8a`.

Fallback on stage, e.g. continue after step 7b without running its prompt:

```bash
git clone rehearsal/grader.bundle grader && cd grader && git reset --hard step7b
```
