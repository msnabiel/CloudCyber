/*
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Cloud.Storage.V1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;

namespace GoogleDriveMonitor
{
    class Program
    {
        private static DriveService? driveService;
        private static string smtpSenderEmail = string.Empty;
        private static string smtpSenderPassword = string.Empty;
        private static string smtpServer = "smtp.google.com"; // Default SMTP server
        private static int smtpPort = 587; // Default SMTP port
        //private static string privateKeyPath = string.Empty;
        //private static string publicKeyPath = string.Empty;
        //private static string contractAddress = string.Empty;
        private static string? userIpAddress; // Optional: store user IP address

        static async Task Main(string[] args)
        {
            LoadEnvironmentVariables();

            Console.WriteLine("Starting Google Drive Monitor application...");
            InitializeGoogleDrive();
            Console.WriteLine("Google Drive initialized.");

            // Optional: Log user IP address during initialization
            LogUserIpAddress();

            await MonitorDrive();
        }

        private static void LoadEnvironmentVariables()
        {
            smtpSenderEmail = Environment.GetEnvironmentVariable("SMTP_SENDER_EMAIL") ?? throw new ApplicationException("SMTP_SENDER_EMAIL environment variable not set.");
            smtpSenderPassword = Environment.GetEnvironmentVariable("SMTP_SENDER_PASSWORD") ?? throw new ApplicationException("SMTP_SENDER_PASSWORD environment variable not set.");
            smtpServer = Environment.GetEnvironmentVariable("SMTP_SERVER") ?? smtpServer;
            smtpPort = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out int port) ? port : smtpPort;
            /*privateKeyPath = Environment.GetEnvironmentVariable("PRIVATE_KEY_PATH") ?? throw new ApplicationException("PRIVATE_KEY_PATH environment variable not set.");
            publicKeyPath = Environment.GetEnvironmentVariable("PUBLIC_KEY_PATH") ?? throw new ApplicationException("PUBLIC_KEY_PATH environment variable not set.");
            contractAddress = Environment.GetEnvironmentVariable("CONTRACT_ADDRESS") ?? throw new ApplicationException("CONTRACT_ADDRESS environment variable not set.");

            // Initialize FirebaseApp
            FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.GetApplicationDefault(),
            });
        }
    
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
  

        private static async Task MonitorDrive()
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
                Console.WriteLine("Monitoring Google Drive...");

                if (startPageToken == null)
                {
                    Console.WriteLine("Start page token is null. Exiting monitoring.");
                    return;
                }

                try
                {
                    var changes = await driveService.Changes.List(startPageToken).ExecuteAsync();

                    if (changes == null || changes.Changes == null || changes.Changes.Count == 0)
                    {
                        Console.WriteLine("No changes found in Google Drive.");
                        await Task.Delay(10000); // Wait for 1 minute before checking again
                        continue;
                    }

                    foreach (var change in changes.Changes)
                    {
                        string fileId = change.FileId ?? "Unknown";
                        string fileName = change.File?.Name ?? "Unknown";

                        // Calculate hash and compare with stored hash
                        var hashResult = await ComputeFileHashAsync(fileId);
                        if (hashResult == null)
                        {
                            Console.WriteLine($"Error computing hash for file {fileId}");
                            continue; // Skip this file and move to the next change
                        }

                        string currentHash = hashResult;
                        bool isModified = !string.Equals(currentHash, change.File?.Md5Checksum, StringComparison.OrdinalIgnoreCase);

                        if (isModified)
                        {
                            string modifiedTime = change.File?.ModifiedTimeRaw ?? "Unknown";
                            Console.WriteLine($"File {fileName} (ID: {fileId}) was modified at {modifiedTime}");

                            // Send push notification
                            await SendPushNotification($"File {fileName} was modified");
                            
                            SendEmailNotification("Google Drive Update", $"File {fileName} (ID: {fileId}) was modified at {modifiedTime}.");
                        }
                        else
                        {
                            Console.WriteLine($"File {fileName} (ID: {fileId}) has not been modified.");
                        }
                    }

                    startPageToken = changes.NewStartPageToken;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error occurred during monitoring: {ex.Message}");
                }

                await Task.Delay(10000); // Wait for 1 minute before checking again
            }
        }

        private static async Task<string?> ComputeFileHashAsync(string fileId)
        {
            if (driveService == null)
            {
                return null; // Return null if driveService is not initialized
            }

            var request = driveService.Files.Get(fileId);
            using (var stream = new MemoryStream())
            {
                await request.DownloadAsync(stream);
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(stream.ToArray());
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                }
            }
        }

        private static void SendEmailNotification(string subject, string body)
        {
            Console.WriteLine("Sending email notification...");

            var msg = new MailMessage();

            msg.From = new MailAddress(smtpSenderEmail);
            msg.Subject = subject;
            msg.Body = body;
            msg.IsBodyHtml = false;
            msg.To.Add("msyednabiel@gmail.com"); // Replace with actual recipient email

            using (var client = new SmtpClient(smtpServer, smtpPort))
            {
                client.Credentials = new NetworkCredential(smtpSenderEmail, smtpSenderPassword);
                client.EnableSsl = true;

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

        private static async Task SendPushNotification(string message)
        {
            Console.WriteLine($"Sending push notification: {message}");

            try
            {
                // This registration token must be taken from the client app once it's ready to receive notifications.
                string registrationToken = "YOUR_DEVICE_REGISTRATION_TOKEN";

                // See documentation on defining a message payload.
                var messageObj = new FirebaseAdmin.Messaging.Message()
                {
                    Notification = new FirebaseAdmin.Messaging.Notification
                    {
                        Title = "Google Drive Update",
                        Body = message,
                    },
                    Token = registrationToken,
                };

                // Send a message to the device corresponding to the provided registration token.
                string response = await FirebaseMessaging.DefaultInstance.SendAsync(messageObj);
                Console.WriteLine($"Successfully sent message: {response}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending FCM message: {ex.Message}");
            }
        }

        private static void LogUserIpAddress()
        {
            userIpAddress = GetUserIpAddress();
            Console.WriteLine($"User IP Address: {userIpAddress}");
        }

        private static string GetUserIpAddress()
        {
            string ipAddress = string.Empty;
            try
            {
                ipAddress = Dns.GetHostEntry(Dns.GetHostName())
                                .AddressList
                                .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                                ?.ToString() ?? "Unknown";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving IP address: {ex.Message}");
            }
            return ipAddress;
        }
    }
}
*/