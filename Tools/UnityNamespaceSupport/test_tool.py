"""Tests use disposable copies and never patch the supplied Unity installation."""
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest

DOTNET, TOOL, GENERATOR = sys.argv[1:4]
sys.argv = sys.argv[:1]


class NamespaceSupportTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        self.path = Path(self.directory.name) / "Unity.SourceGenerators.dll"
        shutil.copyfile(GENERATOR, self.path)
        self.original = self.path.read_bytes()

    def run_tool(self, command, *extra, expected=0):
        result = subprocess.run([DOTNET, TOOL, command, str(self.path), *map(str, extra)],
                                capture_output=True, text=True)
        self.assertEqual(result.returncode, expected, result.stdout + result.stderr)
        return result

    def test_check_is_read_only(self):
        self.run_tool("--check", expected=1)
        self.assertEqual(self.original, self.path.read_bytes())

    def test_prepare_does_not_change_input(self):
        target = self.path.parent / "patched.dll"
        self.run_tool("--prepare", target)
        self.assertEqual(self.original, self.path.read_bytes())
        self.assertNotEqual(self.original, target.read_bytes())

    def test_apply_is_idempotent_and_restore_is_exact(self):
        self.run_tool("--apply")
        patched = self.path.read_bytes()
        self.run_tool("--check")
        self.run_tool("--apply")
        self.assertEqual(patched, self.path.read_bytes())
        self.assertEqual(self.original, Path(str(self.path) + ".file-scoped-namespace.backup").read_bytes())
        self.run_tool("--restore")
        self.assertEqual(self.original, self.path.read_bytes())
        self.run_tool("--check", expected=1)

    def test_restore_refuses_modified_generator(self):
        self.run_tool("--apply")
        modified = self.path.read_bytes() + b"modified"
        self.path.write_bytes(modified)
        self.run_tool("--restore", expected=2)
        self.assertEqual(modified, self.path.read_bytes())

    def test_restore_refuses_modified_backup(self):
        self.run_tool("--apply")
        patched = self.path.read_bytes()
        Path(str(self.path) + ".file-scoped-namespace.backup").write_bytes(b"changed backup")
        self.run_tool("--restore", expected=2)
        self.assertEqual(patched, self.path.read_bytes())

    def test_prepare_cannot_overwrite_input(self):
        self.run_tool("--prepare", self.path, expected=2)
        self.assertEqual(self.original, self.path.read_bytes())

    def test_unknown_assembly_is_rejected_without_changes(self):
        shutil.copyfile(TOOL, self.path)
        original = self.path.read_bytes()
        self.run_tool("--apply", expected=2)
        self.assertEqual(original, self.path.read_bytes())


unittest.main()
