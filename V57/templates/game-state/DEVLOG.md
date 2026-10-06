# DEVLOG — `<slug>`

Append-only work journal. One entry per work block (≈ every gate or every 30–60 min), newest last. At session start/resume V57 reads the **last 3 entries**. Keep entries short and factual; link evidence instead of pasting it.

## Entry template

```markdown
## <UTC> · <stage> · <session id>
- **Did:** <1–3 bullets>
- **Result:** gate <pass|fail|running> · gold path <green|red|n/a> · commit <sha>
- **Evidence:** <paths>
- **Decisions:** D-### …
- **Next:** <the next concrete action>
GF-PROGRESS stage=<id> spec=<spec|none> done=<n> total=<m> gate=<pass|fail|running> status_word=<implemented|agent-verified> action="<short>"
```

---

<!-- entries below, newest last -->
