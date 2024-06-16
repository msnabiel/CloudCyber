import os
import io
import hashlib
import requests
import smtplib
from email.mime.text import MIMEText
from email.mime.multipart import MIMEMultipart
from cryptography.hazmat.primitives import hashes
from cryptography.hazmat.primitives.asymmetric import padding
from cryptography.hazmat.primitives import serialization
from cryptography.hazmat.backends import default_backend
from google.oauth2 import service_account
from googleapiclient.discovery import build
from googleapiclient.http import MediaIoBaseDownload
import datetime
import time

# Constants
smtp_sender_email = "msyednabiel@gmail.com"
smtp_sender_password = "wvyt rxjc vowz bnhr"
smtp_server = "smtp.gmail.com"
smtp_port = 587
timestamp_server_url = "https://time.certbot.org"
certificate_path = "/Users/msnabiel/Desktop/SHARP/CloudCyber/certificate.pem"
certificate_password = "Sunshine!23"

drive_service = None  # Global variable for Google Drive service

def main():
    print("Starting Google Drive Monitor application...")
    initialize_google_drive()
    print("Google Drive initialized.")

    monitor_drive()

def initialize_google_drive():
    try:
        credentials = service_account.Credentials.from_service_account_file(
            '/Users/msnabiel/Downloads/msnabiel-c89f9bee7591.json',
            scopes=['https://www.googleapis.com/auth/drive']
        )
        global drive_service
        drive_service = build('drive', 'v3', credentials=credentials)
    except Exception as ex:
        print(f"Error initializing Google Drive: {str(ex)}")
        drive_service = None

def monitor_drive():
    while True:
        print("Monitoring Google Drive...")
        if not drive_service:
            print("Drive service not initialized. Exiting monitoring.")
            return
        
        changes_request = drive_service.changes().getStartPageToken()
        start_page_token = changes_request.execute().get('startPageToken')

        if not start_page_token:
            print("Start page token is null. Exiting monitoring.")
            return
        
        try:
            changes = drive_service.changes().list(startPageToken=start_page_token).execute()

            if not changes.get('changes'):
                print("No changes found in Google Drive.")
                time.sleep(10)  # Wait for 10 seconds before checking again
                continue
            
            for change in changes['changes']:
                if change.get('removed') and change['removed']:
                    file_id_to_remove = change['fileId'] if 'fileId' in change else 'Unknown'
                    print(f"File {file_id_to_remove} was removed (possibly renamed).")
                    
                    # Get previous metadata for the file (before rename)
                    try:
                        previous_metadata = drive_service.files().get(fileId=file_id_to_remove).execute()
                        previous_name = previous_metadata.get('name', 'Unknown')

                        new_file = drive_service.files().get(fileId=file_id_to_remove).execute()
                        new_name = new_file.get('name', 'Unknown') if new_file else 'Unknown'

                        print(f"File {file_id_to_remove} was renamed from '{previous_name}' to '{new_name}'.")
                    except Exception as ex:
                        print(f"Previous metadata for file {file_id_to_remove} not found: {str(ex)}")
                    
                    continue
                
                # Normal processing for modified or added files
                file_id = change['fileId'] if 'fileId' in change else 'Unknown'
                file_name = change['file']['name'] if 'file' in change and 'name' in change['file'] else 'Unknown'

                current_hash, _ = compute_file_hash(file_id)
                current_md5 = change['file']['md5Checksum'] if 'file' in change and 'md5Checksum' in change['file'] else ''

                is_modified = current_hash and current_hash.lower() != current_md5.lower()

                if is_modified:
                    modified_time = change['file']['modifiedTime'] if 'file' in change and 'modifiedTime' in change['file'] else 'Unknown'
                    print(f"File {file_name} (ID: {file_id}) was modified at {modified_time}")

                    timestamp = get_timestamp(current_hash) or 'Unavailable'
                    print(f"Timestamp for file {file_name}: {timestamp}")

                    signature_data = f"{current_hash}:{timestamp}"
                    signature = sign_data(signature_data)
                    print(f"Digital signature for file {file_name}: {signature}")

                    send_email_notification("Google Drive Update", f"File {file_name} (ID: {file_id}) was modified at {modified_time}.\nTimestamp: {timestamp}\nDigital Signature: {signature}")
                else:
                    print(f"File {file_name} (ID: {file_id}) has not been modified.")
            
            start_page_token = changes['newStartPageToken']

        except Exception as ex:
            print(f"Error occurred during monitoring: {str(ex)}")
        
        time.sleep(10)  # Wait for 10 seconds before checking again

def compute_file_hash(file_id_to_compute):
    request = drive_service.files().get_media(fileId=file_id_to_compute)
    fh = io.BytesIO()
    downloader = MediaIoBaseDownload(fh, request)

    done = False
    while done is False:
        status, done = downloader.next_chunk()
    
    fh.seek(0)
    file_content = fh.read()

    hash_object = hashlib.sha256()
    hash_object.update(file_content)
    file_hash = hash_object.hexdigest()

    return file_hash, datetime.datetime.utcnow()

def get_timestamp(file_hash):
    try:
        timestamp_response = requests.post(timestamp_server_url, data=file_hash)
        timestamp_data = timestamp_response.text
        return timestamp_data
    except Exception as ex:
        print(f"Error retrieving timestamp: {str(ex)}")
        return None

def sign_data(data):
    try:
        with open(certificate_path, 'rb') as f:
            certificate_data = f.read()
        
        private_key = serialization.load_pem_private_key(
            certificate_data,
            password=certificate_password.encode(),
            backend=default_backend()
        )

        signature = private_key.sign(
            data.encode(),
            padding.PKCS1v15(),
            hashes.SHA256()
        )

        return signature
    except Exception as ex:
        print(f"Error signing data: {str(ex)}")
        return ''

def send_email_notification(subject, body):
    try:
        msg = MIMEMultipart()
        msg['From'] = smtp_sender_email
        msg['To'] = "msyednabiel@gmail.com"  # Replace with actual recipient email
        msg['Subject'] = subject
        msg.attach(MIMEText(body, 'plain'))

        server = smtplib.SMTP(smtp_server, smtp_port)
        server.starttls()
        server.login(smtp_sender_email, smtp_sender_password)
        server.sendmail(smtp_sender_email, "msyednabiel@gmail.com", msg.as_string())
        server.quit()

        print("Email notification sent successfully.")
    except Exception as ex:
        print(f"Error sending email: {str(ex)}")

if __name__ == "__main__":
    main()
