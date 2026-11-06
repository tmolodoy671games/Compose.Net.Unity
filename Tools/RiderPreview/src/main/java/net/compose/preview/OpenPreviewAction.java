package net.compose.preview;

import com.intellij.openapi.actionSystem.*;
import com.intellij.openapi.project.DumbAwareAction;
import org.jetbrains.annotations.NotNull;

public final class OpenPreviewAction extends DumbAwareAction {
    @Override public @NotNull ActionUpdateThread getActionUpdateThread() { return ActionUpdateThread.BGT; }
    @Override public void update(@NotNull AnActionEvent event) {
        var file = event.getData(CommonDataKeys.VIRTUAL_FILE);
        event.getPresentation().setEnabledAndVisible(event.getProject() != null && file != null
            && "cs".equalsIgnoreCase(file.getExtension()) && event.getData(CommonDataKeys.EDITOR) != null);
    }
    @Override public void actionPerformed(@NotNull AnActionEvent event) {
        var project = event.getProject();
        var editor = event.getData(CommonDataKeys.EDITOR);
        var file = event.getData(CommonDataKeys.VIRTUAL_FILE);
        if (project != null && editor != null && file != null)
            PreviewOpening.open(project, file, editor.getCaretModel().getLogicalPosition().line + 1);
    }
}
