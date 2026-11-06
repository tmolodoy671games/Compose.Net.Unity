package net.compose.preview;

import java.util.*;
import java.util.regex.Pattern;

final class PreviewMethodLocator {
    record Marker(int start, int end, int line) { }
    private static final Pattern METHOD = Pattern.compile(
        "(?m)(?:^|(?<=\\]))[\\t ]*(?:(?:public|private|protected|internal|static|partial|unsafe|extern|new)\\s+)*void\\s+(@?[_\\p{L}][_\\p{L}\\p{N}]*)\\s*\\(");
    private static final Pattern ATTRIBUTE = Pattern.compile("\\bUnityPreview(?:Attribute)?\\b");
    static List<Marker> find(String source) {
        String code = mask(source);
        List<Marker> result = new ArrayList<>();
        var methods = METHOD.matcher(code);
        while (methods.find()) {
            int cursor = skipWhitespace(code, methods.start() - 1);
            boolean preview = false;
            while (cursor >= 0 && code.charAt(cursor) == ']') {
                int end = cursor + 1, depth = 1;
                while (cursor > 0 && depth > 0) {
                    cursor--;
                    if (code.charAt(cursor) == ']') depth++;
                    else if (code.charAt(cursor) == '[') depth--;
                }
                if (depth != 0) break;
                preview |= ATTRIBUTE.matcher(code.substring(cursor, end)).find();
                cursor = skipWhitespace(code, cursor - 1);
            }
            if (preview) {
                int start = methods.start(1);
                int line = 1;
                for (int i = 0; i < start; i++) if (source.charAt(i) == '\n') line++;
                result.add(new Marker(start, methods.end(1), line));
            }
        }
        return result;
    }
    private static int skipWhitespace(String code, int offset) {
        while (offset >= 0 && Character.isWhitespace(code.charAt(offset))) offset--;
        return offset;
    }
    // Keep offsets/newlines intact while excluding comments and C# string/char contents.
    private static String mask(String source) {
        char[] chars = source.toCharArray();
        for (int i = 0; i < chars.length;) {
            int start = i, end = i;
            if (source.startsWith("//", i)) {
                end = source.indexOf('\n', i + 2); if (end < 0) end = chars.length;
            } else if (source.startsWith("/*", i)) {
                end = source.indexOf("*/", i + 2); end = end < 0 ? chars.length : end + 2;
            } else if (chars[i] == '"' || chars[i] == '\'') {
                char quote = chars[i];
                int count = 1;
                if (quote == '"') while (i + count < chars.length && chars[i + count] == '"') count++;
                if (count >= 3) {
                    String delimiter = "\"".repeat(count);
                    end = source.indexOf(delimiter, i + count); end = end < 0 ? chars.length : end + count;
                } else {
                    boolean verbatim = i > 0 && chars[i - 1] == '@';
                    end = i + 1;
                    while (end < chars.length) {
                        if (!verbatim && chars[end] == '\\') { end = Math.min(chars.length, end + 2); continue; }
                        if (chars[end++] == quote) {
                            if (verbatim && end < chars.length && chars[end] == quote) { end++; continue; }
                            break;
                        }
                    }
                }
            }
            if (end == i) { i++; continue; }
            for (int j = start; j < end; j++) if (chars[j] != '\n' && chars[j] != '\r') chars[j] = ' ';
            i = end;
        }
        return new String(chars);
    }
}
