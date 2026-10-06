using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace V57.GameForge.Editor
{
    /// <summary>
    /// Adds "Add to Forge Chat" to the Unity Console context menu (right-click).
    /// Uses reflection — ConsoleWindow, LogEntry and ListViewState are internal to UnityEditor.
    /// </summary>
    static class GameForgeConsoleIntegration
    {
        const string MenuTitle = "Add to Forge Chat";

        static readonly List<ConsoleLogLine> s_Selected = new();

        static Type s_ConsoleWindowType;
        static Type s_LogEntryType;
        static Type s_LogEntriesType;
        static Type s_ListViewStateType;

        static FieldInfo s_ListViewField;
        static FieldInfo s_ListViewSelectedItemsField;
        static FieldInfo s_LogEntryMessageField;
        static FieldInfo s_LogEntryFileField;
        static FieldInfo s_LogEntryLineField;
        static FieldInfo s_LogEntryModeField;
        static PropertyInfo s_LogEntryMessageProperty;

        static MethodInfo s_LogEntriesGetLinesAndMode;
        static MethodInfo s_LogEntriesGetCount;
        static MethodInfo s_LogEntriesGetEntryInternal;
        static MethodInfo s_LogEntriesStartGetting;
        static MethodInfo s_LogEntriesEndGetting;

        static int s_ErrorModeFlags;
        static int s_WarningModeFlags;

        static object s_Entry;
        static EditorWindow s_ConsoleWindow;
        static object s_ListView;

        static bool s_ReflectionReady;
        static bool s_HooksReady;

        [InitializeOnLoadMethod]
        static void Register()
        {
            if (s_HooksReady) return;
            // Defer until after AI Assistant registers its console hooks (same frame, later call).
            EditorApplication.delayCall += TryRegisterHooks;
        }

        static void TryRegisterHooks()
        {
            if (s_HooksReady) return;
            if (!EnsureReflection())
            {
                Debug.LogWarning("[GameForge] Console integration unavailable: Unity console types not found.");
                return;
            }

            // Drop legacy toolbar button (ConsoleWindow static hooks can survive domain reload).
            TryUnhookToolbar(s_ConsoleWindowType);

            var contextOk = TryHookContextMenu(s_ConsoleWindowType);
            if (!contextOk)
            {
                Debug.LogWarning("[GameForge] Console integration unavailable: could not hook ConsoleWindow context menu.");
                return;
            }

            s_HooksReady = true;
        }

        static void TryUnhookToolbar(Type consoleType)
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            try
            {
                var toolbarField = consoleType.GetField("drawCustomToolbarGui", flags);
                if (toolbarField == null) return;

                var existing = toolbarField.GetValue(null) as Delegate;
                if (existing == null) return;

                Delegate kept = null;
                foreach (var d in existing.GetInvocationList())
                {
                    var decl = d.Method?.DeclaringType;
                    if (decl == typeof(GameForgeConsoleIntegration))
                        continue;
                    kept = kept == null ? d : Delegate.Combine(kept, d);
                }

                toolbarField.SetValue(null, kept);
            }
            catch
            {
                /* best-effort — toolbar may already be clean */
            }
        }

        static bool EnsureReflection()
        {
            if (s_ReflectionReady) return s_ConsoleWindowType != null;

            var asm = typeof(EditorWindow).Assembly;
            s_ConsoleWindowType = asm.GetType("UnityEditor.ConsoleWindow");
            s_LogEntryType = asm.GetType("UnityEditor.LogEntry");
            s_LogEntriesType = asm.GetType("UnityEditor.LogEntries");
            s_ListViewStateType = asm.GetType("UnityEditor.ListViewState");

            if (s_ConsoleWindowType == null || s_LogEntryType == null || s_LogEntriesType == null)
                return false;

            s_ListViewField = s_ConsoleWindowType.GetField("m_ListView",
                BindingFlags.Instance | BindingFlags.NonPublic);
            s_ListViewSelectedItemsField = s_ListViewStateType?.GetField("selectedItems",
                BindingFlags.Instance | BindingFlags.Public);

            s_LogEntryMessageField = s_LogEntryType.GetField("message", BindingFlags.Instance | BindingFlags.Public);
            s_LogEntryMessageProperty = s_LogEntryType.GetProperty("message",
                BindingFlags.Instance | BindingFlags.Public);
            s_LogEntryFileField = s_LogEntryType.GetField("file", BindingFlags.Instance | BindingFlags.Public);
            s_LogEntryLineField = s_LogEntryType.GetField("line", BindingFlags.Instance | BindingFlags.Public);
            s_LogEntryModeField = s_LogEntryType.GetField("mode", BindingFlags.Instance | BindingFlags.Public);
            s_Entry = Activator.CreateInstance(s_LogEntryType);

            const BindingFlags staticFlags = BindingFlags.Public | BindingFlags.Static;
            s_LogEntriesGetCount = s_LogEntriesType.GetMethod("GetCount", staticFlags);
            s_LogEntriesGetEntryInternal = s_LogEntriesType.GetMethod("GetEntryInternal", staticFlags);
            s_LogEntriesStartGetting = s_LogEntriesType.GetMethod("StartGettingEntries", staticFlags);
            s_LogEntriesEndGetting = s_LogEntriesType.GetMethod("EndGettingEntries", staticFlags);
            s_LogEntriesGetLinesAndMode = s_LogEntriesType.GetMethod("GetLinesAndModeFromEntryInternal",
                staticFlags);

            var modeType = s_ConsoleWindowType.GetNestedType("Mode",
                BindingFlags.Public | BindingFlags.NonPublic);
            if (modeType != null)
            {
                s_ErrorModeFlags =
                    ModeFlag(modeType, "Error") | ModeFlag(modeType, "Assert") | ModeFlag(modeType, "Fatal") |
                    ModeFlag(modeType, "AssetImportError") | ModeFlag(modeType, "ScriptingError") |
                    ModeFlag(modeType, "ScriptCompileError") | ModeFlag(modeType, "ScriptingException") |
                    ModeFlag(modeType, "GraphCompileError") | ModeFlag(modeType, "ScriptingAssertion") |
                    ModeFlag(modeType, "StickyError") | ModeFlag(modeType, "ReportBug") |
                    ModeFlag(modeType, "DisplayPreviousErrorInStatusBar") | ModeFlag(modeType, "VisualScriptingError");

                s_WarningModeFlags =
                    ModeFlag(modeType, "AssetImportWarning") | ModeFlag(modeType, "ScriptingWarning") |
                    ModeFlag(modeType, "ScriptCompileWarning");
            }

            s_ReflectionReady = true;
            return s_LogEntriesGetCount != null && s_LogEntriesGetEntryInternal != null &&
                   s_ListViewField != null && s_ListViewSelectedItemsField != null;
        }

        static int ModeFlag(Type modeType, string name)
        {
            var field = modeType.GetField(name, BindingFlags.Public | BindingFlags.Static);
            return field != null ? (int)field.GetValue(null) : 0;
        }

        static bool TryHookContextMenu(Type consoleType)
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            try
            {
                var contextField = consoleType.GetField("entryContextClicked", flags);
                if (contextField == null) return false;

                var existing = contextField.GetValue(null) as Delegate;
                var fieldType = contextField.FieldType;
                Delegate forgeHandler = null;

                if (fieldType == typeof(Action))
                    forgeHandler = (Action)ShowContextMenu;
                else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Action<>))
                    forgeHandler = CreateGenericContextHandler(fieldType.GetGenericArguments()[0]);

                if (forgeHandler == null) return false;

                contextField.SetValue(null, Delegate.Combine(existing, forgeHandler));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[GameForge] Console context hook failed: " + ex.Message);
                return false;
            }
        }

        static Delegate CreateGenericContextHandler(Type entryType)
        {
            var helper = typeof(ContextMenuHelper<>).MakeGenericType(entryType);
            var method = helper.GetMethod("Handle", BindingFlags.Static | BindingFlags.Public);
            var handlerType = typeof(Action<>).MakeGenericType(entryType);
            return Delegate.CreateDelegate(handlerType, method);
        }

        internal static void ShowContextMenu()
        {
            // Runs after other entryContextClicked handlers — one menu with Forge + Assistant.
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent(MenuTitle), false, SendSelectedLogsToChat);
            TryAddAssistantMenuItem(menu);
            menu.ShowAsContext();
        }

        static void TryAddAssistantMenuItem(GenericMenu menu)
        {
            try
            {
                var consoleUi = Type.GetType(
                    "Unity.AI.Assistant.UI.Editor.Scripts.ConsoleUI, Unity.AI.Assistant.UI.Editor");
                var method = consoleUi?.GetMethod("AddSelectedLogsToAssistantContext",
                    BindingFlags.Static | BindingFlags.NonPublic);
                if (method == null) return;

                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Add to Assistant"), false,
                    () => method.Invoke(null, null));
            }
            catch
            {
                /* Assistant optional */
            }
        }

        static void SendSelectedLogsToChat()
        {
            if (!TryCollectSelectedLogs(s_Selected) || s_Selected.Count == 0)
            {
                Debug.LogWarning("[GameForge] No console logs selected.");
                return;
            }

            GameForgeChatWindow.Open();
            GameForgeChatWindow.PrefillComposeExternal(FormatForChat(s_Selected));
        }

        static string FormatForChat(IReadOnlyList<ConsoleLogLine> logs)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Please help me with this Unity console output:");
            sb.AppendLine();

            for (var i = 0; i < logs.Count; i++)
            {
                var log = logs[i];
                if (i > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("---");
                    sb.AppendLine();
                }

                sb.Append('[').Append(log.Severity).Append("] ");
                var body = string.IsNullOrWhiteSpace(log.Message) ? "(no message text)" : log.Message.TrimEnd();
                sb.AppendLine(body);
                if (!string.IsNullOrEmpty(log.File))
                    sb.AppendLine($"at {log.File}:{log.Line}");
            }

            return sb.ToString().TrimEnd();
        }

        static bool TryCollectSelectedLogs(List<ConsoleLogLine> results)
        {
            results.Clear();
            if (!EnsureReflection() || !TryGetSelectedRows(out var selectedRows))
                return false;

            StartGettingEntries();
            try
            {
                for (var i = 0; i < selectedRows.Length; i++)
                {
                    if (!selectedRows[i]) continue;
                    if (!TryGetEntryInternal(i, out var line)) continue;
                    results.Add(line);
                }
            }
            finally
            {
                EndGettingEntries();
            }

            return results.Count > 0;
        }

        static bool TryGetSelectedRows(out bool[] selectedRows)
        {
            selectedRows = null;
            var current = GetOpenConsoleWindow();
            if (current == null) return false;

            if (!ReferenceEquals(current, s_ConsoleWindow))
            {
                s_ConsoleWindow = current;
                s_ListView = s_ListViewField?.GetValue(s_ConsoleWindow);
            }

            if (s_ListView == null) return false;
            selectedRows = s_ListViewSelectedItemsField?.GetValue(s_ListView) as bool[];
            return selectedRows != null;
        }

        static EditorWindow GetOpenConsoleWindow()
        {
            if (s_ConsoleWindowType == null) return null;
            var windows = Resources.FindObjectsOfTypeAll(s_ConsoleWindowType);
            return windows != null && windows.Length > 0 ? windows[0] as EditorWindow : null;
        }

        static void StartGettingEntries() => s_LogEntriesStartGetting?.Invoke(null, null);

        static void EndGettingEntries() => s_LogEntriesEndGetting?.Invoke(null, null);

        static bool TryGetEntryInternal(int index, out ConsoleLogLine line)
        {
            line = default;
            if (s_LogEntriesGetEntryInternal?.Invoke(null, new[] { index, s_Entry }) is not true)
                return false;

            var mode = s_LogEntryModeField?.GetValue(s_Entry) is int m ? m : 0;
            var message = ReadLogEntryMessage(index, s_Entry);
            line = new ConsoleLogLine
            {
                Severity = ClassifySeverity(mode),
                Message = message,
                File = s_LogEntryFileField?.GetValue(s_Entry) as string ?? "",
                Line = s_LogEntryLineField?.GetValue(s_Entry) is int l ? l : 0
            };
            return true;
        }

        static string ReadLogEntryMessage(int index, object entry)
        {
            var message = s_LogEntryMessageField?.GetValue(entry) as string;
            if (string.IsNullOrEmpty(message))
                message = s_LogEntryMessageProperty?.GetValue(entry) as string;

            if (!string.IsNullOrWhiteSpace(message))
                return message;

            if (s_LogEntriesGetLinesAndMode != null)
            {
                var modeBox = 0;
                var text = string.Empty;
                var args = new object[] { index, 10, modeBox, text };
                if (s_LogEntriesGetLinesAndMode.Invoke(null, args) is true)
                {
                    var fromLines = args[3] as string;
                    if (!string.IsNullOrWhiteSpace(fromLines))
                        return fromLines;
                }
            }

            return message ?? "";
        }

        static string ClassifySeverity(int mode)
        {
            if ((mode & s_ErrorModeFlags) != 0) return "Error";
            if ((mode & s_WarningModeFlags) != 0) return "Warning";
            return "Log";
        }

        static class ContextMenuHelper<T>
        {
            public static void Handle(T _) => ShowContextMenu();
        }

        struct ConsoleLogLine
        {
            public string Severity;
            public string Message;
            public string File;
            public int Line;
        }
    }
}
