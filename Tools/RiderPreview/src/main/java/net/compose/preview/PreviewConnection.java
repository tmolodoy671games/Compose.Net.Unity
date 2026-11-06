package net.compose.preview;

import com.google.gson.Gson;
import javax.imageio.ImageIO;
import java.awt.image.BufferedImage;
import java.io.IOException;
import java.nio.file.*;
import java.util.*;
import java.util.regex.Pattern;

final class PreviewConnection {
    private static final Gson JSON = new Gson();
    final Path project, directory;
    final String clientId = UUID.randomUUID().toString().replace("-", "");
    final String requestId = UUID.randomUUID().toString();
    final String sourceFile;
    final int line;
    private long sequence;
    private int viewportWidth = 640, viewportHeight = 480, screenWidth, screenHeight;
    PreviewConnection(Path project, String sourceFile, int line) {
        this.project = project; this.sourceFile = sourceFile; this.line = line;
        directory = project.resolve("Library/ComposeNetPreview/clients").resolve(clientId);
    }
    static final class Request {
        String clientId, requestId, op, sourceFile, previewId = "", eventType = "", keyCode = "";
        int line = 1, button, modifiers, character, clickCount = 1, width, height, screenWidth, screenHeight;
        float x, y, deltaX, deltaY;
        boolean running;
        static Request of(String op) { var request = new Request(); request.op = op; return request; }
    }
    static final class State {
        int protocol, width, height;
        String requestId, previewId, name, error;
        boolean running, autoWidth, autoHeight;
        long heartbeat, revision;
    }
    void send(Request request) throws IOException {
        request.clientId = clientId; request.requestId = requestId;
        request.sourceFile = sourceFile; request.line = line;
        if ("open".equals(request.op)) { request.width = viewportWidth; request.height = viewportHeight; }
        request.screenWidth = screenWidth; request.screenHeight = screenHeight;
        Path inbox = project.resolve("Library/ComposeNetPreview/requests");
        Files.createDirectories(inbox);
        String name = String.format(Locale.ROOT, "%020d-%08d-%s", System.currentTimeMillis(), ++sequence, UUID.randomUUID());
        Path temporary = inbox.resolve(name + ".tmp");
        Files.writeString(temporary, JSON.toJson(request), StandardOpenOption.CREATE_NEW);
        try { Files.move(temporary, inbox.resolve(name + ".json"), StandardCopyOption.ATOMIC_MOVE); }
        catch (AtomicMoveNotSupportedException ignored) { Files.move(temporary, inbox.resolve(name + ".json")); }
        finally { Files.deleteIfExists(temporary); }
    }
    State readState() throws IOException {
        Path path = directory.resolve("state.json");
        if (!Files.isRegularFile(path)) return null;
        State state = JSON.fromJson(Files.readString(path), State.class);
        if (state == null || state.protocol != 1 || !requestId.equals(state.requestId)
            || Math.abs(System.currentTimeMillis() - state.heartbeat) > 6000) return null;
        return state;
    }
    BufferedImage readFrame() throws IOException {
        Path path = directory.resolve("frame.png");
        if (Files.size(path) > 64L * 1024 * 1024) throw new IOException("Unity preview frame is too large");
        BufferedImage image = ImageIO.read(path.toFile());
        if (image == null) throw new IOException("Unity returned an invalid preview frame");
        return image;
    }
    void screen(int width, int height) { screenWidth = width; screenHeight = height; }
    void viewport(int width, int height) { viewportWidth = width; viewportHeight = height; }
    void focusUnity() throws IOException {
        ensureUnityStarted();
        if (System.getProperty("os.name").toLowerCase(Locale.ROOT).contains("mac")) {
            Path app = locateUnity().getParent().getParent().getParent();
            new ProcessBuilder("/usr/bin/open", "-a", app.toString()).start();
        }
    }
    void ensureUnityStarted() throws IOException {
        if (Files.exists(project.resolve("Temp/UnityLockfile"))) return;
        Path executable = locateUnity();
        Path log = project.resolve("Library/ComposeNetPreview/unity-launch.log");
        Files.createDirectories(log.getParent());
        new ProcessBuilder(executable.toString(), "-projectPath", project.toString())
            .redirectErrorStream(true).redirectOutput(ProcessBuilder.Redirect.appendTo(log.toFile())).start();
    }
    private Path locateUnity() throws IOException {
        var matcher = Pattern.compile("(?m)^m_EditorVersion:\\s*(\\S+)")
            .matcher(Files.readString(project.resolve("ProjectSettings/ProjectVersion.txt")));
        if (!matcher.find()) throw new IOException("The Unity project has no editor version");
        String version = matcher.group(1);
        String home = System.getProperty("user.home");
        String os = System.getProperty("os.name").toLowerCase(Locale.ROOT);
        List<Path> candidates = os.contains("mac") ? List.of(
            Path.of("/Applications/Unity/Hub/Editor", version, "Unity.app/Contents/MacOS/Unity"),
            Path.of(home, "Applications/Unity/Hub/Editor", version, "Unity.app/Contents/MacOS/Unity"))
            : os.contains("win") ? List.of(Path.of(System.getenv().getOrDefault("ProgramFiles", "C:\\Program Files"),
                "Unity/Hub/Editor", version, "Editor/Unity.exe"))
            : List.of(Path.of(home, "Unity/Hub/Editor", version, "Editor/Unity"), Path.of("/opt/unity/Editor", version, "Editor/Unity"));
        return candidates.stream().filter(Files::isRegularFile).findFirst()
            .orElseThrow(() -> new IOException("Install Unity " + version + " through Unity Hub, or open this project in Unity first."));
    }
}
