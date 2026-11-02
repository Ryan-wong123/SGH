import importlib.util
import json
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location("rendered_runner", Path(__file__).with_name("Run-RenderedClients.py"))
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)


class RunnerContracts(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.config = self.root / "config.json"
        self.identifier = "00000000-0000-0000-0000-000000000001"
        self.config.write_text(json.dumps({"appId": self.identifier}))
        self.output = self.root / "results"
        self.children = []
        self.commands = []

    def report(self, role, status="passed", keyboard=True):
        (self.output / (role + ".json")).write_text(json.dumps({
            "status": status, "sessions": 2, "observedStages": 22,
            "keyboard": keyboard, "mouse": True, "click": True, "avatars": True, "chat": True,
            "clickX": 20, "clickY": 20,
        }))

    def launch(self, args, stdout, stderr):
        role = args[args.index("-sgh-role") + 1]
        self.report(role, "input-ready")
        stdout.write(self.identifier)
        owner = self

        class Child:
            pid = 100 + len(owner.children)
            returncode = None
            stopped = False

            def poll(child):
                data = json.loads((owner.output / (role + ".json")).read_text())
                child.returncode = 0 if data["status"] == "passed" else None
                return child.returncode

            def wait(child, timeout):
                return 0

            def terminate(child):
                child.stopped = True

            def kill(child):
                child.stopped = True

        child = Child()
        self.children.append(child)
        return child

    def command(self, args, check):
        self.commands.append(args)
        if args[1] == "windowactivate":
            self.current = "host" if args[-1] == "100" else "guest"
        if args[1] == "keyup":
            self.report(self.current, "mouse-ready")
        if args[1] == "mousemove_relative":
            self.report(self.current, "cursor-ready")
        if args[1] == "key":
            self.report(self.current, "click-ready")
        if args[1] == "click":
            self.report(self.current)

    def execute(self, command=None, window=lambda args, text: args[-1] + "\n"):
        with patch.object(runner.subprocess, "Popen", side_effect=self.launch), \
             patch.object(runner.subprocess, "check_output", side_effect=window), \
             patch.object(runner.subprocess, "run", side_effect=command or self.command), \
             patch.object(runner.time, "sleep"), patch("builtins.print"):
            runner.run(self.root / "player", self.config, self.output, timeout=2)

    def test_both_windows_require_input_and_complete_evidence_and_logs_are_redacted(self):
        self.execute()
        self.assertEqual(2, len([c for c in self.commands if c[1] == "keydown"]))
        self.assertEqual(40, len([c for c in self.commands if c[1] == "mousemove_relative"]))
        for log in self.output.glob("*.log"):
            self.assertNotIn(self.identifier, log.read_text())

    def test_missing_window_fails_and_terminates_both_players(self):
        with self.assertRaisesRegex(RuntimeError, "no rendered window"):
            self.execute(window=lambda args, text: "")
        self.assertTrue(all(c.stopped for c in self.children))

    def test_mouse_injection_failure_releases_key_and_terminates_players(self):
        def fail_mouse(args, check):
            self.command(args, check)
            if args[1] == "mousemove_relative":
                raise subprocess.CalledProcessError(1, args)
        with self.assertRaises(subprocess.CalledProcessError):
            self.execute(command=fail_mouse)
        self.assertTrue(any(c[1] == "keyup" for c in self.commands))
        self.assertTrue(any(c.stopped for c in self.children))

    def test_missing_keyboard_evidence_fails(self):
        def incomplete(args, check):
            self.command(args, check)
            if args[1] == "click":
                self.report(self.current, keyboard=False)
        with self.assertRaisesRegex(RuntimeError, "lacks required"):
            self.execute(command=incomplete)

    def test_stale_result_directory_is_rejected(self):
        self.output.mkdir()
        with self.assertRaises(FileExistsError):
            self.execute()

    def test_passing_report_without_window_input_is_rejected(self):
        original = self.launch
        def shortcut(args, stdout, stderr):
            child = original(args, stdout, stderr)
            self.report(args[args.index("-sgh-role") + 1])
            return child
        self.launch = shortcut
        with self.assertRaisesRegex(RuntimeError, "without input injection"):
            self.execute()


if __name__ == "__main__":
    unittest.main()
