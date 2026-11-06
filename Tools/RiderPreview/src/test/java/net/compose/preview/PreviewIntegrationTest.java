package net.compose.preview;

import com.google.gson.Gson;
import javax.imageio.ImageIO;
import javax.swing.*;
import java.awt.event.*;
import java.awt.image.BufferedImage;
import java.nio.file.*;
import java.util.*;

public final class PreviewIntegrationTest {
    public static void main(String[] args) throws Exception {
        String source = "class Previews {\n [UnityPreview(Name = \"Phone\")]\n [UnityPreview(Name = \"Tablet\")]\n [Composable]\n private static void Counter() {}\n"
            + " // [UnityPreview] private static void Comment() {}\n"
            + " string text = \"[UnityPreview] private static void Fake() {}\";\n"
            + " [Compose.Net.Unity.UnityPreviewAttribute(\n Name = \"Other\")]\n private static void Other() {}\n"
            + " private static void Ordinary() {}\n}";
        var markers = PreviewMethodLocator.find(source);
        check(markers.size() == 2 && source.substring(markers.get(0).start(), markers.get(0).end()).equals("Counter")
            && markers.get(0).line() == 5 && source.substring(markers.get(1).start(), markers.get(1).end()).equals("Other"),
            "Run markers belong to annotated method names and ignore comments/strings");
        Path root = Files.createTempDirectory("compose-preview-test");
        try {
            Files.createDirectories(root.resolve("Assets"));
            Files.createDirectories(root.resolve("ProjectSettings"));
            Files.writeString(root.resolve("ProjectSettings/ProjectVersion.txt"), "m_EditorVersion: 7000.0.0a16\n");
            check(PreviewOpening.isProject(root), "Unity project detection");
            var connection = new PreviewConnection(root, root.resolve("Assets/Example.cs").toString(), 17);
            connection.send(PreviewConnection.Request.of("open"));
            var toggle = PreviewConnection.Request.of("playInUnity"); toggle.previewId = "preview-1";
            connection.send(toggle);
            List<Path> files;
            try (var paths = Files.list(root.resolve("Library/ComposeNetPreview/requests"))) { files = paths.sorted().toList(); }
            check(files.size() == 2 && files.stream().allMatch(path -> path.toString().endsWith(".json")), "Atomic, ordered request files");
            var gson = new Gson();
            var first = gson.fromJson(Files.readString(files.get(0)), PreviewConnection.Request.class);
            var second = gson.fromJson(Files.readString(files.get(1)), PreviewConnection.Request.class);
            check("open".equals(first.op) && "playInUnity".equals(second.op), "Command order and native Unity launch");
            check(connection.clientId.equals(first.clientId) && connection.requestId.equals(first.requestId)
                && first.line == 17 && first.sourceFile.endsWith("Example.cs"), "Request source and identity");
            check(connection.readState() == null, "Waiting for Unity is a valid state");
            Files.createDirectories(connection.directory);
            var state = new PreviewConnection.State();
            state.protocol = 1; state.requestId = connection.requestId; state.previewId = "preview-1";
            state.width = 360; state.height = 240; state.heartbeat = System.currentTimeMillis(); state.revision = 1;
            Files.writeString(connection.directory.resolve("state.json"), gson.toJson(state));
            var image = new BufferedImage(360, 240, BufferedImage.TYPE_INT_ARGB);
            image.setRGB(0, 0, 0xff336699);
            ImageIO.write(image, "png", connection.directory.resolve("frame.png").toFile());
            check(connection.readState().width == 360 && connection.readFrame().getRGB(0, 0) == 0xff336699, "Frame/state transport");
            state.requestId = "old-request";
            Files.writeString(connection.directory.resolve("state.json"), gson.toJson(state));
            check(connection.readState() == null, "Old preview cannot overwrite current selection");
            state.requestId = connection.requestId; state.heartbeat -= 10000;
            Files.writeString(connection.directory.resolve("state.json"), gson.toJson(state));
            check(connection.readState() == null, "Stale Unity process is detected");
            SwingUtilities.invokeAndWait(() -> testSurface(image));
            System.out.println("PASS: request order, source identity, PNG transport, stale/reloaded hosts and static viewport rendering");
        } finally {
            try (var paths = Files.walk(root)) {
                for (Path path : paths.sorted(Comparator.reverseOrder()).toList()) Files.deleteIfExists(path);
            }
        }
    }
    private static void testSurface(BufferedImage image) {
        var surface = new PreviewSurface();
        var state = new PreviewConnection.State(); state.width = 360; state.height = 240; state.previewId = "preview-1";
        surface.setSize(180, 120); surface.showFrame(image, state);
        check(surface.imageBounds().width == 180 && surface.imageBounds().height == 120,
            "Fixed layout fits a narrow sidebar by default");
        check(surface.getScrollableTracksViewportWidth() && surface.getScrollableTracksViewportHeight(), "Fit tracks the viewport");
        surface.setSize(300, 300);
        check(surface.imageBounds().equals(new java.awt.Rectangle(0, 50, 300, 200)), "Fit preserves aspect ratio and centers both axes");
        surface.setZoom(2);
        check(surface.imageBounds().width == 600 && surface.imageBounds().height == 400, "Slider zoom is relative to Fit");
        check(!surface.getScrollableTracksViewportWidth() && !surface.getScrollableTracksViewportHeight(), "Enlarged previews can scroll");
        surface.setZoom(.5);
        check(surface.imageBounds().equals(new java.awt.Rectangle(75, 100, 150, 100)), "Zoom out centers the canvas");
        surface.setZoom(1);
        state.autoWidth = state.autoHeight = true; state.width = 180; state.height = 120;
        surface.setSize(180, 120);
        surface.showFrame(new BufferedImage(180, 120, BufferedImage.TYPE_INT_ARGB), state);
        check(surface.imageBounds().width == 180 && surface.imageBounds().height == 120, "Automatic preview fills its viewport");
        // Exercise Swing's actual scrolling layout: zoom must use the viewport extent,
        // not the enlarged surface itself, or repeated layouts would compound zoom.
        var scroll = new JScrollPane(surface);
        scroll.setSize(220, 180);
        scroll.doLayout(); scroll.getViewport().doLayout();
        surface.setZoom(2);
        for (int i = 0; i < 5; i++) { scroll.doLayout(); scroll.getViewport().doLayout(); }
        var bounds = surface.imageBounds();
        scroll.doLayout(); scroll.getViewport().doLayout();
        check(surface.imageBounds().equals(bounds), "Zoom remains stable across scroll layout passes");
        surface.setZoom(1);
        for (int i = 0; i < 5; i++) { scroll.doLayout(); scroll.getViewport().doLayout(); }
        check(surface.imageBounds().width <= scroll.getViewport().getWidth()
            && surface.imageBounds().height <= scroll.getViewport().getHeight(), "Reset to Fit removes overflow");
        check(surface.getMouseListeners().length == 0 && surface.getKeyListeners().length == 0,
            "Rider has no interactive input path");
        surface.clearFrame();
        check(surface.imageBounds().isEmpty(), "A detached preview displays no stale image");
    }
    private static void check(boolean condition, String message) { if (!condition) throw new AssertionError(message); }
}
