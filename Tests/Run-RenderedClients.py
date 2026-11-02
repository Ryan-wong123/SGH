"""Run two real SGH windows under an isolated X display, using dedicated Photon.

The CI runner supplies Xvfb/openbox/xdotool. Missing IDs, windows, input evidence,
reports, session completion or a failed child exit make this command fail.
"""
import argparse
import json
import os
from pathlib import Path
import subprocess
import time
import uuid


def run(player, config, directory, timeout=600):
    settings = json.loads(config.read_text())
    uuid.UUID(settings["appId"])
    directory.mkdir(parents=True, exist_ok=False)  # Never consume stale success reports.
    room = "sgh-rendered-" + uuid.uuid4().hex
    processes = {}
    files = {}
    injected = set()
    mouse_injected = set()
    cursor_keys = set()
    clicked = set()
    window_ids = {}
    deadline = time.monotonic() + timeout
    try:
        for role in ("host", "guest"):
            files[role] = open(directory / (role + "-stdout.log"), "w")
            processes[role] = subprocess.Popen([
                str(player), "-sgh-rendered", "-sgh-role", role,
                "-sgh-room", room, "-sgh-results", str(directory),
                "-sgh-config", str(config), "-screen-width", "1280", "-screen-height", "720",
                "-screen-fullscreen", "0", "-logFile", str(directory / (role + "-player.log")),
            ], stdout=files[role], stderr=subprocess.STDOUT)
        while time.monotonic() < deadline:
            reports = {}
            for role, process in processes.items():
                path = directory / (role + ".json")
                try:
                    reports[role] = json.loads(path.read_text())
                except (FileNotFoundError, json.JSONDecodeError):
                    pass  # Report writes are short and can overlap polling.
                if reports.get(role, {}).get("status") == "failed":
                    raise RuntimeError(role + " failed: " + reports[role].get("error", "no diagnostic"))
                if process.poll() is not None and (process.returncode or reports.get(role, {}).get("status") != "passed"):
                    raise RuntimeError(role + " exited without a passing report")
                if reports.get(role, {}).get("status") == "input-ready" and role not in injected:
                    windows = subprocess.check_output([
                        "xdotool", "search", "--onlyvisible", "--pid", str(process.pid)
                    ], text=True).splitlines()
                    if not windows:
                        raise RuntimeError(role + " has no rendered window")
                    subprocess.run(["xdotool", "windowactivate", "--sync", windows[-1]], check=True)
                    window_ids[role] = windows[-1]
                    subprocess.run(["xdotool", "keydown", "w"], check=True)
                    try:
                        time.sleep(1.6)
                    finally:
                        subprocess.run(["xdotool", "keyup", "w"], check=True)
                    injected.add(role)
                if reports.get(role, {}).get("status") == "mouse-ready" and role not in mouse_injected:
                    subprocess.run(["xdotool", "windowactivate", "--sync", window_ids[role]], check=True)
                    # Focus/relative-pointer capture can discard the first X event.
                    # Deliver motion over multiple rendered frames after keyboard evidence.
                    time.sleep(.3)
                    for _ in range(20):
                        subprocess.run(["xdotool", "mousemove_relative", "--sync", "--", "12", "3"], check=True)
                        time.sleep(.05)
                    mouse_injected.add(role)
                if reports.get(role, {}).get("status") == "cursor-ready" and role not in cursor_keys:
                    subprocess.run(["xdotool", "windowactivate", "--sync", window_ids[role]], check=True)
                    subprocess.run(["xdotool", "key", "q"], check=True)
                    cursor_keys.add(role)
                if reports.get(role, {}).get("status") == "click-ready" and role not in clicked:
                    subprocess.run(["xdotool", "windowactivate", "--sync", window_ids[role]], check=True)
                    subprocess.run(["xdotool", "mousemove", "--window", window_ids[role],
                                    str(int(reports[role]["clickX"])), str(int(reports[role]["clickY"]))], check=True)
                    subprocess.run(["xdotool", "click", "1"], check=True)
                    clicked.add(role)
            if len(reports) == 2 and all(r.get("status") == "passed" for r in reports.values()):
                if injected != {"host", "guest"} or mouse_injected != {"host", "guest"} or clicked != {"host", "guest"} or cursor_keys != {"host", "guest"}:
                    raise RuntimeError("Passing reports without input injection are insufficient")
                for role, report in reports.items():
                    if report.get("sessions") != 2 or not all(report.get(k) for k in ("keyboard", "mouse", "click", "avatars", "chat")):
                        raise RuntimeError(role + " lacks required input/avatar/two-session evidence")
                if reports["guest"].get("observedStages") != 22:
                    raise RuntimeError("Guest did not observe every stage in both sessions")
                for process in processes.values():
                    if process.wait(timeout=15) != 0:
                        raise RuntimeError("A passing report was followed by a failed process exit")
                print("Two rendered clients passed: avatars, real keyboard/mouse, 22 observed stages and two sessions.")
                return
            time.sleep(.1)
        raise TimeoutError("Rendered clients exceeded the bounded test deadline")
    finally:
        for process in processes.values():
            if process.poll() is None:
                process.terminate()
                try:
                    process.wait(timeout=10)
                except subprocess.TimeoutExpired:
                    process.kill()
                    process.wait(timeout=10)
        for stream in files.values():
            stream.close()
        # Dedicated IDs remain outside artifacts even if a dependency logs settings.
        for path in directory.glob("*.log"):
            text = path.read_text(errors="replace")
            for key in ("appId", "chatAppId"):
                if settings.get(key):
                    text = text.replace(settings[key], "[dedicated-test-id]")
            path.write_text(text)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--player", type=Path, required=True)
    parser.add_argument("--config", type=Path, required=True)
    parser.add_argument("--results", type=Path, required=True)
    args = parser.parse_args()
    run(args.player.resolve(strict=True), args.config.resolve(strict=True), args.results.resolve())
