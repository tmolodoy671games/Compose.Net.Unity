#!/usr/bin/env python3
"""Build an installable plugin against a local Rider SDK without downloading dependencies."""
import argparse
import os
from pathlib import Path
import shutil
import subprocess
import zipfile
import xml.etree.ElementTree as ET

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--rider", type=Path, default=Path.home() / "Applications/Rider.app")
parser.add_argument("--test", action="store_true")
args = parser.parse_args()
root = Path(__file__).resolve().parent
sdk = args.rider / "Contents" if (args.rider / "Contents").is_dir() else args.rider
java = sdk / "jbr/Contents/Home/bin"
if not java.is_dir():
    java = sdk / "jbr/bin"
libraries = list((sdk / "lib").rglob("*.jar"))
if not libraries or not (java / ("javac.exe" if os.name == "nt" else "javac")).is_file():
    parser.error("Expected an installed Rider with its bundled JDK")
classes = root / "build/classes"
if classes.exists():
    shutil.rmtree(classes)
classes.mkdir(parents=True)
classpath = os.pathsep.join(map(str, libraries))
sources = list((root / "src/main/java").rglob("*.java"))
if args.test:
    sources += list((root / "src/test/java").rglob("*.java"))
subprocess.run([str(java / "javac"), "--release", "21", "-encoding", "UTF-8", "-cp", classpath,
                "-d", str(classes), *map(str, sources)], check=True)
if args.test:
    subprocess.run([str(java / "java"), "-Djava.awt.headless=true", "-ea", "-cp",
                    str(classes) + os.pathsep + classpath, "net.compose.preview.PreviewIntegrationTest"], check=True)
jar = root / "build/compose-net-unity-preview.jar"
with zipfile.ZipFile(jar, "w", zipfile.ZIP_DEFLATED) as archive:
    for source_root in (classes, root / "src/main/resources"):
        for path in source_root.rglob("*"):
            if path.is_file() and "Test" not in path.name:
                archive.write(path, path.relative_to(source_root).as_posix())
version = ET.parse(root / "src/main/resources/META-INF/plugin.xml").getroot().findtext("version")
output = root / f"build/compose-net-unity-preview-{version}.zip"
with zipfile.ZipFile(output, "w", zipfile.ZIP_DEFLATED) as archive:
    archive.write(jar, "compose-net-unity-preview/lib/" + jar.name)
print(output)
