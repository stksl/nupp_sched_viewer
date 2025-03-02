using AngleSharp;
using NuppSchedViewer.Core;

namespace NuppSchedViewer.Cli 
{
    public class Program 
    {
        public static async Task Main(string[] args) 
        {
            
            NuppHttpClient client = new NuppHttpClient(Configuration.Default.WithDefaultLoader());

            string csrfToken = await client.GetCsrfLoginTokenAsync();

            NuppUserInfo userInfo;
            if (args.Length != 2) 
            {
                Console.Write("Логін: ");
                string username = Console.ReadLine()!;
                Console.Write("Пароль: ");
                string password = Console.ReadLine()!;

                userInfo = new NuppUserInfo(username, password);
            } 
            else userInfo = new NuppUserInfo(args[0], args[1]);

            string? identityToken = await client.LoginAsync(csrfToken, userInfo);

            if (identityToken == null) 
            {
                Console.WriteLine("Неправильний пароль або логін");
                return;
            }

            Console.Write("Бажаний день для перегляду (ціле число - зрушення від сьогодні): ");
            int nDays = int.Parse(Console.ReadLine()!);
            IList<string> classes = await client.GetScheduleAsync(DateTime.Today.AddDays(nDays), identityToken);
            foreach(var @class in classes) 
            {
                Console.WriteLine(@class);
            }
        }
    }
}