package net.compose.preview;

import javax.swing.*;
import java.awt.*;
import java.awt.image.BufferedImage;

// Static preview only. Interactive execution belongs to the native Unity window.
final class PreviewSurface extends JPanel implements Scrollable {
    private BufferedImage image;
    private PreviewConnection.State state;
    private double zoom = 1;
    void showFrame(BufferedImage image, PreviewConnection.State state) {
        boolean changed = this.image != image || this.state == null
            || this.state.width != state.width || this.state.height != state.height;
        this.image = image; this.state = state;
        revalidate();
        if (changed) repaint();
    }
    void clearFrame() { image = null; state = null; revalidate(); repaint(); }
    void setZoom(double zoom) { this.zoom = zoom; revalidate(); repaint(); }
    private Dimension viewportSize() {
        return getParent() instanceof JViewport viewport ? viewport.getExtentSize() : getSize();
    }
    double displayScale() {
        if (state == null || state.width <= 0 || state.height <= 0) return 1;
        Dimension available = viewportSize();
        return Math.min(Math.max(1, available.width) / (double)state.width,
            Math.max(1, available.height) / (double)state.height) * zoom;
    }
    @Override public Dimension getPreferredSize() {
        if (state == null) return new Dimension();
        double scale = displayScale();
        return new Dimension(Math.max(1, (int)Math.round(state.width * scale)),
            Math.max(1, (int)Math.round(state.height * scale)));
    }
    Rectangle imageBounds() {
        if (image == null || state == null) return new Rectangle();
        Dimension size = getPreferredSize();
        return new Rectangle(Math.max(0, (getWidth() - size.width) / 2),
            Math.max(0, (getHeight() - size.height) / 2), size.width, size.height);
    }
    @Override protected void paintComponent(Graphics graphics) {
        super.paintComponent(graphics);
        if (image == null) return;
        var bounds = imageBounds(); var g = (Graphics2D)graphics.create();
        try {
            g.setColor(Color.WHITE); g.fill(bounds);
            g.setRenderingHint(RenderingHints.KEY_INTERPOLATION, RenderingHints.VALUE_INTERPOLATION_BILINEAR);
            g.drawImage(image, bounds.x, bounds.y, bounds.width, bounds.height, null);
        } finally { g.dispose(); }
    }
    @Override public Dimension getPreferredScrollableViewportSize() { return getPreferredSize(); }
    @Override public int getScrollableUnitIncrement(Rectangle visible, int orientation, int direction) { return 16; }
    @Override public int getScrollableBlockIncrement(Rectangle visible, int orientation, int direction) {
        return orientation == SwingConstants.HORIZONTAL ? visible.width : visible.height;
    }
    @Override public boolean getScrollableTracksViewportWidth() { return getPreferredSize().width <= viewportSize().width; }
    @Override public boolean getScrollableTracksViewportHeight() { return getPreferredSize().height <= viewportSize().height; }
}
