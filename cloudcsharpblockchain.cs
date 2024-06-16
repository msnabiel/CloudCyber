    /*using Google.Apis.Auth.OAuth2;
    using Google.Apis.Drive.v3;
    using Google.Apis.Services;
    using Google.Cloud.Firestore;
    using Nethereum.Web3;
    using Nethereum.RPC.Eth.DTOs;
    using Nethereum.Hex.HexTypes;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using System.Net.Mail;
    using System.Net;
    using System.IO;
    using FirebaseAdmin;
    using dotenv.net;
    using System;

    namespace GoogleDriveMonitor
    {
        class Program
        {
            private static FirebaseApp? firebaseApp;
            private static FirestoreDb? firestoreDb;
            private static DriveService? driveService;
            private static Web3? web3;
            //private static string contractAddress = "0x...";
            //private static string smtpServer = "smtp.example.com";
            //private static int smtpPort = 587;
            private static string smtpSenderEmail = "msyednabiel@gmail.com";
            private static string smtpSenderPassword = "wvyt rxjc vowz bnhr";
            //private static string privateKeyPath = "path/to/private_key.pem";
            //private static string publicKeyPath = "path/to/public_key.pem";
            private static string? contractAddress;
            private static string? smtpServer;
            private static int smtpPort;
            //private static string? smtpSenderEmail;
            //private static string? smtpSenderPassword;
            private static string? privateKeyPath;
            private static string? publicKeyPath;


            static async Task Main(string[] args)
            {
                DotEnv.Load();

                // Load variables from environment
                contractAddress = Environment.GetEnvironmentVariable("CONTRACT_ADDRESS");
                smtpServer = Environment.GetEnvironmentVariable("SMTP_SERVER");
                smtpPort = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT") ?? "587");
                //smtpSenderEmail = Environment.GetEnvironmentVariable("SMTP_SENDER_EMAIL");
                //smtpSenderPassword = Environment.GetEnvironmentVariable("SMTP_SENDER_PASSWORD");
                privateKeyPath = Environment.GetEnvironmentVariable("PRIVATE_KEY_PATH");
                publicKeyPath = Environment.GetEnvironmentVariable("PUBLIC_KEY_PATH");
                Console.WriteLine("Starting Google Drive Monitor application...");
                Console.WriteLine("Initializing Firebase...");
                InitializeFirebase();
                Console.WriteLine("Firebase initialized.");
                InitializeGoogleDrive();
                Console.WriteLine("Google Drive initialized.");
                //InitializeEthereum();
                //Console.WriteLine("Ethereum initialized.");
                await MonitorDrive();
                Console.WriteLine("Monitoring Google Drive...");
            }

            private static void InitializeFirebase()
            {
                firebaseApp = FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.FromFile("/Users/msnabiel/Downloads/msnabiel-c89f9bee7591.json")
                });
                firestoreDb = FirestoreDb.Create("msnabiel");
                Console.WriteLine("Firebase app created.");
            }
//                var credential = GoogleCredential.FromFile("/Users/msnabiel/Downloads/msnabiel-firebase-adminsdk-4o8hw-9f46559fc4.json")

            private static void InitializeGoogleDrive()
            {
                var credential = GoogleCredential.FromFile("/Users/msnabiel/Downloads/msnabiel-c89f9bee7591.json")
                    .CreateScoped(DriveService.ScopeConstants.Drive);

                // Get authenticated user's email address for debugging purposes
                var service = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Google Drive Monitor"
                });

                var request = service.About.Get();
                request.Fields = "user/emailAddress";

                var response = request.Execute();
                var emailAddress = response.User.EmailAddress;

                Console.WriteLine($"Authenticated Google Drive account: {emailAddress}");

                // Initialize Drive service
                driveService = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Google Drive Monitor"
                });
            }


            /*private static void InitializeEthereum()
            {
                web3 = new Web3("https://mainnet.infura.io/v3/1ccce0086b8246c9a42eb42240c21af9");
                Console.WriteLine("Ethereum web3 provider initialized.");
            }*/

            /*private static async Task MonitorDrive()
{
    if (driveService == null)
    {
        Console.WriteLine("Drive service not initialized. Exiting monitoring.");
        return;
    }

    var changesRequest = driveService.Changes.GetStartPageToken();
    var startPageToken = (await changesRequest.ExecuteAsync())?.StartPageTokenValue;

    while (true)
    {
        Console.WriteLine("Montioring Google Drive 4 U...");
        if (startPageToken == null)
        {
            Console.WriteLine("Start page token is null. Exiting monitoring.");
            return;
        }

        try
        {
            var changes = await driveService.Changes.List(startPageToken).ExecuteAsync();
            if (changes?.Changes == null)
            {
                Console.WriteLine("No changes found in Google Drive.");
                await Task.Delay(10000); // Wait for 1 minute before checking again
                continue;
            }

            foreach (var change in changes.Changes)
            {
                string fileId = change.FileId ?? "Unknown";
                string fileName = change.File?.Name ?? "Unknown";
                string modifiedTime = change.File?.ModifiedTimeRaw ?? "Unknown";

                Console.WriteLine($"File {fileName} (ID: {fileId}) was modified at {modifiedTime}");

                //await StoreChangeOnBlockchain(fileId, fileName, modifiedTime);
                SendEmailNotification("Google Drive Update", $"File {fileName} (ID: {fileId}) was modified at {modifiedTime}.");
            }

            startPageToken = changes.NewStartPageToken;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred during monitoring: {ex.Message}");
        }

        await Task.Delay(10000); // Wait for 1 minute before checking again
    }
}*/


            /*private static async Task StoreChangeOnBlockchain(string fileId, string fileName, string modifiedTime)
            {   
                Console.WriteLine("Storing change record on blockchain...");
                string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                string payload = $"{fileId},{fileName},{modifiedTime},{timestamp}";

                string txHash = await SendTransactionToBlockchain(payload);
                Console.WriteLine($"Change record stored on the blockchain. Transaction hash: {txHash}");
                
            }

            private static async Task<string> SendTransactionToBlockchain(string payload)
            {
                if (web3 == null || string.IsNullOrEmpty(contractAddress))
                    return string.Empty;

                var privateKey = Environment.GetEnvironmentVariable("PRIVATE_KEY"); // Make sure this is set in your environment variables

                // Create a new account object from the private key
                var account = new Nethereum.Web3.Accounts.Account(privateKey);
                web3 = new Web3(account, "https://mainnet.infura.io/v3/1ccce0086b8246c9a42eb42240c21af9"); // Replace with your provider

                // Create the transaction object
                var transactionInput = new TransactionInput
                {
                    From = account.Address,
                    To = contractAddress,
                    Data = payload,
                    Gas = new HexBigInteger(1000000),
                    GasPrice = new HexBigInteger(Web3.Convert.ToWei(50, Nethereum.Util.UnitConversion.EthUnit.Gwei))
                };

                // Send the transaction and get the transaction hash
                string txHash = await web3.Eth.Transactions.SendTransaction.SendRequestAsync(transactionInput);
                return txHash;
            }*/


            /*private static void SendEmailNotification(string subject, string body)
            {
                if (string.IsNullOrEmpty(smtpSenderEmail))
                {
                    Console.WriteLine("SMTP Sender Email is not configured.");
                    return;
                }

                // Print out the sender and recipient email addresses for debugging
                Console.WriteLine($"Sender Email: {smtpSenderEmail}");
                string recipientEmail = "msyednabiel@gmail.com"; // Replace with your recipient email
                Console.WriteLine($"Recipient Email: {recipientEmail}");

                var msg = new MailMessage();
                try
                {
                    msg.From = new MailAddress(smtpSenderEmail);
                }
                catch (ArgumentNullException ex)
                {
                    Console.WriteLine($"Failed to create MailAddress: {ex.Message}");
                    return;
                }

                msg.Subject = subject;
                msg.Body = body;
                msg.IsBodyHtml = false;
                msg.To.Add(recipientEmail);

                using (var client = new SmtpClient(smtpServer, smtpPort))
                {
                    client.Credentials = new NetworkCredential(smtpSenderEmail, smtpSenderPassword);
                    client.EnableSsl = true; // Ensure SSL is enabled

                    try
                    {
                        client.Send(msg);
                        Console.WriteLine("Email notification sent successfully.");
                    }
                    catch (SmtpException ex)
                    {
                        Console.WriteLine($"Error sending email: {ex.Message}");
                    }
                }
            }




            private static string ComputeFileHash(byte[] fileBytes)
            {
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(fileBytes);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                }
            }*/
        

            /*private static string ComputeDigitalSignature(string fileHash)
            {
                if (string.IsNullOrWhiteSpace(privateKeyPath) || !File.Exists(privateKeyPath)) return string.Empty;

                using (RSA rsa = RSA.Create())
                {
                    rsa.ImportFromPem(File.ReadAllText(privateKeyPath));
                    byte[] hashBytes = Encoding.UTF8.GetBytes(fileHash);
                    byte[] signatureBytes = rsa.SignData(hashBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
                    return Convert.ToBase64String(signatureBytes);
                }
            }

            private static async Task VerifyDigitalSignature(string fileId, string fileHash)
            {
                string? signature = await GetSignatureFromBlockchain(fileId);
                if (string.IsNullOrEmpty(signature))
                {
                    Console.WriteLine("No digital signature found on the blockchain.");
                    return;
                }

                try
                {
                    if (string.IsNullOrWhiteSpace(publicKeyPath) || !File.Exists(publicKeyPath)) return;

                    using (RSA rsa = RSA.Create())
                    {
                        rsa.ImportFromPem(File.ReadAllText(publicKeyPath));
                        byte[] hashBytes = Encoding.UTF8.GetBytes(fileHash);
                        byte[] signatureBytes = Convert.FromBase64String(signature);
                        bool isVerified = rsa.VerifyData(hashBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
                        Console.WriteLine(isVerified ? "Digital signature verified successfully." : "Digital signature verification failed.");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Digital signature verification failed: {e.Message}");
                }
            }

            private static async Task<string> GetSignatureFromBlockchain(string fileId)
            {
                if (web3 == null) return string.Empty;

                var contract = web3.Eth.GetContract("your_contract_abi", contractAddress);
                var getFileSignatureFunction = contract.GetFunction("getFileSignature");

                string? signature = await getFileSignatureFunction.CallAsync<string>(fileId);
                return signature ?? string.Empty;
            }
        }
    }
*/