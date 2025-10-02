using MenuSystem;
using System;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, CONNECT4!");

            // Loo peamenüü ja alam-menüüd
            var mainMenu = new Menu("Connect4 Main Menu", EMenuLevel.Root);
            var settings = new Menu("Settings", EMenuLevel.First);
            var difficultyMenu = new Menu("Difficulty Levels", EMenuLevel.Deep);

            // Lisa Settings submenu peamenüüsse
            mainMenu.AddSubMenu("s", settings);

            // Lisa Difficulty submenu Settings menüüsse
            settings.AddSubMenu("d", difficultyMenu);

            // Näidisvalik: New Game peamenüüs
            mainMenu.AddMenuItem("n", "New Game", () =>
            {
                Console.WriteLine("Starting new game...");
                Console.ReadKey();
                return "";
            });
            
            mainMenu.Run();
            
            Console.ReadKey();
        }
    }
}