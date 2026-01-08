using System;
using System.Collections.Generic;
using System.Linq;

namespace MenuSystem
{
    // Menu system with automatic exit options based on level
    // Supports 2 input modes: text input and arrow key navigation
    public class Menu
    {
        private readonly string _title;
        private readonly EMenuLevel _level;  // Root/First/Deep
        private readonly Dictionary<string, MenuItem> _menuItems;

        public Menu(string title, EMenuLevel level)
        {
            _title = title ?? throw new ArgumentNullException(nameof(title));
            _level = level;
            _menuItems = new Dictionary<string, MenuItem>();

            // Add automatic exit options based on menu level
            switch (_level)
            {
                case EMenuLevel.Root:
                    // Root menu: only Exit
                    _menuItems["x"] = new MenuItem("x", "Exit", () => "x");
                    break;
                case EMenuLevel.First:
                    // First level submenu: Main Menu + Exit
                    _menuItems["m"] = new MenuItem("m", "Return to Main Menu", () => "m");
                    _menuItems["x"] = new MenuItem("x", "Exit", () => "x");
                    break;
                case EMenuLevel.Deep:
                    // Deep submenu: Back + Main Menu + Exit
                    _menuItems["b"] = new MenuItem("b", "Back to Previous Menu", () => "b");
                    _menuItems["m"] = new MenuItem("m", "Return to Main Menu", () => "m");
                    _menuItems["x"] = new MenuItem("x", "Exit", () => "x");
                    break;
            }
        }

        // Add menu item with action
        public void AddMenuItem(string key, string value, Func<string> methodToRun)
        {
            key = key?.ToLower() ?? throw new ArgumentNullException(nameof(key));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Menu item key cannot be null or empty.", nameof(key));
            if (_menuItems.ContainsKey(key))
                throw new ArgumentException($"Menu item with key '{key}' already exists.");
            _menuItems[key] = new MenuItem(key, value, methodToRun);
        }

        // Add submenu (creates MenuItem that calls subMenu.Run)
        public void AddSubMenu(string key, Menu subMenu)
        {
            key = key?.ToLower() ?? throw new ArgumentNullException(nameof(key));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Submenu key cannot be null or empty.", nameof(key));
            if (_menuItems.ContainsKey(key))
                throw new ArgumentException($"Shortcut '{key}' on juba kasutusel.");
            if (subMenu == null)
                throw new ArgumentNullException(nameof(subMenu));
            // Create MenuItem with reference to submenu's Run method
            _menuItems[key] = new MenuItem(key, subMenu._title, subMenu.Run);
        }

        // Update menu item label dynamically (for showing current values)
        public void UpdateMenuItemLabel(string key, string newValue)
        {
            key = key?.ToLower() ?? throw new ArgumentNullException(nameof(key));
            if (!_menuItems.ContainsKey(key))
                throw new ArgumentException($"Menu item with key '{key}' does not exist.");
            if (string.IsNullOrWhiteSpace(newValue))
                throw new ArgumentException("Menu item value cannot be null or empty.", nameof(newValue));
            _menuItems[key].Value = newValue;
        }

        // Main menu loop - supports 2 input modes
        public string Run()
        {
            bool useCursorInput = false;  // false = text input, true = arrow keys
            while (true)
            {
                Console.Clear();
                DisplayMenu();

                if (useCursorInput)
                {
                    // ARROW KEYS MODE
                    string selectedKey = RunCursorBasedInput();
                    if (selectedKey == "TOGGLE_MODE")
                    {
                        useCursorInput = false;  // Switch to text mode
                        continue;
                    }
                    string result = ProcessSelection(selectedKey);
                    if (result != "") return result;  // Navigation command
                }
                else
                {
                    // TEXT INPUT MODE
                    Console.Write("Select an option (or '/' for cursor mode): ");
                    string? input = Console.ReadLine()?.Trim().ToLower();
                    if (input == "/")
                    {
                        useCursorInput = true;  // Switch to arrow keys mode
                        continue;
                    }
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        Console.WriteLine("Invalid input. Press any key to continue...");
                        Console.ReadKey();
                        continue;
                    }
                    string result = ProcessSelection(input);
                    if (result != "") return result;  // Navigation command
                }
            }
        }

        // Process menu selection and handle navigation
        private string ProcessSelection(string key)
        {
            key = key?.ToLower() ?? "";
            if (_menuItems.ContainsKey(key))
            {
                // Execute menu item action
                string result = _menuItems[key].MethodToRun?.Invoke() ?? "";
                
                // Handle navigation commands
                // "x" = Exit (works at all levels)
                if (result == "x")
                {
                    return result;
                }
                
                // "m" = Return to Main Menu (works at First and Deep levels)
                if (result == "m")
                {
                    if (_level == EMenuLevel.First || _level == EMenuLevel.Deep)
                        return result;
                }
                
                // "b" = Back (works only at Deep level)
                if (result == "b")
                {
                    if (_level == EMenuLevel.Deep)
                        return result;
                }
                
                // Empty string = stay in current menu
                return "";
            }
            else
            {
                Console.WriteLine("Invalid option. Press any key to continue...");
                Console.ReadKey();
            }
            return "";
        }

        // Arrow key navigation mode
        private string RunCursorBasedInput()
        {
            var items = _menuItems.Values.OrderBy(i => i.Key).ToList();
            int selectedIndex = 0;
            ConsoleKey key;

            do
            {
                Console.Clear();
                DisplayMenu();
                Console.WriteLine($"\nUse arrow keys to navigate, Enter to select, '/' to switch to text input");

                // Display items with selection marker
                for (int i = 0; i < items.Count; i++)
                {
                    if (i == selectedIndex)
                        Console.Write(">");  // Selected item
                    else
                        Console.Write(" ");
                    Console.WriteLine($" {items[i]}");
                }

                // Read key
                key = Console.ReadKey(true).Key;
                
                // Handle arrow keys
                if (key == ConsoleKey.UpArrow && selectedIndex > 0)
                    selectedIndex--;
                else if (key == ConsoleKey.DownArrow && selectedIndex < items.Count - 1)
                    selectedIndex++;
                else if (key == ConsoleKey.Divide || key == ConsoleKey.Oem2)  // '/' key
                    return "TOGGLE_MODE";
                    
            } while (key != ConsoleKey.Enter);

            return items[selectedIndex].Key;
        }

        // Display menu title and items
        private void DisplayMenu()
        {
            Console.WriteLine($"\n{_title}");
            Console.WriteLine(new string('-', _title.Length + 4));  // Underline
            foreach (var item in _menuItems.Values.OrderBy(i => i.Key))
            {
                Console.WriteLine(item);  // Calls MenuItem.ToString()
            }
        }
    }
}
