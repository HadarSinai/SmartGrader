# #!/usr/bin/env bash
# set -euo pipefail

# N="${1:-1}"

# echo "=== SmartGrader Client: verify (N=$N) ==="
# cd "$(dirname "$0")"

# echo "[1/3] Install deps"
# npm ci || npm install

# has_test_script() {
#   node -e "const p=require('./package.json'); process.exit(p.scripts && p.scripts.test ? 0 : 1)"
# }

# run_tests_best_effort() {
#   echo "[test] trying: npm test -- --watch=false"
#   if npm test -- --watch=false; then
#     return 0
#   fi

#   echo "[test] fallback: npm test -- --watch=false --browsers=ChromeHeadless"
#   if npm test -- --watch=false --browsers=ChromeHeadless; then
#     return 0
#   fi

#   echo "[test] fallback: npm test (no extra args)"
#   npm test
# }

# echo "[2/3] Build/Test loop"
# for i in $(seq 1 "$N"); do
#   echo "---- Iteration $i/$N ----"
#   npm run build

#   if has_test_script; then
#     run_tests_best_effort
#   else
#     echo "(skip) No test script in package.json"
#   fi
# done

# echo "[3/3] Done ✅"
#!/usr/bin/env bash
#!/usr/bin/env bash
set -euo pipefail

N="${1:-1}"
PROJECT="grading-system-frontend"

echo "=== SmartGrader Client: verify (N=$N) ==="
cd "$(dirname "$0")"

echo "[1/3] Install deps"
npm ci || npm install

has_test_target() {
  node -e "
    const a=require('./angular.json');
    const p=a.projects?.['$PROJECT'];
    const arch = p?.architect || p?.targets;
    process.exit(arch?.test ? 0 : 1);
  "
}

echo "[2/3] Build/Test loop"
for i in $(seq 1 "$N"); do
  echo "---- Iteration $i/$N ----"
  npm run build

  if has_test_target; then
    echo "[test] ng test $PROJECT"
    ng test "$PROJECT"
  else
    echo "[test] (skip) No 'test' target in angular.json for $PROJECT"
  fi
done

echo "[3/3] Done ✅"

