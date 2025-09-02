using Codezerg.Data.Repository.Example.Examples;

namespace Codezerg.Data.Repository.Example;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("====================================");
        Console.WriteLine("  Codezerg.Data.Repository Examples");
        Console.WriteLine("====================================");
        
        // Check if we should run in non-interactive mode
        bool isInteractive = Environment.UserInteractive && !Console.IsInputRedirected;
        
        if (!isInteractive || args.Length > 0)
        {
            // Run all examples in non-interactive mode
            Console.WriteLine("\nRunning in non-interactive mode...\n");
            RunAllExamplesNonInteractive();
            return;
        }
        
        // Interactive mode
        bool exit = false;
        while (!exit)
        {
            Console.WriteLine("\nSelect an example to run:");
            Console.WriteLine("1. InMemory Repository");
            Console.WriteLine("2. Database Repository (SQLite)");
            Console.WriteLine("3. Cached Repository");
            Console.WriteLine("4. Dependency Injection");
            Console.WriteLine("5. Run All Examples");
            Console.WriteLine("0. Exit");
            Console.Write("\nEnter your choice: ");
            
            var choice = Console.ReadLine();
            
            try
            {
                switch (choice)
                {
                    case "1":
                        InMemoryExample.Run();
                        break;
                    case "2":
                        DatabaseExample.Run();
                        break;
                    case "3":
                        CachedExample.Run();
                        break;
                    case "4":
                        DependencyInjectionExample.Run();
                        break;
                    case "5":
                        RunAllExamples();
                        break;
                    case "0":
                        exit = true;
                        Console.WriteLine("\nGoodbye!");
                        break;
                    default:
                        Console.WriteLine("\nInvalid choice. Please try again.");
                        break;
                }
                
                if (!exit && choice != "0")
                {
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                    Console.Clear();
                    Console.WriteLine("====================================");
                    Console.WriteLine("  Codezerg.Data.Repository Examples");
                    Console.WriteLine("====================================");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError: {ex.Message}");
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
        }
    }
    
    static void RunAllExamples()
    {
        Console.WriteLine("\n========== RUNNING ALL EXAMPLES ==========\n");
        
        Console.WriteLine("Press any key to start InMemory Repository example...");
        Console.ReadKey();
        InMemoryExample.Run();
        
        Console.WriteLine("\nPress any key to continue to Database Repository example...");
        Console.ReadKey();
        DatabaseExample.Run();
        
        Console.WriteLine("\nPress any key to continue to Cached Repository example...");
        Console.ReadKey();
        CachedExample.Run();
        
        Console.WriteLine("\nPress any key to continue to Dependency Injection example...");
        Console.ReadKey();
        DependencyInjectionExample.Run();
        
        Console.WriteLine("\n========== ALL EXAMPLES COMPLETED ==========");
    }
    
    static void RunAllExamplesNonInteractive()
    {
        Console.WriteLine("========== RUNNING ALL EXAMPLES ==========\n");
        
        try
        {
            Console.WriteLine(">>> InMemory Repository Example");
            InMemoryExample.Run();
            
            Console.WriteLine("\n>>> Database Repository Example");
            DatabaseExample.Run();
            
            Console.WriteLine("\n>>> Cached Repository Example");
            CachedExample.Run();
            
            Console.WriteLine("\n>>> Dependency Injection Example");
            DependencyInjectionExample.Run();
            
            Console.WriteLine("\n========== ALL EXAMPLES COMPLETED ==========");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError running examples: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}