package net.compose.preview;

import com.intellij.openapi.Disposable;
import com.intellij.openapi.project.Project;
import javax.swing.*;
import java.awt.*;
import java.awt.event.HierarchyEvent;
import java.awt.event.ComponentAdapter;
import java.awt.event.ComponentEvent;
import java.util.concurrent.atomic.AtomicBoolean;
import java.util.concurrent.atomic.AtomicReference;
import java.awt.image.BufferedImage;
import java.io.IOException;
import java.nio.file.Path;
import java.util.concurrent.*;

final class PreviewPanel extends JPanel implements Disposable {
    private final ScheduledExecutorService worker = Executors.newSingleThreadScheduledExecutor(runnable -> {
        Thread thread = new Thread(runnable, "Compose.Net Unity Preview"); thread.setDaemon(true); return thread;
    });
    private record Envelope(PreviewConnection connection, PreviewConnection.Request request) { }
    private final ConcurrentLinkedQueue<Envelope> input = new ConcurrentLinkedQueue<>();
    private final JButton play = new JButton(new PlayIcon());
    private final JSlider zoom = new JSlider(25, 400, 100);
    private final JLabel zoomLabel = new JLabel("Fit");
    private final JLabel message = new JLabel("Open a [UnityPreview] from the editor gutter.");
    private final PreviewSurface surface = new PreviewSurface();
    private final JScrollPane scroll = new JScrollPane(surface);
    private record Presentation(PreviewConnection connection, PreviewConnection.State state, BufferedImage image) { }
    private final AtomicReference<Presentation> latest = new AtomicReference<>();
    private final AtomicBoolean uiScheduled = new AtomicBoolean();
    private PreviewConnection workerConnection;
    private PreviewConnection.State workerState;
    private long lastState;
    private volatile PreviewConnection connection;
    private volatile boolean disposed, visible;
    private PreviewConnection.State state;
    private long lastPing, lastOpen, loadedRevision = -1;
    private BufferedImage frame;
    private String failure;

    PreviewPanel(Project project) {
        super(new BorderLayout(0, 4));
        JPanel toolbar = new JPanel(new BorderLayout());
        play.setToolTipText("Play in Unity"); play.setEnabled(false);
        play.setPreferredSize(new Dimension(30, 26));
        toolbar.add(play, BorderLayout.WEST);
        JPanel scaling = new JPanel(new FlowLayout(FlowLayout.RIGHT, 4, 0));
        JButton fit = new JButton("Fit");
        zoom.setPreferredSize(new Dimension(120, 26));
        zoom.setToolTipText("Zoom relative to Fit (25–400%)");
        zoom.addChangeListener(event -> {
            surface.setZoom(zoom.getValue() / 100.0);
            zoomLabel.setText(zoom.getValue() == 100 ? "Fit" : zoom.getValue() + "% Fit");
        });
        fit.addActionListener(event -> zoom.setValue(100));
        scaling.add(fit); scaling.add(zoom); scaling.add(zoomLabel);
        toolbar.add(scaling, BorderLayout.EAST);
        JPanel header = new JPanel(new BorderLayout());
        header.add(toolbar, BorderLayout.NORTH); header.add(message, BorderLayout.SOUTH);
        add(header, BorderLayout.NORTH); add(scroll, BorderLayout.CENTER);
        scroll.getViewport().addComponentListener(new ComponentAdapter() {
            @Override public void componentResized(ComponentEvent event) {
                surface.revalidate(); surface.repaint();
                requestResize();
            }
        });
        addHierarchyBoundsListener(new java.awt.event.HierarchyBoundsAdapter() {
            @Override public void ancestorMoved(HierarchyEvent event) { requestResize(); }
        });
        play.addActionListener(event -> {
            if (state == null || connection == null) return;
            var request = PreviewConnection.Request.of("playInUnity");
            request.previewId = state.previewId;
            var current = connection;
            play.setEnabled(false);
            worker.execute(() -> {
                try { current.send(request); current.focusUnity(); }
                catch (Exception exception) { showFailure(current, exception.getMessage()); }
                finally { SwingUtilities.invokeLater(() -> { if (!disposed && connection == current) play.setEnabled(true); }); }
            });
        });
        addHierarchyListener(event -> {
            if ((event.getChangeFlags() & HierarchyEvent.SHOWING_CHANGED) == 0) return;
            boolean showing = isShowing();
            if (visible && !showing) send(PreviewConnection.Request.of("release"));
            visible = showing;
        });
        worker.scheduleWithFixedDelay(this::poll, 0, 100, TimeUnit.MILLISECONDS);
    }

    void open(Path project, String sourceFile, int line) {
        PreviewConnection previous = connection;
        var next = new PreviewConnection(project, sourceFile, line);
        Dimension size = scroll.getViewport().getExtentSize();
        next.viewport(Math.max(1, size.width), Math.max(1, size.height));
        Dimension limit = screenLimit(); next.screen(limit.width, limit.height);
        connection = next;
        state = null; failure = null;
        zoom.setValue(100);
        lastPing = lastOpen = 0;
        input.clear(); surface.clearFrame(); play.setEnabled(false);
        message.setText("Starting Unity preview…"); message.setVisible(true);
        worker.execute(() -> {
            try {
                if (previous != null) { previous.send(PreviewConnection.Request.of("release")); }
                next.send(PreviewConnection.Request.of("open"));
                lastOpen = System.currentTimeMillis();
                next.ensureUnityStarted();
            } catch (Exception exception) { showFailure(next, exception.getMessage()); }
        });
    }

