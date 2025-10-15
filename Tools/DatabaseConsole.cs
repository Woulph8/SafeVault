using SafeVault.Tools;

namespace SafeVault.ConsoleApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("SafeVault Database Viewer");
            Console.WriteLine("========================");
            Console.WriteLine();
            
            try
            {
                await DatabaseViewer.ShowAllUsers();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}