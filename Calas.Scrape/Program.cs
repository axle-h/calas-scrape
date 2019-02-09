using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Calas.Scrape.Models;
using CsvHelper;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Calas.Scrape
{
    class Program
    {
        public static int Main(string[] args)
        {
            var app = new CommandLineApplication();
            app.HelpOption("-h|--help");
            app.FullName = "Calas Scrape";
            app.Description = "Scraper for the Calas customer portal";
            app.Name = "Calas.Scrape";

            var optionStart = app.Option<int>("-s|--start-index <START-INDEX>", "The zero based start index", CommandOptionType.SingleValue)
                                 .IsRequired()
                                 .Accepts(v => v.Range(0, int.MaxValue));

            var optionLimit = app.Option<int>("-l|--limit <LIMIT>", "The maximum number of records to scrape", CommandOptionType.SingleValue)
                                 .IsRequired()
                                 .Accepts(v => v.Range(1, int.MaxValue));

            var optionUsername = app.Option("-u|--username <USERNAME>", "Your Calas username", CommandOptionType.SingleValue).IsRequired();
            var optionPassword = app.Option("-p|--password <PASSWORD>", "Your Calas password", CommandOptionType.SingleValue).IsRequired();

            app.OnExecute(() => RunAsync(optionUsername.Value(), optionPassword.Value(), optionStart.ParsedValue, optionLimit.ParsedValue));

            return app.Execute(args);
        }

        private static async Task RunAsync(string username, string password, int start, int limit)
        {
            var services = new ServiceCollection();
            services.AddLogging(x => x.AddConsole());
            services.AddTransient<CalasClient>();

            using (var provider = services.BuildServiceProvider())
            using (var client = provider.GetRequiredService<CalasClient>())
            {
                var logger = provider.GetRequiredService<ILogger<Program>>();
                ICollection<Contact> contacts;

                try
                {
                    await client.LoginAsync(username, password);
                    contacts = await client.GetContactsAsync(start, limit);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Failed to scrape Calas");
                    return;
                }

                using (var writer = new StreamWriter($"contacts-{start}-{start + limit}.csv"))
                using (var csv = new CsvWriter(writer))
                {
                    csv.Configuration.SanitizeForInjection = false;
                    csv.WriteRecords(contacts);
                }

                using (var writer = new StreamWriter($"enquiries-for-contacts-{start}-{start + limit}.csv"))
                using (var csv = new CsvWriter(writer))
                {
                    csv.Configuration.SanitizeForInjection = false;
                    csv.WriteRecords(contacts.SelectMany(x => x.Enquiries));
                }
            }
        }
    }
}
