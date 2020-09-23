using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Microsoft.Rest.Azure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace DeleteAllAssets
{
    class Program
    {
        private static IAzureMediaServicesClient client = null;
        static void Main(string[] args)
        {
            ConfigWrapper config = new ConfigWrapper(new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build());

            try
            {
                RunApplication(config);
            }
            catch (Exception exception)
            {
                if (exception.Source.Contains("ActiveDirectory"))
                {
                    Console.Error.WriteLine("TIP: Make sure that you have filled out the appsettings.json file before running this sample.");
                }

                Console.Error.WriteLine($"{exception.Message}");

                ApiErrorException apiException = exception.GetBaseException() as ApiErrorException;
                if (apiException != null)
                {
                    Console.Error.WriteLine(
                        $"ERROR: API call failed with error code '{apiException.Body.Error.Code}' and message '{apiException.Body.Error.Message}'.");
                }
            }
        }
        private static void RunApplication(ConfigWrapper config)
        {
            client = CreateMediaServicesClient(config);
            // Set the polling interval for long running operations to 2 seconds.
            // The default value is 30 seconds for the .NET client SDK
            client.LongRunningOperationRetryTimeout = 2;

            List<string> assetNameList = new List<string>();
            Console.WriteLine("Building a list of assets...");

            // Get a list of all of the assets and enumerate through them a page at a time.
            IPage<Asset> assetPage = client.Assets.List(config.ResourceGroup, config.AccountName);
            bool breakout;
            do
            {
                foreach (Asset asset in assetPage)
                {
                    // Add each asset name to a list
                    assetNameList.Add(asset.Name);
                }
                if (assetPage.NextPageLink != null)
                {
                    assetPage = client.Assets.ListNext(assetPage.NextPageLink);
                    breakout = false;
                }
                else
                {
                    breakout = true;
                }
            }
            while (!breakout);

            // We delete in a separate for loop because deleting within the do loop changes the size of the loop itself.
            bool always = false;
            for (int i = 0; i < assetNameList.Count; i++)
            {
                if (!always)
                {
                    Console.WriteLine("Delete the asset '{0}'?  (y)es, (n)o, (a)ll assets, (q)uit", assetNameList[i]);
                    ConsoleKeyInfo response = Console.ReadKey();
                    Console.WriteLine();
                    string responseChar = response.Key.ToString();

                    if (responseChar.Equals("N"))
                        continue;
                    if (responseChar.Equals("A"))
                    {
                        always = true;
                    }
                    else if (!(responseChar.Equals("Y")))
                    {
                        break; // At this point anything other than a 'yes' should quit the loop/application.
                    }
                }
                Console.WriteLine("Deleting {0}", assetNameList[i]);
                client.Assets.Delete(config.ResourceGroup, config.AccountName, assetNameList[i]);
            }
        }
        private static IAzureMediaServicesClient CreateMediaServicesClient(ConfigWrapper config)
        {
            ArmClientCredentials credentials = new ArmClientCredentials(config);
            return new AzureMediaServicesClient(config.ArmEndpoint, credentials)
            {
                SubscriptionId = config.SubscriptionId,
            };
        }
    }
    public class ArmClientCredentials : ServiceClientCredentials
    {
        private readonly AuthenticationContext _authenticationContext;
        private readonly Uri _customerArmAadAudience;
        private readonly ClientCredential _clientCredential;
        public ArmClientCredentials(ConfigWrapper config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            var authority = config.AadEndpoint.AbsoluteUri + config.AadTenantId;
            _authenticationContext = new AuthenticationContext(authority);
            _customerArmAadAudience = config.ArmAadAudience;
            _clientCredential = new ClientCredential(config.AadClientId, config.AadSecret);
        }
        public async override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var token = await _authenticationContext.AcquireTokenAsync(_customerArmAadAudience.OriginalString, _clientCredential);
            request.Headers.Authorization = new AuthenticationHeaderValue(token.AccessTokenType, token.AccessToken);
            await base.ProcessHttpRequestAsync(request, cancellationToken);
        }
    }
}