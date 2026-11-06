package net.compose.preview;

import com.intellij.codeInsight.daemon.LineMarkerInfo;
import com.intellij.codeInsight.daemon.LineMarkerProvider;
import com.intellij.icons.AllIcons;
import com.intellij.openapi.editor.markup.GutterIconRenderer;
import com.intellij.openapi.project.DumbAware;
import com.intellij.psi.PsiElement;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;
import com.intellij.openapi.util.Key;
import java.util.List;

public final class PreviewLineMarkerProvider implements LineMarkerProvider, DumbAware {
    private static final Key<Cache> CACHE = Key.create("compose.net.preview.method-markers");
    private record Cache(long stamp, List<PreviewMethodLocator.Marker> markers) { }
    @Override public @Nullable LineMarkerInfo<?> getLineMarkerInfo(@NotNull PsiElement element) {
        if (element.getFirstChild() != null) return null;
        var file = element.getContainingFile();
        if (file == null || file.getVirtualFile() == null || !"cs".equalsIgnoreCase(file.getVirtualFile().getExtension())) return null;
        var document = file.getViewProvider().getDocument();
        if (document == null) return null;
        Cache cache = file.getUserData(CACHE);
        if (cache == null || cache.stamp != document.getModificationStamp()) {
            cache = new Cache(document.getModificationStamp(), PreviewMethodLocator.find(document.getText()));
            file.putUserData(CACHE, cache);
        }
        var marker = cache.markers.stream().filter(item -> item.start() == element.getTextOffset()).findFirst().orElse(null);
        if (marker == null) return null;
        return new LineMarkerInfo<>(element, element.getTextRange(), AllIcons.Actions.Execute,
            ignored -> "Open Compose.Net preview", (event, target) ->
                PreviewOpening.open(target.getProject(), target.getContainingFile().getVirtualFile(), marker.line()),
            GutterIconRenderer.Alignment.LEFT, () -> "Open Compose.Net preview");
    }
}
