using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media.Imaging;


namespace VisionWinML
{
    public class FaceData
    {
        public string persistedFaceId { get; set; }
        public string userData { get; set; }
    }
    class PersonFaceCmds
    {
        //static string GetBlobSasUri(CloudBlobContainer container, string userData)
        //{
        //    //Get a reference to a blob within the container.
        //    CloudBlockBlob blob = container.GetBlockBlobReference(userData);

        //    //Upload text to the blob. If the blob does not yet exist, it will be created.
        //    //If the blob does exist, its existing content will be overwritten.
        //    string blobContent = "This blob will be accessible to clients via a Shared Access Signature.";
        //    MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(blobContent));
        //    ms.Position = 0;
        //    using (ms)
        //    {
        //        blob.UploadFromStreamAsync(ms);
        //    }

        //    //Set the expiry time and permissions for the blob.
        //    //In this case the start time is specified as a few minutes in the past, to mitigate clock skew.
        //    //The shared access signature will be valid immediately.
        //    SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy();
        //    sasConstraints.SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-15);
        //    sasConstraints.SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24);
        //    sasConstraints.Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write;

        //    //Generate the shared access signature on the blob, setting the constraints directly on the signature.
        //    string sasBlobToken = blob.GetSharedAccessSignature(sasConstraints);

        //    //Return the URI string for the container, including the SAS token.
        //    return blob.Uri + sasBlobToken;
        //}

        public static async Task<string> AddPersonFace(string personGroupId, string personId, string userData)
        {
            string responseBody = null;
            string uri = HttpHandler.BaseUri + "/" + personGroupId + "/persons/" + personId + "/persistedFaces?userData=" + userData;
            //breakpoint point normal 
            CloudBlockBlob blob = HttpHandler.blobContainer.GetBlockBlobReference(userData);
            var sas1 = blob.GetSharedAccessSignature(new SharedAccessBlobPolicy()
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(1),//Set this date/time according to your requirements
            });
            var imageuri = string.Format("{0}{1}", blob.Uri, sas1);
            string bloburi = new Uri(imageuri).ToString();
            string jsonString = "{\"url\":\"" + bloburi + "\"}";
            //string jsonString = "{\"url\":\"" + GetBlobSasUri(HttpHandler.blobContainer, userData) + "\"}";
            //string jsonString = "{\"url\":\"" + HttpHandler.storagePath + "originals/" + userData + "\"}";

            //HttpContent content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            //HttpResponseMessage response = await HttpHandler.client.PostAsync(uri, content);

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "88c1a3fefd4945f2a9eef3f6a321bf51");
            HttpResponseMessage response;
            byte[] byteData = Encoding.UTF8.GetBytes(jsonString);
            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                response = await client.PostAsync(uri, content);
                if (response.IsSuccessStatusCode)
                {
                    responseBody = await response.Content.ReadAsStringAsync();
                }
                else
                {
                    responseBody = await response.Content.ReadAsStringAsync();
                    globals.ShowJsonErrorPopup(responseBody);
                }
            }

            return responseBody;
        }

        //public static async Task<string> DeletePersonFace(string personGroupId, string personId, string persistedFaceId)
        //{
        //    string responseBody = null;
        //    string uri = HttpHandler.BaseUri + "/" + personGroupId + "/persons/" + personId + "/persistedFaces/" + persistedFaceId;
        //    HttpResponseMessage response = await HttpHandler.client.DeleteAsync(uri);
        //    if (response.IsSuccessStatusCode)
        //    {
        //        responseBody = await response.Content.ReadAsStringAsync();
        //    }
        //    else
        //    {
        //        responseBody = await response.Content.ReadAsStringAsync();
        //        globals.ShowJsonErrorPopup(responseBody);
        //    }
        //    return responseBody;
        //}

        public static async Task<FaceData> GetPersonFace(string personGroupId, string personId, string persistedFaceId)
        {
            FaceData face = null;

            string uri = HttpHandler.BaseUri + "/" + personGroupId + "/persons/" + personId + "/persistedFaces/" + persistedFaceId;
            HttpResponseMessage response = await HttpHandler.client.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                face = JsonConvert.DeserializeObject<FaceData>(responseBody);
            }
            else
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                globals.ShowJsonErrorPopup(responseBody);
            }
            return face;
        }

        public static async Task updateToBlob(StorageFile file)
        {
            CloudBlockBlob blob = null;
            string personGroupId, personId, fileName, blobFileName = null;
            BitmapImage bitmapImage = new BitmapImage();
            IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.Read);

            if (null != fileStream)
            {
                personGroupId = globals.gPersonGroupSelected.personGroupId;
                personId = globals.gPersonSelected.personId;
                fileName = System.Guid.NewGuid() + "." + file.Name.Split('.').Last<string>();
                blobFileName = personGroupId + "/" + personId + "/" + fileName;

                await HttpHandler.blobContainer.CreateIfNotExistsAsync();
                blob = HttpHandler.blobContainer.GetBlockBlobReference(blobFileName);
                await blob.DeleteIfExistsAsync();
                await blob.UploadFromFileAsync(file);
            }
            AddFaceToPerson(blobFileName);
        }

        private static async void AddFaceToPerson(string blobName)
        {
            if (null != blobName)
            {
                //Associate with a face
                string personGroupId = globals.gPersonGroupSelected.personGroupId;
                string personId = globals.gPersonSelected.personId;

                if (null == await AddPersonFace(personGroupId, personId, blobName))
                {
                    //failed, delete blob
                    var blob = HttpHandler.blobContainer.GetBlockBlobReference(blobName);
                    await blob.DeleteIfExistsAsync();
                    blob = HttpHandler.thumbContainer.GetBlockBlobReference(blobName);
                    await blob.DeleteIfExistsAsync();
                }
                else
                {
                    //MessageDialog dialog = new MessageDialog("Picture added to person, it will take some time to show up in the list here. Don't forget to 'Train' person group from settings.", "Picture Added");
                    //await dialog.ShowAsync();
                }
            }
        }
    }
}
