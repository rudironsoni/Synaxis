#!/usr/bin/env bash
set -euo pipefail

PROJ_ABS="/home/rrj/src/github/rudironsoni/Synaxis/tests/InferenceGateway/IntegrationTests/Synaxis.InferenceGateway.IntegrationTests.csproj"
OUTDIR=".sisyphus"
NOTEPAD_DIR="$OUTDIR/notepads/synaxis-enterprise-stabilization"

mkdir -p "$OUTDIR" "$NOTEPAD_DIR"

# Clean previous artifacts (ignore errors)
rm -f "$OUTDIR/exitcodes_raw.txt" "$OUTDIR/smoke-test-results.txt" "$OUTDIR/all-failed-tests.txt" \
      "$OUTDIR/baseline-flakiness.txt" "$OUTDIR/flaky-tests.txt" "$OUTDIR/run-"*"-failed-tests.txt" "$OUTDIR/run-"*.log "$OUTDIR/smoke-"*.trx 2>/dev/null || true

for i in $(seq 1 10); do
  echo "=== Run $i ==="
  dotnet test "$PROJ_ABS" --filter "FullyQualifiedName~SmokeTests" --logger "trx;LogFileName=$OUTDIR/smoke-$i.trx" > "$OUTDIR/run-$i.log" 2>&1 || true
  rc=$?
  echo "$rc" >> "$OUTDIR/exitcodes_raw.txt"

  if [ -f "$OUTDIR/smoke-$i.trx" ]; then
    grep -o 'testName="[^"]*"[^>]*outcome="Failed"' "$OUTDIR/smoke-$i.trx" | sed -n 's/.*testName="\([^"]*\)".*/\1/p' > "$OUTDIR/run-$i-failed-tests.txt" || true
    if [ -s "$OUTDIR/run-$i-failed-tests.txt" ]; then
      while read -r t; do
        [ -z "$t" ] && continue
        echo "$t: $i" >> "$OUTDIR/all-failed-tests.txt"
      done < "$OUTDIR/run-$i-failed-tests.txt"
      echo "Run $i: FAIL" >> "$OUTDIR/smoke-test-results.txt"
    else
      # check summary in log
      if grep -q "Passed!  - Failed:" "$OUTDIR/run-$i.log" 2>/dev/null; then
        failed=$(grep "Passed!  - Failed:" "$OUTDIR/run-$i.log" | sed -n 's/.*Failed:[[:space:]]*\([0-9]*\).*/\1/p') || true
        if [ -n "${failed:-}" ] && [ "$failed" -gt 0 ]; then
          echo "Run $i: FAIL ($failed failed)" >> "$OUTDIR/smoke-test-results.txt"
        else
          echo "Run $i: PASS" >> "$OUTDIR/smoke-test-results.txt"
        fi
      else
        echo "Run $i: PASS" >> "$OUTDIR/smoke-test-results.txt"
      fi
    fi
  else
    if grep -q "Passed!  - Failed:" "$OUTDIR/run-$i.log" 2>/dev/null; then
      failed=$(grep "Passed!  - Failed:" "$OUTDIR/run-$i.log" | sed -n 's/.*Failed:[[:space:]]*\([0-9]*\).*/\1/p') || true
      if [ -n "${failed:-}" ] && [ "$failed" -gt 0 ]; then
        echo "Run $i: FAIL ($failed failed)" >> "$OUTDIR/smoke-test-results.txt"
      else
        echo "Run $i: PASS" >> "$OUTDIR/smoke-test-results.txt"
      fi
    else
      echo "Run $i: PASS" >> "$OUTDIR/smoke-test-results.txt"
    fi
  fi
done

passes=$(grep -c "PASS" "$OUTDIR/smoke-test-results.txt" || true)
fails=$(grep -c "FAIL" "$OUTDIR/smoke-test-results.txt" || true)
failure_rate=$(awk -v f="$fails" 'BEGIN{printf "%.1f", (f/10)*100}')

if [ -f "$OUTDIR/all-failed-tests.txt" ]; then
  awk -F": " '{cnt[$1]++; runs[$1]=(runs[$1]?runs[$1] " " : "") $2} END{for (t in cnt) print t ": failed in " cnt[t] " run(s) -> runs:" runs[t]}' "$OUTDIR/all-failed-tests.txt" | sort > "$OUTDIR/flaky-tests.txt"
else
  echo "No failed tests recorded" > "$OUTDIR/flaky-tests.txt"
fi

cat > "$OUTDIR/baseline-flakiness.txt" <<EOF
Smoke tests flakiness baseline
Date: $(date -u +"%Y-%m-%dT%H:%M:%SZ")
Total runs: 10
Passes: $passes
Fails: $fails
Run-level failure rate: $failure_rate % ($fails/10 runs)

Run results (from $OUTDIR/smoke-test-results.txt):
$(cat "$OUTDIR/smoke-test-results.txt")

EOF

cat >> "$NOTEPAD_DIR/issues.md" <<NOTE

### Smoke test flakiness baseline run - $(date -u +"%Y-%m-%dT%H:%M:%SZ")
- Total runs: 10
- Passes: $passes
- Fails: $fails
- Failure rate: $failure_rate% ($fails/10)
- Per-run results saved in $OUTDIR/smoke-test-results.txt
- Baseline summary: $OUTDIR/baseline-flakiness.txt
- Flaky tests list: $OUTDIR/flaky-tests.txt
NOTE

echo "Verification: smoke-test-results.txt lines=$(wc -l < "$OUTDIR/smoke-test-results.txt")"
ls -l "$OUTDIR" | sed -n '1,200p'
