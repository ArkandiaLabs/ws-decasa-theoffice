#!/usr/bin/env python3
"""Cases for secret-read-guard.sh, driven through the payloads Claude Code actually sends.

WHY THIS EXISTS. The guard's Bash branch has no file_path to inspect: it works out which words of
a command name a file, and that inference is subtle enough to regress without anyone noticing.
Both directions of the failure are quiet. Too strict and it denies commands that merely SPEAK a
credential's name — those denials look like the guard working, so they get worked around instead
of reported, and the workaround is to stop running the hook. Too loose and it stops blocking
anything, while still exiting 0 and printing nothing.

The hook is not imported or sourced: each case runs the real script with a real JSON payload on
stdin, the way Claude Code invokes it, and asserts on the decision it prints.

Run it with `make hooks-test`, or directly: python3 scripts/agent-hooks/tests/test_secret_read_guard.py
"""
import json
import os
import subprocess
import sys

HOOK = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "secret-read-guard.sh")

# Built rather than written literally, so that the file testing the guard is not itself a file the
# guard denies an agent from reading.
E = "." + "env"


def bash(cmd):
    return {"tool_name": "Bash", "tool_input": {"command": cmd}}


def read(path):
    return {"tool_name": "Read", "tool_input": {"file_path": path}}


# (label, payload, must_deny)
CASES = [
    # ---- a real open of a credential file: must be DENIED ---------------------------------
    ("plain read", bash("cat " + E), True),
    ("trailing glob", bash("cat " + E + "*"), True),
    ("double-quoted operand", bash('cat "src/' + E + '"'), True),
    ("single-quoted operand", bash("cat 'src/" + E + "'"), True),
    ("multi-line command", bash("cd src\ncat " + E + "\necho done"), True),
    ("after a pipe", bash("true | cat " + E), True),
    ("after &&", bash("cd src && head -1 " + E), True),
    ("inside a subshell", bash("(cat " + E + ")"), True),
    ("leading assignment", bash("ENV_FILE=" + E + " printenv"), True),
    ("output redirection", bash("echo x > " + E), True),
    ("input redirection", bash("grep -c x < " + E), True),
    ("private key", bash("cat ~/.ssh/id_rsa"), True),
    ("aws credentials", bash("cat ~/.aws/credentials"), True),
    ("pem file", bash("openssl x509 -in server.pem -text"), True),
    ("dotted variant", bash("cat " + E + ".production"), True),
    ("second operand", bash("cp backup " + E), True),
    ("Read tool", read("/repo/" + E), True),
    ("Read a pem", read("/repo/certs/server.pem"), True),

    # ---- the name is spoken, not opened: must be ALLOWED ----------------------------------
    ("echo, double quotes", bash('echo "=== LEDGER ' + E + ' ==="'), False),
    ("echo, single quotes", bash("echo 'see " + E + " for the value'"), False),
    ("printf", bash('printf "%s\\n" "' + E + ' is ignored"'), False),
    ("commit -m", bash('git commit -m "chore: ignore ' + E + ' files"'), False),
    ("commit --message", bash('git commit --message "ignore ' + E + '"'), False),
    ("heredoc body", bash("python3 - <<'PY'\nold = 'entry for " + E + "'\nPY"), False),
    ("heredoc, bare delimiter", bash("cat <<EOF\nmentions " + E + " inline\nEOF"), False),
    ("heredoc, <<- form", bash("cat <<-EOF\n\tabout " + E + "\n\tEOF"), False),
    ("here-string", bash('grep x <<< "' + E + ' is prose"'), False),
    ("quoted sentence argument", bash("grep -n 'the " + E + " file' notes.txt"), False),
    ("grep over the gitignore", bash("grep -n -i 'env' .gitignore"), False),

    # ---- the template is what the agent should read instead: must be ALLOWED --------------
    ("template file", bash("cat " + E + ".example"), False),
    ("template, quoted", bash('cat "' + E + '.sample"'), False),
    ("Read the template", read("/repo/" + E + ".example"), False),

    # ---- nothing to do with credentials: must be ALLOWED ----------------------------------
    ("git status", bash("git status --short"), False),
    ("unrelated file", bash("cat global.json"), False),
    ("the word env alone", bash("printenv | grep PATH"), False),
    ("directory named env", bash("ls src/env/"), False),
    ("environment.ts", bash("cat src/environment.ts"), False),
    ("keys directory", bash("ls deploy/keys/"), False),
    ("dotnet user-secrets", bash("dotnet user-secrets list"), False),
]

# The three commands the guard denied in error on 2026-08-25, kept verbatim. Each one edits or
# describes the credential convention without opening anything.
REGRESSIONS = [
    ("grep over the gitignore, real",
     "grep -n -i 'env\\|secret\\|appsettings' .gitignore | head -20; "
     'echo "--- tail ---"; tail -20 .gitignore; '
     'echo "=== LEDGER ' + E + ' ==="; '
     "grep -n 'gitignore' docs/claims-ledger.md"),

    ("python heredoc editing the ledger",
     "python3 - <<'PY'\n"
     "import io\n"
     "p='docs/claims-ledger.md'\n"
     "old='| `.gitignore` has no entry for `" + E + "`. | ...'\n"
     "PY"),

    ("commit message naming the file",
     "git add -A && git commit -q -F - <<'EOF'\n"
     "chore: ignore credential files\n"
     "\n"
     "Ignores " + E + ", " + E + ".* and appsettings.Secrets.json.\n"
     "EOF"),
]


def decide(payload):
    """Run the hook the way Claude Code does. Returns (denied, exit_code, stderr)."""
    p = subprocess.run(["bash", HOOK], input=json.dumps(payload),
                       capture_output=True, text=True, timeout=30)
    return '"permissionDecision":"deny"' in p.stdout, p.returncode, p.stderr.strip()


def main():
    cases = CASES + [(label, bash(cmd), False) for label, cmd in REGRESSIONS]
    failed = []

    for label, payload, must_deny in cases:
        denied, code, err = decide(payload)
        ok = denied == must_deny and code == 0
        # A non-zero exit is a failure whatever the decision was: the hook must never break the
        # session, and a crash that happens to print nothing reads exactly like an allow.
        note = "" if code == 0 else "  (exit %d %s)" % (code, err)
        print("%s  %-32s want=%-5s got=%s%s" % (
            "ok  " if ok else "FAIL", label,
            "deny" if must_deny else "allow",
            "deny" if denied else "allow", note))
        if not ok:
            failed.append(label)

    print("\n%d/%d passed" % (len(cases) - len(failed), len(cases)))
    if failed:
        print("failed: " + ", ".join(failed))
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
