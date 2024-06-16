# Google Drive Monitor

## Overview
Google Drive Monitor is a C# console application designed to monitor changes in Google Drive and send email notifications for detected modifications. It utilizes Google Drive API for accessing file metadata and content, and integrates SMTP for sending email notifications.

## Features
- **File Change Detection**: Monitors Google Drive for changes including modifications, renames, and deletions.
- **Hash Comparison**: Computes file hashes to detect modifications in file content.
- **Timestamp Verification**: Retrieves and verifies timestamps to ensure data integrity.
- **Email Notifications**: Sends email notifications with details of modified files, including timestamps and digital signatures.

## Prerequisites
Before running Google Drive Monitor, ensure you have the following prerequisites set up:

### Google API Credentials
Google Drive Monitor requires OAuth credentials to access Google Drive API. Follow these steps to set up your credentials:
1. Visit the [Google Cloud Console](https://console.cloud.google.com/).
2. Create a new project or select an existing project.
3. Navigate to the API & Services > Credentials section.
4. Create credentials for a "Service Account".
5. Download the JSON file containing your service account credentials.
6. Place the downloaded JSON file in the project directory or update the file path in `InitializeGoogleDrive` method (`Program.cs`).

### SMTP Configuration
Google Drive Monitor uses SMTP to send email notifications. Configure the following SMTP settings in `Program.cs`:
- `smtpSenderEmail`: Sender email address for sending notifications.
- `smtpSenderPassword`: Password for the sender email account.
- `smtpServer`: SMTP server address (e.g., `"smtp.gmail.com"` for Gmail).
- `smtpPort`: SMTP server port number (e.g., `587` for Gmail).

Ensure that the sender email account allows access from less secure apps or uses two-factor authentication with an app password.

### Certificate for Digital Signature
For digital signature generation, provide the following certificate details:
- `certificatePath`: Path to the PFX certificate file.
- `certificatePassword`: Password to access the certificate file.

Ensure the certificate has a private key that can be accessed by the application.

## Setup
1. **Compile and Run**:
   - Compile the application and run it. The program will start monitoring Google Drive and display logs in the console.

2. **Monitoring Interval**:
   - Adjust the monitoring interval (`await Task.Delay(10000);` in `MonitorDrive` method) as per your application's requirements.

3. **Email Notifications**:
   - When a file modification is detected, an email notification will be sent to the specified recipient (`SendEmailNotification` method).

## Error Handling
- Basic error handling is implemented to log errors to the console (`try-catch` blocks). Enhance error handling based on specific use case scenarios or integrate with error tracking systems for production environments.

## Dependencies
- **Google.Apis.Drive.v3**: Google Drive API library for accessing Google Drive files and changes.
- **System.Net.Mail**: .NET library for sending email notifications via SMTP.

## Notes
- Ensure proper network connectivity and permissions for accessing Google Drive and sending emails.
- Customize email content and notification logic (`SendEmailNotification` method) based on your requirements.
- Review and comply with Google Drive API usage policies and quotas to avoid rate limits or restrictions.

## License
This project is licensed under the MIT License - see the LICENSE file for details.
