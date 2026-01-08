using System;
namespace MenuSystem
{
    // Single menu item - represents one selectable option
    // Contains shortcut key, display text, and action to execute
    public class MenuItem
    {
        public string Key { get; set; }              // Shortcut key: "n", "1", "x"
        public string Value { get; set; }            // Display text: "New Game", "Exit"
        public Func<string>? MethodToRun { get; set; }  // Action function (returns navigation: "", "x", "m", "b")

        public MenuItem(string key, string value, Func<string>? methodToRun = null)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Value = value ?? throw new ArgumentNullException(nameof(value));
            MethodToRun = methodToRun;  // Can be null for simple items
        }

        // String representation: "n) New Game"
        public override string ToString()
        {
            return $"{Key}) {Value}";
        }
    }
}
