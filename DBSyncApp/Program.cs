using Dotmim.Sync;
using Dotmim.Sync.SqlServer;

namespace HelloSync
{
    class Program
    {
        private static string serverConnectionString = $"Server=192.168.0.13,4368;Initial Catalog=SyncDbServer;Persist Security Info=False;User ID=softadmin;password=w23eW@#E;MultipleActiveResultSets=False;Encrypt=False;TrustServerCertificate=False;Connection Timeout=30;";

        private static string clientConnectionString = $"Server=192.168.0.13,4368;Initial Catalog=SyncDbClient;Persist Security Info=False;User ID=softadmin;password=w23eW@#E;MultipleActiveResultSets=False;Encrypt=False;TrustServerCertificate=False;Connection Timeout=30;";

        static async Task Main() => await SynchronizeAsync();


        private static async Task SynchronizeAsync()
        {
            // Database script used for this sample : https://github.com/Mimetis/Dotmim.Sync/blob/master/CreateAdventureWorks.sql 

            // Create 2 Sql Sync providers
            // First provider is using the Sql change tracking feature. Don't forget to enable it on your database until running this code !
            // For instance, use this SQL statement on your server database : ALTER DATABASE AdventureWorks  SET CHANGE_TRACKING = ON  (CHANGE_RETENTION = 10 DAYS, AUTO_CLEANUP = ON)  
            // Otherwise, if you don't want to use Change Tracking feature, just change 'SqlSyncChangeTrackingProvider' to 'SqlSyncProvider'
            var serverProvider = new SqlSyncProvider(serverConnectionString);

            // Second provider is using plain old Sql Server provider, relying on triggers and tracking tables to create the sync environment
            //var clientProvider = new SqliteSyncProvider("adv.db");
            var clientProvider = new SqlSyncProvider(clientConnectionString);

            // Tables involved in the sync process:
            //var setup = new SyncSetup("ProductCategory", "ProductModel", "Product",
            //            "Address", "Customer", "CustomerAddress", "SalesOrderHeader", "SalesOrderDetail");

            var setup = new SyncSetup("SyncClint");


            // Creating an agent that will handle all the process
            var agent = new SyncAgent(clientProvider, serverProvider);
            var process = new SynchronousProgress<ProgressArgs>(args => Console.WriteLine($"{args.Context.SessionId}"));

            var remoteOrchestrator = agent.RemoteOrchestrator;
            var localOrchestrator = agent.LocalOrchestrator;

            do
            {
                // Launch the sync process
                var s1 = await agent.SynchronizeAsync(setup);

                var serverScope = await remoteOrchestrator.ProvisionAsync(setup);
                setup = new SyncSetup("SyncClint");
                var schema = await remoteOrchestrator.GetSchemaAsync(setup);
                serverScope.Schema = schema;
                serverScope.Setup = setup;
                await remoteOrchestrator.ProvisionAsync(serverScope, overwrite: true);

                var cs = await localOrchestrator.ProvisionAsync(serverScope, overwrite: true);

                s1 = await agent.SynchronizeAsync(process);


                // Write results
                Console.WriteLine(s1);

            } while (Console.ReadKey().Key != ConsoleKey.Escape);

            Console.WriteLine("End");
        }
    }
}
