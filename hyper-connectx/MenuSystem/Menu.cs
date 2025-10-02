using System;
using System.Collections.Generic;
using System.Linq;

namespace MenuSystem
{

    public class Menu
    {
        private readonly string _title;
        private readonly EMenuLevel _level;
        private readonly Dictionary<string, MenuItem> _menuItems;

        public Menu(string title, EMenuLevel level)
        {
            _title = title ?? throw new ArgumentNullException(nameof(title));
            _level = level;
            _menuItems = new Dictionary<string, MenuItem>();

            switch (_level)
            {
                case EMenuLevel.Root:
                    _menuItems["x"] = new MenuItem("x", "Exit", () => "x");
                    break;
                case EMenuLevel.First:
                    _menuItems["m"] = new MenuItem("m", "Return to Main Menu", () => "m");
                    _menuItems["x"] = new MenuItem("x", "Exit", () => "x");
                    break;
                case EMenuLevel.Deep:
                    _menuItems["b"] = new MenuItem("b", "Back to Previous Menu", () => "b");
                    _menuItems["m"] = new MenuItem("m", "Return to Main Menu", () => "m");
                    _menuItems["x"] = new MenuItem("x", "Exit", () => "x");
                    break;
            }
        }

        public void AddMenuItem(string key, string value, Func<string> methodToRun)
        {
            key = key?.ToLower() ?? throw new ArgumentNullException(nameof(key));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Menu item key cannot be null or empty.", nameof(key));
            if (_menuItems.ContainsKey(key))
                throw new ArgumentException($"Menu item with key '{key}' already exists.");
            _menuItems[key] = new MenuItem(key, value, methodToRun);
        }

        public void AddSubMenu(string key, Menu subMenu)
        {
            key = key?.ToLower() ?? throw new ArgumentNullException(nameof(key));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Submenu key cannot be null or empty.", nameof(key));
            Console.WriteLine($"Trying to add submenu with key '{key}' to menu '{_title}'. Current keys: {string.Join(", ", _menuItems.Keys)}");
            if (_menuItems.ContainsKey(key))
                throw new ArgumentException($"Shortcut '{key}' on juba kasutusel.");
            if (subMenu == null)
                throw new ArgumentNullException(nameof(subMenu));
            _menuItems[key] = new MenuItem(key, subMenu._title, subMenu.Run);
        }

        public void UpdateMenuItemLabel(string key, string newValue)
        {
            key = key?.ToLower() ?? throw new ArgumentNullException(nameof(key));
            if (!_menuItems.ContainsKey(key))
                throw new ArgumentException($"Menu item with key '{key}' does not exist.");
            if (string.IsNullOrWhiteSpace(newValue))
                throw new ArgumentException("Menu item value cannot be null or empty.", nameof(newValue));
            _menuItems[key].Value = newValue;
        }

        public string Run()
        {
            bool useCursorInput = false;
            while (true)
            {
                Console.Clear();
                DisplayMenu();

                if (useCursorInput)
                {
                    string selectedKey = RunCursorBasedInput();
                    return ProcessSelection(selectedKey);
                }
                else
                {
                    Console.Write("Select an option (or 'c' for cursor mode): ");
                    string? input = Console.ReadLine()?.Trim().ToLower();
                    if (input == "c")
                    {
                        useCursorInput = true;
                        continue;
                    }
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        Console.WriteLine("Invalid input. Press any key to continue...");
                        Console.ReadKey();
                        continue;
                    }
                    string result = ProcessSelection(input);
                    if (result != "") return result;
                }
            }
        }

        private string ProcessSelection(string key)
        {
            key = key?.ToLower() ?? "";
            if (_menuItems.ContainsKey(key))
            {
                string result = _menuItems[key].MethodToRun?.Invoke() ?? "";
                if (result == "x" || (result == "m" && _level != EMenuLevel.Root) || (result == "b" && _level != EMenuLevel.Root))
                {
                    return result;
                }
            }
            else
            {
                Console.WriteLine("Invalid option. Press any key to continue...");
                Console.ReadKey();
            }
            return "";
        }

        private string RunCursorBasedInput()
        {
            var items = _menuItems.Values.OrderBy(i => i.Key).ToList();
            int selectedIndex = 0;
            ConsoleKey key;

            do
            {
                Console.Clear();
                DisplayMenu();
                Console.WriteLine($"\nUse arrow keys to navigate, Enter to select, 'c' to switch to text input");

                for (int i = 0; i < items.Count; i++)
                {
                    if (i == selectedIndex)
                        Console.Write(">");
                    else
                        Console.Write(" ");
                    Console.WriteLine($" {items[i]}");
                }

                key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.UpArrow && selectedIndex > 0)
                    selectedIndex--;
                else if (key == ConsoleKey.DownArrow && selectedIndex < items.Count - 1)
                    selectedIndex++;
                else if (key == ConsoleKey.C)
                    return "c";
            } while (key != ConsoleKey.Enter);

            return items[selectedIndex].Key;
        }

        private void DisplayMenu()
        {
            Console.WriteLine($"\n{_title}");
            Console.WriteLine(new string('-', _title.Length + 4));
            foreach (var item in _menuItems.Values.OrderBy(i => i.Key))
            {
                Console.WriteLine(item);
            }
        }
    }
}