"""
UI automation test: launch Hero VTT, click Browse, load rebuilt/Armageddons.data,
verify 3 characters appear in the tree.

Requires: pip install pywinauto
"""

import time, os, sys, subprocess
from pywinauto import Application
from pywinauto.keyboard import send_keys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

EXE = os.path.join(ROOT,
    "HerovirtualTableTop", "HeroVirtualTabletop.WPF", "Shell",
    "HeroVirtualTableTop.Shell", "bin", "Debug", "HeroVirtualDesktop.exe")

ARMAGEDDONS = os.path.join(ROOT, "data", "crowds", "rebuilt", "Armageddons.data")

EXPECTED = {"Pre-Emptive Strike", "Spyder", "Suzerain"}


def launch_and_connect():
    print("Launching: {}".format(EXE))
    proc = subprocess.Popen([EXE], cwd=ROOT)
    print("PID: {}".format(proc.pid))

    app = Application(backend="uia")
    for _ in range(30):
        try:
            app.connect(process=proc.pid, timeout=2)
            dlg = app.top_window()
            dlg.wait("ready", timeout=5)
            print("Connected. Window: {}".format(dlg.window_text()))
            return app, dlg, proc
        except Exception as e:
            print("  waiting... ({})".format(e))
            time.sleep(1)

    raise RuntimeError("Could not connect to app")


def click_browse(dlg):
    print("Clicking Browse button...")
    # Try by AutomationId or name
    for name in ("Browse Crowd Files...", "\uf07c", "BrowseCrowdFilesButton"):
        try:
            btn = dlg.child_window(title=name, control_type="Button", found_index=0)
            btn.click_input()
            print("  Clicked: {}".format(name))
            return True
        except Exception:
            pass

    # Fallback: all buttons — print them so we can see names
    try:
        buttons = dlg.descendants(control_type="Button")
        print("  Available buttons:")
        for b in buttons:
            try:
                print("    '{}' auto_id={}".format(b.window_text(), b.automation_id()))
            except Exception:
                pass
    except Exception:
        pass
    return False


def fill_open_dialog(app):
    print("Waiting for Open dialog...")
    for _ in range(15):
        try:
            dlg = app.window(title_re=".*Open.*|.*Browse.*|.*Select.*", control_type="Window")
            dlg.wait("visible", timeout=3)
            print("  Dialog: {}".format(dlg.window_text()))
            # Set filename
            try:
                edit = dlg.child_window(control_type="Edit", found_index=0)
                edit.set_text(ARMAGEDDONS)
            except Exception:
                dlg.type_keys(ARMAGEDDONS, with_spaces=True)
            time.sleep(0.3)
            try:
                dlg.child_window(title="Open", control_type="Button").click_input()
            except Exception:
                send_keys("{ENTER}")
            print("  Submitted.")
            return True
        except Exception as e:
            time.sleep(0.5)
    return False


def get_tree_names(dlg):
    names = set()
    try:
        for item in dlg.descendants(control_type="TreeItem"):
            try:
                n = item.window_text().strip()
                if n:
                    names.add(n)
            except Exception:
                pass
    except Exception:
        pass
    return names


def main():
    app, dlg, proc = launch_and_connect()

    time.sleep(2)  # let UI settle

    if not click_browse(dlg):
        print("FAIL: Browse button not found")
        proc.terminate()
        sys.exit(1)

    if not fill_open_dialog(app):
        print("FAIL: Open dialog not found")
        proc.terminate()
        sys.exit(1)

    print("Waiting for crowd to load (3s)...")
    time.sleep(3)

    items = get_tree_names(dlg)
    print("Tree items: {}".format(sorted(items)[:20]))

    found   = EXPECTED & items
    missing = EXPECTED - items

    print()
    if missing:
        print("FAIL: characters missing from tree: {}".format(missing))
        proc.terminate()
        sys.exit(1)
    else:
        print("PASS: all 3 Armageddon characters loaded: {}".format(found))
        proc.terminate()


if __name__ == "__main__":
    main()
