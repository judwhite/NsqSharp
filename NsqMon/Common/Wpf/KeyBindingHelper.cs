using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NsqMon.Common.Wpf
{
    /// <summary>
    /// KeyBindingHelper
    /// </summary>
    public static class KeyBindingHelper
    {
        /// <summary>Sets the key bindings.</summary>
        /// <param name="target">The target <see cref="UIElement" />.</param>
        /// <param name="menuitems">The menu items.</param>
        public static void SetKeyBindings(UIElement target, ItemCollection menuitems)
        {
            if (menuitems == null)
                throw new ArgumentNullException("menuitems");

            foreach (var item in menuitems)
            {
                MenuItem menuItem = item as MenuItem;
                if (menuItem != null)
                {
                    string gestureText = menuItem.InputGestureText;
                    if (!string.IsNullOrWhiteSpace(gestureText) && menuItem.Command != null)
                    {
                        ModifierKeys modifiers = ModifierKeys.None;
                        string[] keyTexts = gestureText.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < keyTexts.Length; i++)
                        {
                            string keyText = keyTexts[i];

                            if (i == keyTexts.Length - 1)
                            {
                                if (char.IsDigit(keyText[0]))
                                {
                                    keyText = "D" + keyText;
                                }

                                const string arrowText = " Arrow";
                                if (keyText.EndsWith(arrowText))
                                {
                                    keyText = keyText.Substring(0, keyText.Length - arrowText.Length);
                                }

                                Key key;
                                if (Enum.TryParse(keyText, true, out key))
                                {
                                    //KeyGestureConverter x = new KeyGestureConverter(); // TODO: Might be able to use this instead
                                    target.InputBindings.Add(new KeyBinding(menuItem.Command, key, modifiers));
                                    //Debug.WriteLine(String.Format("{0} = {1}+{2}", menuItem.Header, modifiers, key)); // TODO: Take out
                                }
                                else
                                {
                                    throw new InvalidDataException(string.Format("'{0}' cannot be parsed.", gestureText));
                                }
                            }
                            else
                            {
                                ModifierKeys modifierKey;
                                if (keyText == "Ctrl")
                                    keyText = "Control";
                                if (Enum.TryParse(keyText, true, out modifierKey))
                                {
                                    modifiers |= modifierKey;
                                }
                                else
                                {
                                    throw new InvalidDataException(string.Format("'{0}' cannot be parsed.", gestureText));
                                }
                            }
                        }
                    }

                    if (menuItem.Items != null)
                        SetKeyBindings(target, menuItem.Items);
                }
            }
        }
    }
}