    private Dimension screenLimit() {
        GraphicsConfiguration configuration = getGraphicsConfiguration();
        if (configuration == null && !GraphicsEnvironment.isHeadless())
            configuration = GraphicsEnvironment.getLocalGraphicsEnvironment().getDefaultScreenDevice().getDefaultConfiguration();
        if (configuration == null) return new Dimension(4096, 4096);
        Rectangle bounds = configuration.getBounds();
        Insets insets = Toolkit.getDefaultToolkit().getScreenInsets(configuration);
        return new Dimension(Math.max(1, bounds.width - insets.left - insets.right - 16),
            Math.max(1, bounds.height - insets.top - insets.bottom - 64));
    }
    private void requestResize() {
        PreviewConnection current = connection;
        if (current == null) return;
        Dimension size = scroll.getViewport().getExtentSize();
        if (size.width <= 0 || size.height <= 0) return;
        current.viewport(size.width, size.height);
        Dimension limit = screenLimit(); current.screen(limit.width, limit.height);
        var request = PreviewConnection.Request.of("resize");
        request.width = size.width; request.height = size.height; send(request);
    }

    private void send(PreviewConnection.Request request) {
        if (disposed || connection == null) return;
        PreviewConnection target = connection;
        if ("resize".equals(request.op)) input.removeIf(item -> item.connection == target && "resize".equals(item.request.op));
        if ("move".equals(request.eventType)) input.removeIf(item -> item.connection == target && "move".equals(item.request.eventType));
        input.add(new Envelope(target, request));
    }

    private void poll() {
        PreviewConnection current = connection;
        if (disposed || current == null) return;
        try {
            Envelope envelope;
            while ((envelope = input.poll()) != null) {
                envelope.connection.send(envelope.request);
            }
            if (!visible || failure != null) return;
            long now = System.currentTimeMillis();
            if (now - lastPing > 1000) { current.send(PreviewConnection.Request.of("ping")); lastPing = now; }
            if (workerConnection != current) {
                workerConnection = current; workerState = null; frame = null;
                lastState = 0; loadedRevision = -1;
            }
            boolean controlChanged = false;
            if (now - lastState >= 100 || workerState == null) {
                workerState = current.readState(); lastState = now; controlChanged = true;
            }
            var snapshot = workerState;
            if (snapshot == null) {
                loadedRevision = -1; frame = null;
                if (now - lastOpen > 3000) {
                    current.send(PreviewConnection.Request.of("open")); lastOpen = now;
                }
                if (controlChanged) post(new Presentation(current, null, null));
                return;
            }
            boolean changed = false;
            if (snapshot.revision <= 0) { frame = null; loadedRevision = -1; }
            if (snapshot.revision > 0 && (loadedRevision != snapshot.revision || frame == null
                || frame.getWidth() != snapshot.width || frame.getHeight() != snapshot.height)) {
                frame = current.readFrame(); loadedRevision = snapshot.revision; changed = true;
            }
            if (changed || controlChanged) post(new Presentation(current, snapshot, frame));
        } catch (IOException | RuntimeException exception) {
            // Unity can delete its mailbox during compilation; the next open restores it.
            loadedRevision = -1;
        }
    }

    private void post(Presentation presentation) {
        latest.set(presentation);
        if (uiScheduled.compareAndSet(false, true)) SwingUtilities.invokeLater(this::presentLatest);
    }
    private void presentLatest() {
        try {
            Presentation presentation = latest.getAndSet(null);
            if (presentation == null || disposed || connection != presentation.connection) return;
            var snapshot = presentation.state;
            if (snapshot == null) {
                state = null; play.setEnabled(false); surface.clearFrame();
                message.setText("Waiting for Unity to compile the preview…"); message.setVisible(true);
                return;
            }
            state = snapshot;
            play.setIcon(new PlayIcon());
            play.setToolTipText("Play in Unity");
            boolean error = snapshot.error != null && !snapshot.error.isBlank();
            play.setEnabled(!error);
            message.setVisible(error || presentation.image == null);
            message.setText(error ? snapshot.error : "Rendering preview…");
            message.setToolTipText(error ? snapshot.error : null);
            if (presentation.image != null && !error) surface.showFrame(presentation.image, snapshot);
            else surface.clearFrame();
        } finally {
            uiScheduled.set(false);
            if (latest.get() != null && uiScheduled.compareAndSet(false, true)) SwingUtilities.invokeLater(this::presentLatest);
        }
    }

    private void showFailure(PreviewConnection current, String text) {
        failure = text;
        SwingUtilities.invokeLater(() -> {
            if (disposed || connection != current) return;
            message.setText(text); message.setToolTipText(text); message.setVisible(true);
            play.setEnabled(false); surface.clearFrame();
        });
    }
    @Override public void dispose() {
        if (disposed) return;
        disposed = true;
        PreviewConnection current = connection;
        if (current != null) worker.execute(() -> {
            try { current.send(PreviewConnection.Request.of("release")); } catch (IOException ignored) { }
        });
        worker.shutdown(); surface.clearFrame();
    }

    private static final class PlayIcon implements Icon {
        @Override public int getIconWidth() { return 16; }
        @Override public int getIconHeight() { return 16; }
        @Override public void paintIcon(Component component, Graphics graphics, int x, int y) {
            var g = (Graphics2D)graphics.create();
            try {
                g.setRenderingHint(RenderingHints.KEY_ANTIALIASING, RenderingHints.VALUE_ANTIALIAS_ON);
                g.setColor(component.isEnabled() ? component.getForeground() : UIManager.getColor("Label.disabledForeground"));
                g.fillPolygon(new int[] { x + 5, x + 13, x + 5 }, new int[] { y + 2, y + 8, y + 14 }, 3);
            } finally { g.dispose(); }
        }
    }
}
