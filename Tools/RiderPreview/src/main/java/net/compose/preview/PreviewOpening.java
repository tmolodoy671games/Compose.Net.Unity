package net.compose.preview;

import com.intellij.ide.util.PropertiesComponent;
import com.intellij.openapi.fileChooser.FileChooser;
import com.intellij.openapi.fileChooser.FileChooserDescriptorFactory;
import com.intellij.openapi.fileEditor.FileDocumentManager;
import com.intellij.openapi.project.Project;
import com.intellij.openapi.vfs.VirtualFile;
import com.intellij.openapi.wm.ToolWindowManager;
import java.nio.file.*;

final class PreviewOpening {
    private static final String PROJECT_KEY = "compose.net.preview.unityProject";
    static void open(Project project, VirtualFile file, int line) {
        var document = FileDocumentManager.getInstance().getDocument(file);
        if (document != null) FileDocumentManager.getInstance().saveDocument(document);
        Path root = ancestorProject(Path.of(file.getPath()).getParent());
        if (root == null && project.getBasePath() != null) root = ancestorProject(Path.of(project.getBasePath()));
        if (root == null) {
            String saved = PropertiesComponent.getInstance(project).getValue(PROJECT_KEY);
            if (saved != null && isProject(Path.of(saved))) root = Path.of(saved);
        }
        if (root != null) { show(project, root, file, line); return; }
        var descriptor = FileChooserDescriptorFactory.createSingleFolderDescriptor()
            .withTitle("Unity project for Compose.Net previews");
        FileChooser.chooseFile(descriptor, project, null, folder -> {
            Path selected = Path.of(folder.getPath());
            if (!isProject(selected)) {
                com.intellij.openapi.ui.Messages.showErrorDialog(project, "Choose the Unity project folder containing Assets and ProjectSettings.", "Compose.Net Preview");
                return;
            }
            PropertiesComponent.getInstance(project).setValue(PROJECT_KEY, selected.toString());
            show(project, selected, file, line);
        });
    }
    private static void show(Project project, Path root, VirtualFile file, int line) {
        var window = ToolWindowManager.getInstance(project).getToolWindow("Compose.Net Preview");
        if (window == null) return;
        window.activate(() -> {
            var content = window.getContentManager().getContent(0);
            if (content != null && content.getComponent() instanceof PreviewPanel panel)
                panel.open(root, file.getPath(), line);
        });
    }
    static boolean isProject(Path path) { return Files.isDirectory(path.resolve("Assets")) && Files.isRegularFile(path.resolve("ProjectSettings/ProjectVersion.txt")); }
    private static Path ancestorProject(Path path) {
        while (path != null) { if (isProject(path)) return path; path = path.getParent(); }
        return null;
    }
}
