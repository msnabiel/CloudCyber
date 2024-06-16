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
1. **Create a Google Cloud Project**:
   - Visit the [Google Cloud Console](https://console.cloud.google.com/).
   - Create a new project or select an existing project.

2. **Create Service Account Credentials**:
   - Navigate to the API & Services > Credentials section in your project.
   - Create credentials for a "Service Account".
   - Select JSON as the key type and download the JSON file containing your service account credentials.

3. **Provide Credentials to Google Drive Monitor**:
   - Place the downloaded JSON file (`msnabiel-c89f9bee7591.json`) in the project directory.
   - Alternatively, update the file path in the `InitializeGoogleDrive` method (`Program.cs`) if stored elsewhere.

### SMTP Configuration
Google Drive Monitor uses SMTP to send email notifications. Configure the following SMTP settings in `Program.cs`:
- `smtpSenderEmail`: Email address used to send notifications.
- `smtpSenderPassword`: Password for the sender email account.
- `smtpServer`: SMTP server address (e.g., `"smtp.gmail.com"` for Gmail).
- `smtpPort`: SMTP server port number (e.g., `587` for Gmail).

Ensure that the sender email account allows access from less secure apps or uses two-factor authentication with an app password.

### Certificate for Digital Signature
For digital signature generation, provide the following certificate details:
- `certificatePath`: Path to the PFX certificate file.
- `certificatePassword`: Password to access the certificate file.

Ensure the certificate has a private key that can be accessed by the application.

## Setup and Configuration
1. **Compile and Run**:
   - Build and run the application. The program will start monitoring Google Drive and display real-time logs in the console.

2. **Monitoring Interval**:
   - Adjust the monitoring interval (`await Task.Delay(10000);` in `MonitorDrive` method) as per your application's requirements. This determines how frequently the application checks for updates in Google Drive.

3. **Customize Email Notifications**:
   - Customize the email content and formatting in the `SendEmailNotification` method (`Program.cs`) to suit your notification preferences and organizational needs.

## Error Handling and Logging
- Basic error handling is implemented using `try-catch` blocks to log errors to the console. Enhance error handling to include logging to external systems or handling specific exceptions based on your deployment environment.

## Dependencies
- **Google.Apis.Drive.v3**: Library for interfacing with Google Drive API.
- **System.Net.Mail**: .NET library for sending email notifications via SMTP.

## Notes and Considerations
- Ensure the application has proper network connectivity and permissions to access Google Drive API and SMTP server.
- Review and comply with Google Drive API usage policies and quotas to avoid rate limits or restrictions.
- Securely manage and protect credentials, especially sensitive information like API keys, passwords, and certificates.

## Author Information
- **Author**: [msyednabiel@gmail.com](mailto:msyednabiel@gmail.com)
- **Website**: [nabielm.framer.website](https://nabielm.framer.website)


