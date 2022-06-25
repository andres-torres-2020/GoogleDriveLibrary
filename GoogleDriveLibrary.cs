
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Upload;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.IO;

namespace GoogleDrive;
public class Library
{
    public Library()
    {

    }
    #region READONLY_FUNCTIONALITY
    public async Task<About> GetAboutAsync(DriveService service, string RequestedFields = "*")
    {
        AboutResource.GetRequest request = service.About.Get();
        request.Fields = RequestedFields;
        About result = await request.ExecuteAsync();
        return result;
    }
    private async Task<List<Google.Apis.Drive.v3.Data.File>> getListAsync(FilesResource.ListRequest request)
    {
        FileList response;
        List<Google.Apis.Drive.v3.Data.File> files = new List<Google.Apis.Drive.v3.Data.File>();
        do
        {
            response = await request.ExecuteAsync();
            Console.WriteLine("{0} more files", response.Files.Count);
            files.AddRange(response.Files);
            if (response.NextPageToken != null && response.NextPageToken.Length > 0)
            {
                request.PageToken = response.NextPageToken;
            }
        } while (response.NextPageToken != null && response.NextPageToken.Length > 0);
        return files;
    }
    public async Task<List<Google.Apis.Drive.v3.Data.File>> ListAsync(DriveService service)
    {
        FilesResource.ListRequest request = new FilesResource.ListRequest(service)
        {
            Fields = "kind,nextPageToken,files(id,name,md5Checksum,mimeType,size,parents,description)",
            PageSize = 50,
            Q = "trashed=false",
            OrderBy = "folder,name"
        };
        return await getListAsync(request);
    }
    public async Task<List<Google.Apis.Drive.v3.Data.File>> ListImagesAsync(DriveService service)
    {
        FilesResource.ListRequest request = new FilesResource.ListRequest(service)
        {
            Fields = "kind,nextPageToken,files(id,name,md5Checksum,mimeType,size,parents,description)",
            PageSize = 50,
            Corpora = "user",
            Q = "trashed=false and mimeType='image/jpeg'",
            OrderBy = "name"
        };
        return await getListAsync(request);
    }
    public async Task<List<Google.Apis.Drive.v3.Data.File>> ListFoldersAsync(DriveService service)
    {
        FilesResource.ListRequest request = new FilesResource.ListRequest(service)
        {
            Fields = "kind,nextPageToken,files(id,name,mimeType,size,parents,description)",
            PageSize = 50,
            Corpora = "user",
            Q = "trashed=false and mimeType='application/vnd.google-apps.folder'",
            OrderBy = "name"
        };
        return await getListAsync(request);
    }
    public async Task<List<Google.Apis.Drive.v3.Data.File>> ListFolderContentsAsync(
        DriveService service
        , string folderId
        )
    {
        FilesResource.ListRequest request = new FilesResource.ListRequest(service)
        {
            Fields = "kind,nextPageToken,files(id,name,md5Checksum,mimeType,size,parents,description)",
            PageSize = 50,
            Corpora = "user",
            Q = $"trashed=false and '{folderId}' in parents",
            OrderBy = "name"
        };
        return await getListAsync(request);
    }
    public async Task<List<Google.Apis.Drive.v3.Data.File>> ListSharedFilesAsync(
        DriveService service
        )
    {
        string RequestedFields = "user(emailAddress)";
        About aboutResults = await GetAboutAsync(service, RequestedFields);
        return await ListSharedFilesAsync(service, aboutResults.User.EmailAddress);
    }
    public async Task<List<Google.Apis.Drive.v3.Data.File>> ListSharedFilesAsync(
        DriveService service,
        string UserEmail
        )
    {
        FilesResource.ListRequest request = new FilesResource.ListRequest(service)
        {
            Fields = "nextPageToken,files(id,name,mimeType,shared,ownedByMe,owners(emailAddress,displayName),permissions(id,emailAddress))",
            PageSize = 50,
            Corpora = "user",
            Q = $"('{UserEmail}' in readers or '{UserEmail}' in writers) and not '{UserEmail}' in owners",
            OrderBy = "name"
        };
        return await getListAsync(request);
    }
    public async Task<List<Google.Apis.Drive.v3.Data.File>> ListVideosAsync(DriveService service)
    {
        FilesResource.ListRequest request = new FilesResource.ListRequest(service)
        {
            PageSize = 50,
            Corpora = "user",
            Fields = "kind,nextPageToken,files(id,name,md5Checksum,mimeType,size,parents,description)",
            //Fields = "Id",
            Q = "trashed=false and mimeType contains 'video'",
            OrderBy = "folder,name"
        };
        return await getListAsync(request);
    }
    #endregion
    #region UPDATE_FUNCTIONALITY
    public async Task<Google.Apis.Drive.v3.Data.File> CreateRootFolderAsync(
        DriveService service
        , string NewFolderName
        )
    {
        Console.WriteLine("Creating folder [{0}]", NewFolderName);
        Google.Apis.Drive.v3.Data.File GoogleFolderMetadata = new Google.Apis.Drive.v3.Data.File()
        {
            Name = NewFolderName,
            MimeType = "application/vnd.google-apps.folder"
        };

        Google.Apis.Drive.v3.Data.File newFolder = await createFolderAsync(service, GoogleFolderMetadata);
        Console.WriteLine("new folder ID: [{0}] at root", newFolder.Id);
        return newFolder;
    }
    public async Task<Google.Apis.Drive.v3.Data.File> CreateSubFolderAsync(
        DriveService service
        , string NewFolderName
        , string ParentFolderId
        )
    {
        Console.WriteLine("Creating subfolder [{0}] of parentId[{1}]", NewFolderName, ParentFolderId);
        Google.Apis.Drive.v3.Data.File GoogleFolderMetadata = new Google.Apis.Drive.v3.Data.File()
        {
            Name = NewFolderName,
            MimeType = "application/vnd.google-apps.folder",
            Parents = new string[] { ParentFolderId }
        };

        Google.Apis.Drive.v3.Data.File newFolder = await createFolderAsync(service, GoogleFolderMetadata);
        string parents = newFolder.Parents == null ? "" : string.Join(",", newFolder.Parents);
        Console.WriteLine("new folder ID: [{0}] of parentId[{1}]", newFolder.Id, parents);
        return newFolder;
    }
    private async Task<Google.Apis.Drive.v3.Data.File> createFolderAsync(
        DriveService service
        , Google.Apis.Drive.v3.Data.File GoogleFolderMetadata
        )
    {
        FilesResource.CreateRequest request = service.Files.Create(GoogleFolderMetadata);
        request.Fields = "id, parents";
        Google.Apis.Drive.v3.Data.File newFolder = await request.ExecuteAsync();
        return newFolder;
    }
    public async Task<string> DeleteFileAsync(
        DriveService service
        , string FileId
        )
    {
        Console.WriteLine($"Deleting FileId[{FileId}]");

        FilesResource.DeleteRequest request = service.Files.Delete(FileId);
        //request.Fields = "id, parents";
        var result = await request.ExecuteAsync();

        Console.WriteLine($"\tdeletion result[{result}]");
        return result;
    }
    public async Task<string> DeletePermissionAsync(
        DriveService service
        , string FileId
        , string PermissionId
        )
    {
        Console.WriteLine($"Deleting Permission : FileId[{FileId}] PermissionId[{PermissionId}]");

        PermissionsResource.DeleteRequest request = service.Permissions.Delete(FileId, PermissionId);
        var result = await request.ExecuteAsync();

        Console.WriteLine($"\tdeletion result[{result}]");
        return result;
    }
    public async Task DownloadFileAsync(
        DriveService service
        , string FileId
        , string DestinationPath
        , string DestinationFile = ""
        )
    {
        Console.WriteLine($"Downloading FileId[{FileId}] to [{DestinationPath}] ...");
        Google.Apis.Drive.v3.Data.File fileInfo = await getFileInfoAsync(service, FileId);
        if (fileInfo == null)
        {
            return;
        }
        string DestinationFilename =
            Path.Combine(DestinationPath
                , DestinationFile.Length == 0 ? fileInfo.Name : DestinationFile
                );

        Console.WriteLine($"\tsaving to file [{DestinationFilename}]");
        //showFileInfo(fileInfo);

        FilesResource.GetRequest request = service.Files.Get(FileId);
        FileStream destinationFile = new FileStream(DestinationFilename, FileMode.Create);
        IDownloadProgress progress;
        do
        {
            progress = request.DownloadWithStatus(destinationFile);
            Thread.Sleep(1000);
        } while (progress.Status != DownloadStatus.Completed && progress.Status != DownloadStatus.Failed);

        destinationFile.Flush();
        destinationFile.Close();
        await destinationFile.DisposeAsync();
        Console.WriteLine("\tdownload finished");
    }
    private async Task<Google.Apis.Drive.v3.Data.File> getFileInfoAsync(
        DriveService service
        , string FileId
        )
    {
        FilesResource.GetRequest request = service.Files.Get(FileId);
        request.Fields = "id,name,mimeType,size";
        Google.Apis.Drive.v3.Data.File result = await request.ExecuteAsync();
        return result;
    }
    public async Task<string> EmptyTrash(DriveService service)
    {
        Console.WriteLine("Emptying trash...");
        FilesResource.EmptyTrashRequest request = service.Files.EmptyTrash();//new FilesResource.EmptyTrashRequest()
        string result = await request.ExecuteAsync();
        Console.WriteLine($"\tEmptying trash result[{result}]");
        return result;
    }
    public async Task UploadFileAsync(
        DriveService service
        , string UploadFilePathName
        , string ParentFolderId
        )
    {
        FileInfo fi = new FileInfo(UploadFilePathName);
        Console.WriteLine($"UploadFileAsync : file[{UploadFilePathName}] parentId[{ParentFolderId}]");

        string UploadFileName = Path.GetFileName(UploadFilePathName);
        if (UploadFileName.Length <= 0)
        {
            return;// null;
        }
        string contentType = ExtensionToContentType(UploadFileName);

        //load file into a memory stream
        FileStream fileContentStream = new FileStream(UploadFilePathName, FileMode.Open);
        MemoryStream inputMemoryStream = new MemoryStream();
        int bufferSize = 1024 * 8;
        int bytesRead = 0;
        byte[] buffer = new byte[bufferSize];
        do
        {
            bytesRead = await fileContentStream.ReadAsync(buffer, 0, bufferSize);
            Console.WriteLine($"\tread {bytesRead} bytes");
            await inputMemoryStream.WriteAsync(buffer, 0, bytesRead);
        } while (bytesRead > 0);
        await fileContentStream.DisposeAsync();
        inputMemoryStream.Seek(0L, SeekOrigin.Begin);

        Google.Apis.Drive.v3.Data.File UploadFileMetaData = new Google.Apis.Drive.v3.Data.File()
        {
            Name = UploadFileName,
        };

        if (ParentFolderId.Length > 0)
        {
            UploadFileMetaData.Parents = new string[] { ParentFolderId };
        }
        FilesResource.CreateMediaUpload request = service.Files.Create(UploadFileMetaData, inputMemoryStream, contentType);
        IUploadProgress progress = await request.UploadAsync();
        while (progress.Status != UploadStatus.Failed && progress.Status != UploadStatus.Completed)
        {
            Console.WriteLine($"\t{progress.Status.ToString()}");
            //Thread.Sleep(100);
            progress = request.GetProgress();
        }
        Console.WriteLine($"\tdone : final status is [{progress.Status.ToString()}]");
    }
    public string ExtensionToContentType(string Filename)
    {
        string extension = Path.GetExtension(Filename).Trim('.').ToLower();
        string contentType;
        if (extension.Equals("jpeg") || extension.Equals("jpg"))
        {
            contentType = "image/jpeg";
        }
        else if (extension.Equals("png") || extension.Equals("bmp") || extension.Equals("gif") || extension.Equals("pdf"))
        {
            contentType = "image/" + extension;
        }
        else if (extension.Equals("mp3") || extension.Equals("mp4"))
        {
            contentType = "video/" + extension;
        }
        else
        {
            contentType = "application/octet-stream";
        }
        return contentType;
    }
    #endregion

}
