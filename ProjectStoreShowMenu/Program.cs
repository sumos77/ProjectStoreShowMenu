using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Shop
{
    public class Product
    {
        public string Code;
        public string Name;
        public string Description;
        public int Price;
    }

    public class Program
    {
        // A static variable in the `Program` class is available in every method of that class. Effectively a "global" variable.

        // An array of all the products available, loaded from "Products.csv".
        public static Product[] Products;

        // A shopping cart is a dictionary mapping a Product object to the number of copies of that product we have added.
        public static Dictionary<Product, int> Cart;

        public static Dictionary<string, int> Rebate = new Dictionary<string, int>();

        // We store product information in a CSV file in the project directory.
        public const string ProductFilePath = "Products.csv";

        public const string RebateFilePath = "Rebates.csv";

        // We store the saved shopping cart in a CSV file outside the project directory, because then it will not be overwritten everytime we start the program.
        public const string CartFilePath = @"C:\Windows\Temp\Cart.csv";

        public static void Main()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            Products = LoadProducts();
            LoadRebates();

            // If we have a saved cart, load it first from the text file.
            if (File.Exists(CartFilePath))
            {
                Cart = LoadCart();
                Console.WriteLine("Din sparade varukorg har laddats:");
                Console.WriteLine();
                Console.WriteLine(CartToString());
                Console.WriteLine();
            }
            // Otherwise create an empty cart.
            else
            {
                Cart = new Dictionary<Product, int>();
            }

            // Loop until the user is done. The `done` variable will be set to true when the user places an order or saves the cart.
            bool done = false;
            while (!done)
            {
                int option = ShowMenu("Vad vill du göra?", new[]
                {
                    "Lägg till produkt",
                    "Beställ",
                    "Spara varukorg",
                    "Lägg till rabbatt"
                });
                Console.Clear();

                if (option == 0)
                {
                    AddProduct();
                }
                else if (option == 1)
                {
                    PlaceOrder();
                    done = true;
                }
                else if (option == 2)
                {
                    SaveCart();
                    done = true;
                }
            }

            Console.WriteLine();
            Console.WriteLine("Välkommen åter!");
        }

        // Add a product to the cart.
        // This method takes no parameters but accesses the `Cart` variable, since it is `static` and thus available in every method of this class.
        public static void AddProduct()
        {
            // Turn the list of products into an array of string options for ShowMenu.
            List<string> options = new List<string>();
            foreach (Product p in Products)
            {
                options.Add(p.Code + ": " + p.Name + " - " + p.Description + " (" + p.Price + " kr)");
            }
            int productIndex = ShowMenu("Välj produkt", options.ToArray());
            Product selected = Products[productIndex];

            Console.Write("Ange antal att köpa: ");
            int amount = int.Parse(Console.ReadLine());

            // If the user has previously added this product, simply increase the amount.
            if (Cart.ContainsKey(selected))
            {
                Cart[selected] += amount;
            }
            // If not, we cannot increase the amount (since there is no amount for that product yet at all) so we simply set it to the number that was entered.
            else
            {
                Cart[selected] = amount;
            }

            Console.Clear();
            Console.WriteLine(amount + " exemplar av '" + selected.Name + "' har lagts till i varukorgen");
            Console.WriteLine();
        }

        public static void PlaceOrder()
        {
            Console.WriteLine("Du har lagt följande beställning:");
            Console.WriteLine();
            Console.WriteLine(CartToString());

            Cart.Clear();
            if (File.Exists(CartFilePath))
            {
                File.Delete(CartFilePath);
            }
        }

        // Save the shopping cart to "Cart.csv".
        // Like before, this method takes no parameters but accesses the static `Cart` variable.
        public static void SaveCart()
        {
            // Create an empty list of text lines that we will fill with strings and then write to a textfile using `WriteAllLines`.
            List<string> lines = new List<string>();
            foreach (KeyValuePair<Product, int> pair in Cart)
            {
                Product p = pair.Key;
                int amount = pair.Value;

                // For each product, we only save the code and the amount.
                // The other info (name, price, description) is already in "Products.csv" and we can look it up when we load the cart.
                lines.Add(p.Code + "," + amount);
            }
            File.WriteAllLines(CartFilePath, lines);

            Console.WriteLine("Din varukorg har sparats: ");
            Console.WriteLine();
            Console.WriteLine(CartToString());
        }

        public static decimal RebateCode()
        {
            string enteredRebate = "";
            Console.Write("Enter rebatecode: ");
            enteredRebate = Console.ReadLine().ToUpper();
            decimal rebatePercent = 1;
            if (enteredRebate != "") 
            {
                try
                {
                    rebatePercent = Rebate[enteredRebate];
                    rebatePercent = 1 - (rebatePercent / 100);
                }
                catch
                {
                    Console.WriteLine("Not valid!");
                }
            }
            
            //foreach (KeyValuePair<string, int> pair in Rebate)
            //{
            //    if (pair.Key == enteredRebate)
            //    {
            //        rebatePercent = pair.Value;
            //        rebatePercent = rebatePercent / 100;
            //    }
            //}
            //if (rebatePercent == 0)
            //{
            //    Console.WriteLine("Not Valid!");
            //}

            return rebatePercent;
        }

        // Build a string describing the contents of the shopping cart.
        // Once again, this method access the static `Cart` variable.
        public static string CartToString()
        {
            // Create an empty string to build from.
            string s = "";

            // Loop over the products in the cart (a dictionary) and add their info to the string while also calculating the total sum.
            decimal sum = 0;
            foreach (KeyValuePair<Product, int> pair in Cart)
            {
                Product p = pair.Key;
                int amount = pair.Value;

                s += p.Name + ": " + amount + " st" + Environment.NewLine;

                // The number to add to the sum is this product's price multiplied by the number of copies we added.
                sum += p.Price * amount * RebateCode();
            }
            s += Environment.NewLine;
            s += "Totalsumma: " + sum + " kr";

            return s;
        }

        public static void LoadRebates()
        {
            if (!File.Exists(RebateFilePath))
            {
                Console.WriteLine(ProductFilePath + " finns inte, eller har inte blivit satt till 'Copy Always'.");
                Environment.Exit(1);
            }

            string[] lines = File.ReadAllLines(RebateFilePath);
            foreach (string line in lines)
            {
                // First, split the line on commas (CSV means "comma-separated values").
                string[] parts = line.Split(',');
                Rebate.Add(parts[0], int.Parse(parts[1]));
            }
        }

        // Read the CSV file "Products.csv" and create product objects from it.
        public static Product[] LoadProducts()
        {
            // If the file doesn't exist, stop the program completely.
            if (!File.Exists(ProductFilePath))
            {
                Console.WriteLine(ProductFilePath + " finns inte, eller har inte blivit satt till 'Copy Always'.");
                Environment.Exit(1);
            }

            // Create an empty list of products, then go through each line of the file to fill it.
            List<Product> products = new List<Product>();
            string[] lines = File.ReadAllLines(ProductFilePath);
            foreach (string line in lines)
            {
                try
                {
                    // First, split the line on commas (CSV means "comma-separated values").
                    string[] parts = line.Split(',');

                    // Then create a product with its values set to the different parts of the line.
                    Product p = new Product
                    {
                        Code = parts[0],
                        Name = parts[1],
                        Description = parts[2],
                        Price = int.Parse(parts[3])
                    };
                    products.Add(p);
                }
                catch
                {
                    Console.WriteLine("Fel vid inläsning av en produkt!");
                }
            }

            // The method returns an array rather than a list (because the products are fixed after the program has started), so we need to convert it before returning.
            return products.ToArray();
        }

        // Load a saved cart from "Cart.csv". This method is similar to `LoadProducts` but with some notable differences.
        public static Dictionary<Product, int> LoadCart()
        {
            // A cart is a dictionary (as described earlier), so create an empty one to fill as we read the CSV file.
            Dictionary<Product, int> savedCart = new Dictionary<Product, int>();

            // Go through each line and split it on commas, as in `LoadProducts`.
            string[] lines = File.ReadAllLines(CartFilePath);
            foreach (string line in lines)
            {
                string[] parts = line.Split(',');
                string code = parts[0];
                int amount = int.Parse(parts[1]);

                // We only store the product's code in the CSV file, but we need to find the actual product object with that code.
                // To do this, we access the static `products` variable and find the one with the matching code, then grab that product object.
                Product current = null;
                foreach (Product p in Products)
                {
                    if (p.Code == code)
                    {
                        current = p;
                    }
                }

                // Now that we have the product object (and not just the code), we can save it in the dictionary.
                savedCart[current] = amount;
            }

            return savedCart;
        }

        public static int ShowMenu(string prompt, string[] options)
        {
            if (options == null || options.Length == 0)
            {
                throw new ArgumentException("Cannot show a menu for an empty array of options.");
            }

            Console.WriteLine(prompt);

            int selected = 0;

            // Hide the cursor that will blink after calling ReadKey.
            Console.CursorVisible = false;

            ConsoleKey? key = null;
            while (key != ConsoleKey.Enter)
            {
                // If this is not the first iteration, move the cursor to the first line of the menu.
                if (key != null)
                {
                    Console.CursorLeft = 0;
                    Console.CursorTop = Console.CursorTop - options.Length;
                }

                // Print all the options, highlighting the selected one.
                for (int i = 0; i < options.Length; i++)
                {
                    var option = options[i];
                    if (i == selected)
                    {
                        Console.BackgroundColor = ConsoleColor.Blue;
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    Console.WriteLine("- " + option);
                    Console.ResetColor();
                }

                // Read another key and adjust the selected value before looping to repeat all of this.
                key = Console.ReadKey().Key;
                if (key == ConsoleKey.DownArrow)
                {
                    selected = Math.Min(selected + 1, options.Length - 1);
                }
                else if (key == ConsoleKey.UpArrow)
                {
                    selected = Math.Max(selected - 1, 0);
                }
            }

            // Reset the cursor and return the selected option.
            Console.CursorVisible = true;
            return selected;
        }
    }
}
