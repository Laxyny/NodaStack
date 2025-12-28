using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace NodaStack.Services
{
    public class KeyboardShortcutManager
    {
        private readonly Dictionary<string, Action> shortcuts;
        private readonly Window window;

        public KeyboardShortcutManager(Window window)
        {
            this.window = window;
            shortcuts = new Dictionary<string, Action>();

            window.KeyDown += Window_KeyDown;
        }

        public void RegisterShortcut(Key key, ModifierKeys modifiers, Action action)
        {
            var keyString = GetKeyString(key, modifiers);
            shortcuts[keyString] = action;
        }

        public void RegisterShortcut(Key key, Action action)
        {
            RegisterShortcut(key, ModifierKeys.None, action);
        }

        private string GetKeyString(Key key, ModifierKeys modifiers)
        {
            var keyString = key.ToString();
            if (modifiers.HasFlag(ModifierKeys.Control))
                keyString = "Ctrl+" + keyString;
            if (modifiers.HasFlag(ModifierKeys.Alt))
                keyString = "Alt+" + keyString;
            if (modifiers.HasFlag(ModifierKeys.Shift))
                keyString = "Shift+" + keyString;
            if (modifiers.HasFlag(ModifierKeys.Windows))
                keyString = "Win+" + keyString;

            return keyString;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            var keyString = GetKeyString(e.Key, Keyboard.Modifiers);

            if (shortcuts.TryGetValue(keyString, out var action))
            {
                e.Handled = true;
                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error executing shortcut {keyString}: {ex.Message}");
                }
            }
        }

        public void UnregisterShortcut(Key key, ModifierKeys modifiers)
        {
            var keyString = GetKeyString(key, modifiers);
            shortcuts.Remove(keyString);
        }

        public void UnregisterShortcut(Key key)
        {
            UnregisterShortcut(key, ModifierKeys.None);
        }

        public void ClearShortcuts()
        {
            shortcuts.Clear();
        }
    }
}