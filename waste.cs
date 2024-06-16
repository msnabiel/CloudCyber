using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;

namespace GoogleDriveMonitor
{
    class Program
    {
        private static DriveService? driveService;
        private static string smtpSenderEmail = "msyednabiel@gmail.com";
        private static string smtpSenderPassword = "wvyt rxjc vowz bnhr";
        private static string smtpServer = "smtp.gmail.com";
        private static int smtpPort = 587;
        private static string timestampServerUrl = "https://time.certbot.org";
        private static string certificatePath = "/Users/msnabiel/Desktop/SHARP/CloudCyber/certificate.pfx";
        private static string certificatePassword = "Sunshine!23";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting Google Drive Monitor application...");
            InitializeGoogleDrive();
            Console.WriteLine("Google Drive initialized.");

            await MonitorDrive();
        }

        private static void InitializeGoogleDrive()
        {
            try
            {
                var credential = GoogleCredential.FromFile("/Users/msnabiel/Downloads/msnabiel-c89f9bee7591.json")
                    .CreateScoped(DriveService.ScopeConstants.Drive);

                driveService = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Google Drive Monitor"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing Google Drive: {ex.Message}");
                driveService = null;
            }
        }

        private static async Task MonitorDrive()
        {
            while (true)
            {
                Console.WriteLine("Monitoring Google Drive...");
                if (driveService == null)
                {
                    Console.WriteLine("Drive service not initialized. Exiting monitoring.");
                    return;
                }

                var changesRequest = driveService.Changes.GetStartPageToken();
                var startPageToken = (await changesRequest.ExecuteAsync())?.StartPageTokenValue;

                if (startPageToken == null)
                {
                    Console.WriteLine("Start page token is null. Exiting monitoring.");
                    return;
                }

                try
                {
                    var changes = await driveService.Changes.List(startPageToken).ExecuteAsync();

                    if (changes?.Changes == null || changes.Changes.Count == 0)
                    {
                        Console.WriteLine("No changes found in Google Drive.");
                        await Task.Delay(10000); // Wait for 10 seconds before checking again
                        continue;
                    }

                    foreach (var change in changes.Changes)
                    {
                        // Check if file was removed (renamed or deleted)
                        if (change.Removed.HasValue && change.Removed == true)
                        {
                            string fileIdToRemove = change.FileId ?? "Unknown";
                            Console.WriteLine($"File {fileIdToRemove} was removed (possibly renamed).");

                            // Get the previous metadata for the file (before rename)
                            var previousMetadataRequest = driveService.Files.Get(fileIdToRemove);
                            var previousMetadata = await previousMetadataRequest.ExecuteAsync();

                            if (previousMetadata != null)
                            {
                                string previousName = previousMetadata.Name;
                                string newName = "Unknown";

                                // Check if there's a new file with the same ID (renamed)
                                var newFileRequest = driveService.Files.Get(fileIdToRemove);
                                var newFile = await newFileRequest.ExecuteAsync();

                                if (newFile != null)
                                {
                                    newName = newFile.Name;
                                }

                                Console.WriteLine($"File {fileIdToRemove} was renamed from '{previousName}' to '{newName}'.");
                            }
                            else
                            {
                                Console.WriteLine($"Previous metadata for file {fileIdToRemove} not found.");
                            }

                            continue; // Skip further processing for this change
                        }

                        // Normal processing for modified or added files
                        string fileId = change.FileId ?? "Unknown";
                        string fileName = change.File?.Name ?? "Unknown";

                        var hashResult = await ComputeFileHashAsync(fileId);
                        string currentHash = hashResult.Item1 ?? "Unknown";
                        bool isModified = !string.Equals(currentHash, change.File?.Md5Checksum, StringComparison.OrdinalIgnoreCase);

                        if (isModified)
                        {
                            string modifiedTime = change.File?.ModifiedTimeRaw ?? "Unknown";
                            Console.WriteLine($"File {fileName} (ID: {fileId}) was modified at {modifiedTime}");

                            string timestamp = await GetTimestampAsync(currentHash) ?? "Unavailable";
                            Console.WriteLine($"Timestamp for file {fileName}: {timestamp}");

                            string signatureData = $"{currentHash}:{timestamp}";
                            string signature = SignData(signatureData);
                            Console.WriteLine($"Digital signature for file {fileName}: {signature}");

                            SendEmailNotification("Google Drive Update", $"File {fileName} (ID: {fileId}) was modified at {modifiedTime}.\nTimestamp: {timestamp}\nDigital Signature: {signature}");
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

                await Task.Delay(10000); // Wait for 10 seconds before checking again
            }
        }

        private static async Task<(string?, DateTime)> ComputeFileHashAsync(string fileIdToCompute)
        {
            if (driveService == null)
            {
                return (null, DateTime.MinValue);
            }

            var request = driveService.Files.Get(fileIdToCompute);
            using (var stream = new MemoryStream())
            {
                await request.DownloadAsync(stream);
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(stream.ToArray());
                    string hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                    return (hash, DateTime.UtcNow);
                }
            }
        }

        private static async Task<string?> GetTimestampAsync(string fileHash)
        {
            try
            {
                var timestampRequest = new HttpRequestMessage(HttpMethod.Post, timestampServerUrl)
                {
                    Content = new StringContent(fileHash)
                };

                using (var httpClient = new HttpClient())
                {
                    var timestampResponse = await httpClient.SendAsync(timestampRequest);
                    var timestampData = await timestampResponse.Content.ReadAsStringAsync();
                    return timestampData;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving timestamp: {ex.Message}");
                return null;
            }
        }

        private static string SignData(string data)
        {
            try
            {
                X509Certificate2 certificate = new X509Certificate2(certificatePath, certificatePassword);

                RSA? rsa = certificate.GetRSAPrivateKey();
                if (rsa == null)
                {
                    Console.WriteLine("Error: RSA private key not found in the certificate.");
                    return string.Empty;
                }

                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                byte[] hashBytes = SHA256.HashData(dataBytes);
                byte[] signatureBytes = rsa.SignHash(hashBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                return Convert.ToBase64String(signatureBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error signing data: {ex.Message}");
                return string.Empty;
            }
        }

        private static void SendEmailNotification(string subject, string body)
        {
            try
            {
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
                    client.Send(msg);
                    Console.WriteLine("Email notification sent successfully.");
                }
            }
            catch (SmtpException ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
            }
        }
    }
}
